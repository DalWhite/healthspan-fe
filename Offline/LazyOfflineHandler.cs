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
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _inner ??= _services.GetRequiredService<OfflineHttpHandler>();
        return _inner.ProcessAsync(request, base.InnerHandler!, cancellationToken);
    }
}
