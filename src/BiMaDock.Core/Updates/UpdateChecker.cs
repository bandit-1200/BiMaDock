using System.Net.Http.Headers;
using System.Text.Json;

namespace BiMaDock.Core.Updates;

/// <summary>Neue Version auf GitHub.</summary>
public sealed record UpdateInfo(Version Version, string Tag, string DownloadUrl, string AssetName, long Size, string Notes, string PageUrl);

/// <summary>
/// Fragt das neueste Release auf GitHub ab und lädt den Installer herunter.
/// Releases tragen Tags wie <c>v2.0.0</c> und einen Anhang <c>BiMaDock2-Setup-2.0.0.exe</c>.
/// </summary>
public sealed class UpdateChecker
{
    public const string Repository = "bandit-1200/BiMaDock";
    private const string AssetPrefix = "BiMaDock2-Setup-";

    private readonly HttpClient _http;
    private readonly string _repository;

    public UpdateChecker(HttpClient http, string repository = Repository)
    {
        _http = http;
        _repository = repository;
        if (_http.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("BiMaDock-Updater", "2"));
        }
    }

    /// <summary>Neueres Release als <paramref name="current"/>, sonst null.</summary>
    public async Task<UpdateInfo?> FindNewerAsync(Version current, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{_repository}/releases/latest");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null; // noch kein Release
        }

        response.EnsureSuccessStatusCode();
        var latest = Parse(await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false));
        return latest is not null && latest.Version > Normalize(current) ? latest : null;
    }

    /// <summary>Lädt den Installer in einen temporären Ordner und gibt den Pfad zurück.</summary>
    public async Task<string> DownloadAsync(UpdateInfo update, CancellationToken cancellationToken = default)
    {
        string folder = Path.Combine(Path.GetTempPath(), "BiMaDock2-Update");
        Directory.CreateDirectory(folder);
        string target = Path.Combine(folder, update.AssetName);
        string partial = target + ".part";

        using (var response = await _http.GetAsync(update.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
        {
            response.EnsureSuccessStatusCode();
            await using var source = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using var file = File.Create(partial);
            await source.CopyToAsync(file, cancellationToken).ConfigureAwait(false);
        }

        long size = new FileInfo(partial).Length;
        if (size == 0 || (update.Size > 0 && size != update.Size))
        {
            File.Delete(partial);
            throw new IOException($"Download unvollständig ({size} von {update.Size} Bytes).");
        }

        File.Move(partial, target, overwrite: true);
        return target;
    }

    /// <summary>Liest die Antwort von <c>releases/latest</c>. Ohne passenden Installer-Anhang null.</summary>
    public static UpdateInfo? Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.TryGetProperty("draft", out var draft) && draft.GetBoolean()
            || root.TryGetProperty("prerelease", out var pre) && pre.GetBoolean())
        {
            return null;
        }

        string tag = root.GetProperty("tag_name").GetString() ?? string.Empty;
        if (!TryParseVersion(tag, out var version) || !root.TryGetProperty("assets", out var assets))
        {
            return null;
        }

        foreach (var asset in assets.EnumerateArray())
        {
            string name = asset.GetProperty("name").GetString() ?? string.Empty;
            if (name.StartsWith(AssetPrefix, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                return new UpdateInfo(
                    version,
                    tag,
                    asset.GetProperty("browser_download_url").GetString() ?? string.Empty,
                    name,
                    asset.TryGetProperty("size", out var size) ? size.GetInt64() : 0,
                    root.TryGetProperty("body", out var body) ? body.GetString() ?? string.Empty : string.Empty,
                    root.TryGetProperty("html_url", out var url) ? url.GetString() ?? string.Empty : string.Empty);
            }
        }

        return null;
    }

    /// <summary>"v2.1.0" oder "2.1" → 2.1.0. Ungültiges ergibt false.</summary>
    public static bool TryParseVersion(string? tag, out Version version)
    {
        version = new Version(0, 0, 0);
        string text = (tag ?? string.Empty).Trim().TrimStart('v', 'V').Split('-', '+')[0];
        if (!Version.TryParse(text.Contains('.') ? text : text + ".0", out var parsed))
        {
            return false;
        }

        version = Normalize(parsed);
        return true;
    }

    /// <summary>Auf drei Stellen bringen, damit 2.0.0 und 2.0.0.0 gleich sind.</summary>
    public static Version Normalize(Version v) => new(v.Major, Math.Max(0, v.Minor), Math.Max(0, v.Build));
}
