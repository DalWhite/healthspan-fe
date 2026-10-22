/// <summary>
/// Bridge between the Today workout view and the app shell. While a session is
/// in progress the shell FAB becomes the green "Finalizar" action (DESIGN.md
/// §5.2), so the shell needs to know that a session exists without owning any
/// workout data itself.
///
/// Registered as Scoped: in Blazor WebAssembly one circuit is one scope, so the
/// layout and the page resolve the same instance.
/// </summary>
public sealed class ActiveSessionState
{
    public Guid? SessionId { get; private set; }

    /// <summary>Subscribed by the shell so the FAB re-renders on change.</summary>
    public event Action? Changed;

    private Func<Task>? _finishHandler;

    public void SetSession(Guid? sessionId)
    {
        if (SessionId == sessionId)
        {
            return;
        }

        SessionId = sessionId;
        Changed?.Invoke();
    }

    /// <summary>The page registers the real finish flow (save fatigue, show the
    /// summary) so the shell FAB triggers exactly what the page's own "Finalizar"
    /// button does.</summary>
    public void RegisterFinish(Func<Task> handler) => _finishHandler = handler;

    public void UnregisterFinish(Func<Task> handler)
    {
        if (ReferenceEquals(_finishHandler, handler))
        {
            _finishHandler = null;
        }
    }

    public Task FinishAsync()
        => _finishHandler is { } handler ? handler() : Task.CompletedTask;
}
