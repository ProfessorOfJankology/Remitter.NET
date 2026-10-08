using System.Net.Http.Headers;
using System.Net.Http.Json;
using Remitter.Domain.Models;

namespace Remitter.Client;

public sealed class RemitterApiClient
{
    private readonly HttpClient _httpClient;

    public RemitterApiClient(HttpClient httpClient, string? bearerToken = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (!string.IsNullOrWhiteSpace(bearerToken))
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", bearerToken);
    }

    public async Task<IReadOnlyList<QueueItem>> GetQueueAsync(CancellationToken cancellationToken = default)
        => await _httpClient.GetFromJsonAsync<List<QueueItem>>("api/queue", cancellationToken)
           ?? [];
}
