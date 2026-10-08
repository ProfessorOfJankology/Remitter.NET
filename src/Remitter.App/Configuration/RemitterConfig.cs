using System.Globalization;
using System.IO;

namespace Remitter.App.Configuration;

public sealed class RemitterConfig
{
    public string BaseUrl { get; init; } = "http://esc-japps:8095";
    public string Token { get; init; } = "";
    public int TimeoutSeconds { get; init; } = 30;
    public int RefreshSeconds { get; init; } = 5;
    public bool AlwaysOnTop { get; init; }
    public bool AutoGrow { get; init; }
    public string DateFormat { get; init; } = "ymd";

    public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, "config.ini");

    public static RemitterConfig Load(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException("config.ini was not found. Copy config.example.ini and set the API token.", path);

        var values = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        string section = "";
        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#') || line.StartsWith(';'))
                continue;
            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1].Trim();
                if (!values.ContainsKey(section))
                    values[section] = new(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            var equals = line.IndexOf('=');
            if (equals <= 0 || section.Length == 0)
                continue;
            values[section][line[..equals].Trim()] = line[(equals + 1)..].Trim();
        }

        string Get(string s, string k, string fallback = "")
            => values.TryGetValue(s, out var group) && group.TryGetValue(k, out var value) ? value : fallback;
        int Int(string s, string k, int fallback)
            => int.TryParse(Get(s, k), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : fallback;
        bool Bool(string s, string k, bool fallback)
        {
            var value = Get(s, k).ToLowerInvariant();
            return value switch { "1" or "true" or "yes" or "on" => true, "0" or "false" or "no" or "off" => false, _ => fallback };
        }

        var baseUrl = Get("API", "BaseUrl", "http://esc-japps:8095").TrimEnd('/');
        var token = Get("API", "Token").Trim();
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || (uri.Scheme != "http" && uri.Scheme != "https"))
            throw new InvalidDataException("API BaseUrl must be an absolute HTTP or HTTPS URL.");
        if (token.Length == 0)
            throw new InvalidDataException("API Token is required.");

        return new RemitterConfig
        {
            BaseUrl = baseUrl,
            Token = token,
            TimeoutSeconds = Math.Clamp(Int("API", "TimeoutSeconds", 30), 1, 300),
            RefreshSeconds = Math.Clamp(Int("General", "RefreshSeconds", 5), 5, 3600),
            AlwaysOnTop = Bool("General", "AlwaysOnTop", false),
            AutoGrow = Bool("General", "AutoGrow", false),
            DateFormat = Get("General", "DateFormat", "ymd").Equals("dmy", StringComparison.OrdinalIgnoreCase) ? "dmy" : "ymd"
        };
    }
}
