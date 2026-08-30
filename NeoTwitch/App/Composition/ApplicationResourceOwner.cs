namespace NeoTwitch.Services;

public static class ApplicationShutdownOrder
{
    public const int EventIngress = 100;
    public const int ActiveExecution = 200;
    public const int VisualMedia = 300;
    public const int Connections = 400;
    public const int NetworkClients = 500;
    public const int Persistence = 600;
}

public sealed record ApplicationResourceDisposalFailure(string ResourceName, Exception Exception);

public sealed class ApplicationResourceOwner : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly List<Registration> _registrations = [];
    private readonly List<ApplicationResourceDisposalFailure> _failures = [];
    private Task? _disposeTask;
    private long _sequence;

    public IReadOnlyList<ApplicationResourceDisposalFailure> Failures
    {
        get
        {
            lock (_sync)
            {
                return _failures.ToArray();
            }
        }
    }

    public void Register(string name, int order, Func<ValueTask> disposeAsync)
    {
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposeTask is not null, this);
            _registrations.Add(new Registration(name, order, _sequence++, disposeAsync));
        }
    }

    public void Register(string name, int order, IDisposable resource) =>
        Register(name, order, () =>
        {
            resource.Dispose();
            return ValueTask.CompletedTask;
        });

    public void Register(string name, int order, IAsyncDisposable resource) =>
        Register(name, order, resource.DisposeAsync);

    public ValueTask DisposeAsync()
    {
        lock (_sync)
        {
            _disposeTask ??= DisposeCoreAsync();
            return new ValueTask(_disposeTask);
        }
    }

    private async Task DisposeCoreAsync()
    {
        Registration[] registrations;
        lock (_sync)
        {
            registrations = _registrations
                .OrderBy(item => item.Order)
                .ThenBy(item => item.Sequence)
                .ToArray();
        }

        foreach (var registration in registrations)
        {
            try
            {
                await registration.DisposeAsync();
            }
            catch (Exception ex)
            {
                lock (_sync)
                {
                    _failures.Add(new ApplicationResourceDisposalFailure(registration.Name, ex));
                }
            }
        }
    }

    private sealed record Registration(string Name, int Order, long Sequence, Func<ValueTask> DisposeAsync);
}
