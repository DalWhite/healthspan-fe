public class HealthspanApiClient
{
    private readonly HttpClient _httpClient;
    public HealthspanApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }
    
}
