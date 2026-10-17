namespace Healthspan.Offline;

/// <summary>
/// Attaches the offline handler to the app's HttpClient without forcing DI
/// to build the JS-backed store at startup: the real handler is created on
/// first use, when the Blazor JS runtime is guaranteed to exist.
/// </summary>
public sealed class LazyOfflineHandler : DelegatingHandler
{
    private readonly IServiceProvider _services;
    private OfflineHttpHandler? _inner;

    public LazyOfflineHandler(IServiceProvider services)
    {
        _services = services;
        // The chain must be complete BEFORE the HttpClient is constructed.
        // Blazor WASM does NOT inject a transport into a custom chain: the
        // terminal handler must be an HttpClientHandler (mapped to the
        // browser fetch transport on WASM).
        _inner = services.GetRequiredService<OfflineHttpHandler>();
        _inner.AssignTerminalHandler(new HttpClientHandler());
        InnerHandler = _inner;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return base.SendAsync(request, cancellationToken);
    }
}
