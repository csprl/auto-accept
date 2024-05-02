using System.Text.Json;

namespace AutoAccept.Utils;

internal abstract class ClientBase : IDisposable
{
    protected static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected readonly HttpClient HttpClient;

    protected ClientBase()
    {
        // Create handler without certificate validation
        var httpClientHandler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        };

        // Configure http client
        HttpClient = new HttpClient(httpClientHandler)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    public void Dispose() =>
        HttpClient.Dispose();
}
