namespace Healthspan.Offline;

/// <summary>
/// Late-bound wiring between the DelegatingHandler (which cannot take the
/// engine directly: the engine needs HttpClients created by the handler's
/// own pipeline) and the scoped SyncEngine instances.
/// </summary>
public sealed class SyncCoordinator
{
    private SyncEngine? _engine;

    public void Register(SyncEngine engine) => _engine = engine;

    public void Unregister(SyncEngine engine)
    {
        if (ReferenceEquals(_engine, engine))
        {
            _engine = null;
        }
    }

    public SyncEngine? Engine => _engine;

    public async Task TriggerDrainAsync()
    {
        var engine = _engine;
        if (engine is null)
        {
            return;
        }

        try
        {
            await engine.DrainAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[sync] triggered drain failed: {ex.Message}");
        }
    }
}
