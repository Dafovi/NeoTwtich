using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using NeoTwitch.Services.Text;

namespace NeoTwitch.Services.Lights;

internal static class WindowsSerialPortApi
{
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x80;
    private const uint PurgeRxAbort = 0x0002;
    private const uint PurgeRxClear = 0x0008;

    public static SafeFileHandle OpenAndConfigure(string port, int baudRate, IUiTextService text)
    {
        var handle = CreateFile(
            $@"\\.\{port}",
            GenericRead | GenericWrite,
            0,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal,
            IntPtr.Zero);

        if (handle.IsInvalid)
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), text.Format(UiTextKeys.SerialOpenFailure, port));
        }

        Configure(handle, baudRate, text);
        return handle;
    }

    public static bool TryWrite(SafeFileHandle handle, byte[] bytes, out uint written, out int error)
    {
        var success = WriteFile(handle, bytes, (uint)bytes.Length, out written, IntPtr.Zero);
        error = success ? 0 : Marshal.GetLastWin32Error();
        return success;
    }

    public static bool TryRead(SafeFileHandle handle, byte[] buffer, out uint read)
    {
        return ReadFile(handle, buffer, 1, out read, IntPtr.Zero);
    }

    public static void ClearReadBuffer(SafeFileHandle handle)
    {
        _ = PurgeComm(handle, PurgeRxAbort | PurgeRxClear);
    }

    private static void Configure(SafeFileHandle handle, int baudRate, IUiTextService text)
    {
        var dcb = new Dcb
        {
            DcbLength = (uint)Marshal.SizeOf<Dcb>()
        };

        if (!BuildCommDCB($"baud={baudRate} parity=N data=8 stop=1", ref dcb))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), text.Get(UiTextKeys.SerialPrepareFailure));
        }

        if (!SetCommState(handle, ref dcb))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), text.Get(UiTextKeys.SerialApplyFailure));
        }

        var timeouts = new CommTimeouts
        {
            ReadIntervalTimeout = 30,
            ReadTotalTimeoutConstant = 80,
            WriteTotalTimeoutConstant = 1000,
            WriteTotalTimeoutMultiplier = 10
        };

        if (!SetCommTimeouts(handle, ref timeouts))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error(), text.Get(UiTextKeys.SerialTimeoutFailure));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool BuildCommDCB(string lpDef, ref Dcb lpDcb);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetCommState(SafeFileHandle hFile, ref Dcb lpDcb);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetCommTimeouts(SafeFileHandle hFile, ref CommTimeouts lpCommTimeouts);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool PurgeComm(SafeFileHandle hFile, uint dwFlags);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(
        SafeFileHandle hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToWrite,
        out uint lpNumberOfBytesWritten,
        IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(
        SafeFileHandle hFile,
        byte[] lpBuffer,
        uint nNumberOfBytesToRead,
        out uint lpNumberOfBytesRead,
        IntPtr lpOverlapped);

    [StructLayout(LayoutKind.Sequential)]
    private struct Dcb
    {
        public uint DcbLength;
        public uint BaudRate;
        public uint Flags;
        public ushort WReserved;
        public ushort XonLim;
        public ushort XoffLim;
        public byte ByteSize;
        public byte Parity;
        public byte StopBits;
        public sbyte XonChar;
        public sbyte XoffChar;
        public sbyte ErrorChar;
        public sbyte EofChar;
        public sbyte EvtChar;
        public ushort WReserved1;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct CommTimeouts
    {
        public uint ReadIntervalTimeout;
        public uint ReadTotalTimeoutMultiplier;
        public uint ReadTotalTimeoutConstant;
        public uint WriteTotalTimeoutMultiplier;
        public uint WriteTotalTimeoutConstant;
    }
}
