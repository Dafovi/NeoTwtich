using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace NeoTwitch.Services.Configuration.Security;

public interface IConfigurationSecretProtector
{
    string Protect(string purpose, string plaintext);

    string Unprotect(string purpose, string protectedValue);
}

public sealed class WindowsDpapiConfigurationSecretProtector : IConfigurationSecretProtector
{
    private const int CryptProtectUiForbidden = 0x1;
    private const string Prefix = "dpapi:v1:";

    public string Protect(string purpose, string plaintext)
    {
        if (string.IsNullOrEmpty(plaintext))
        {
            return "";
        }

        EnsureWindows();
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var entropyBytes = CreateEntropy(purpose);
        try
        {
            using var input = DataBlobHandle.FromBytes(plaintextBytes);
            using var entropy = DataBlobHandle.FromBytes(entropyBytes);
            if (!CryptProtectData(
                    ref input.Blob,
                    "Neo Twitch configuration secret",
                    ref entropy.Blob,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    out var output))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows no pudo proteger una credencial local.");
            }

            try
            {
                return Prefix + Convert.ToBase64String(output.ToArray());
            }
            finally
            {
                output.FreeLocal();
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintextBytes);
            CryptographicOperations.ZeroMemory(entropyBytes);
        }
    }

    public string Unprotect(string purpose, string protectedValue)
    {
        if (string.IsNullOrEmpty(protectedValue))
        {
            return "";
        }

        EnsureWindows();
        if (!protectedValue.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("La credencial protegida usa un formato desconocido.");
        }

        byte[] ciphertext;
        try
        {
            ciphertext = Convert.FromBase64String(protectedValue[Prefix.Length..]);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("La credencial protegida está dañada.", ex);
        }

        var entropyBytes = CreateEntropy(purpose);
        try
        {
            using var input = DataBlobHandle.FromBytes(ciphertext);
            using var entropy = DataBlobHandle.FromBytes(entropyBytes);
            if (!CryptUnprotectData(
                    ref input.Blob,
                    IntPtr.Zero,
                    ref entropy.Blob,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    CryptProtectUiForbidden,
                    out var output))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows no pudo abrir una credencial para este usuario.");
            }

            try
            {
                var plaintextBytes = output.ToArray();
                try
                {
                    return Encoding.UTF8.GetString(plaintextBytes);
                }
                finally
                {
                    CryptographicOperations.ZeroMemory(plaintextBytes);
                }
            }
            finally
            {
                output.FreeLocal();
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(ciphertext);
            CryptographicOperations.ZeroMemory(entropyBytes);
        }
    }

    private static byte[] CreateEntropy(string purpose) =>
        SHA256.HashData(Encoding.UTF8.GetBytes($"NeoTwitch|configuration|{purpose}"));

    private static void EnsureWindows()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("La protección DPAPI de configuración requiere Windows.");
        }
    }

    [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptProtectData(
        ref DataBlob dataIn,
        string description,
        ref DataBlob optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        int flags,
        out DataBlob dataOut);

    [DllImport("Crypt32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CryptUnprotectData(
        ref DataBlob dataIn,
        IntPtr description,
        ref DataBlob optionalEntropy,
        IntPtr reserved,
        IntPtr promptStruct,
        int flags,
        out DataBlob dataOut);

    [DllImport("Kernel32.dll", SetLastError = true)]
    private static extern IntPtr LocalFree(IntPtr memory);

    [StructLayout(LayoutKind.Sequential)]
    private struct DataBlob
    {
        public int Length;
        public IntPtr Data;

        public readonly byte[] ToArray()
        {
            var bytes = new byte[Length];
            if (Length > 0)
            {
                Marshal.Copy(Data, bytes, 0, Length);
            }

            return bytes;
        }

        public void FreeLocal()
        {
            if (Data != IntPtr.Zero)
            {
                if (Length > 0)
                {
                    var zeroes = new byte[Length];
                    Marshal.Copy(zeroes, 0, Data, Length);
                }

                _ = LocalFree(Data);
                Data = IntPtr.Zero;
                Length = 0;
            }
        }
    }

    private sealed class DataBlobHandle : IDisposable
    {
        private DataBlobHandle(DataBlob blob)
        {
            Blob = blob;
        }

        public DataBlob Blob;

        public static DataBlobHandle FromBytes(byte[] bytes)
        {
            var pointer = Marshal.AllocHGlobal(bytes.Length);
            if (bytes.Length > 0)
            {
                Marshal.Copy(bytes, 0, pointer, bytes.Length);
            }

            return new DataBlobHandle(new DataBlob { Length = bytes.Length, Data = pointer });
        }

        public void Dispose()
        {
            if (Blob.Data == IntPtr.Zero)
            {
                return;
            }

            if (Blob.Length > 0)
            {
                var zeroes = new byte[Blob.Length];
                Marshal.Copy(zeroes, 0, Blob.Data, Blob.Length);
            }

            Marshal.FreeHGlobal(Blob.Data);
            Blob.Data = IntPtr.Zero;
            Blob.Length = 0;
        }
    }
}
