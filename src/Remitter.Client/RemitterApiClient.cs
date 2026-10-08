using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Remitter.Domain.Models;

namespace Remitter.Client;

public sealed class RemitterApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public RemitterApiClient(HttpClient httpClient, string? bearerToken = null, string? actor = null, string? clientName = null)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        if (!string.IsNullOrWhiteSpace(bearerToken))
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
        if (!string.IsNullOrWhiteSpace(actor))
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Remitter-Actor", actor);
        if (!string.IsNullOrWhiteSpace(clientName))
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Remitter-Client", clientName);
    }

    public Task<HealthResponse> GetHealthAsync(CancellationToken ct = default)
        => SendAsync<HealthResponse>(HttpMethod.Get, "/api/health", null, ct);

    public async Task<IReadOnlyList<Payment>> GetPaymentsAsync(CancellationToken ct = default)
        => (await SendAsync<PaymentsResponse>(HttpMethod.Get, "/api/payments", null, ct)).Items;

    public Task<PaymentMutationResponse> AddPaymentAsync(AddPaymentRequest request, CancellationToken ct = default)
        => SendAsync<PaymentMutationResponse>(HttpMethod.Post, "/api/payments", request, ct);

    public Task<BatchResultResponse> CheckPaymentsAsync(CheckPaymentsRequest request, CancellationToken ct = default)
        => SendAsync<BatchResultResponse>(HttpMethod.Post, "/api/payments/check", request, ct);

    public Task<BatchResultResponse> RemovePaymentsAsync(RemovePaymentsRequest request, CancellationToken ct = default)
        => SendAsync<BatchResultResponse>(HttpMethod.Post, "/api/payments/remove", request, ct);

    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        using var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var message = response.ReasonPhrase ?? "Request failed.";
            try
            {
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
                if (doc.RootElement.TryGetProperty("error", out var error))
                    message = error.GetString() ?? message;
            }
            catch (JsonException) { }

            throw new RemitterApiException(message, (int)response.StatusCode);
        }

        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Server returned an empty response.");
    }
}
