using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Remitter.Domain.Models;

namespace Remitter.Client;

public sealed class ManualAddRetryStore
{
    private readonly string _path;
    private readonly object _sync = new();

    public ManualAddRetryStore(string baseUrl)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Remitter.NET");
        Directory.CreateDirectory(root);
        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(baseUrl))).ToLowerInvariant()[..16];
        _path = Path.Combine(root, key + ".pending-add.json");
    }

    public async Task<PaymentMutationResponse> AddAsync(
        RemitterApiClient api,
        string invoiceNo,
        string amount,
        int payType,
        string comment,
        CancellationToken ct = default)
    {
        var fingerprint = Fingerprint(invoiceNo, amount, payType, comment);
        string operationId;

        lock (_sync)
        {
            var pending = Read();
            if (!pending.TryGetValue(fingerprint, out operationId!))
            {
                operationId = Guid.NewGuid().ToString();
                pending[fingerprint] = operationId;
                Write(pending);
            }
        }

        var response = await api.AddPaymentAsync(new AddPaymentRequest
        {
            InvoiceNo = invoiceNo,
            Amount = amount,
            PayType = payType,
            Comment = comment,
            OperationId = operationId
        }, ct);

        lock (_sync)
        {
            var pending = Read();
            if (pending.TryGetValue(fingerprint, out var stored) && stored == operationId)
            {
                pending.Remove(fingerprint);
                Write(pending);
            }
        }

        return response;
    }

    private static string Fingerprint(string invoiceNo, string amount, int payType, string comment)
    {
        var canonical = JsonSerializer.Serialize(new { invoiceNo, amount, payType, comment });
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }

    private Dictionary<string, string> Read()
    {
        if (!File.Exists(_path))
            return new Dictionary<string, string>();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(_path))
                ?? new Dictionary<string, string>();
        }
        catch (JsonException)
        {
            return new Dictionary<string, string>();
        }
    }

    private void Write(Dictionary<string, string> pending)
    {
        var temp = _path + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(pending));
        File.Move(temp, _path, true);
    }
}
