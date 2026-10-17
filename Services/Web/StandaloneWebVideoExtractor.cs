using System.Net.Http;
using System.Text.RegularExpressions;

namespace PureMediaPlayer.Services.Web;

public sealed partial class StandaloneWebVideoExtractor : IDisposable
{
    private readonly HttpClient _httpClient;

    private static readonly string[] DirectVideoExtensions =
    [
        ".mp4", ".m3u8", ".webm", ".mkv", ".avi", ".mov", ".flac", ".mp3", ".wav"
    ];

    public StandaloneWebVideoExtractor()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };

        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) PureMediaEngine/1.0");
    }

    public async Task<IReadOnlyList<DiscoveredStream>> ResolvePlayableStreamsAsync(string rawInputUrl, CancellationToken ct = default)
    {
        if (!Uri.TryCreate(rawInputUrl.Trim(), UriKind.Absolute, out Uri? targetUri))
        {
            throw new ArgumentException("The supplied address is not a valid absolute URL.", nameof(rawInputUrl));
        }

        if (HasDirectMediaExtension(targetUri))
        {
            return [new DiscoveredStream(targetUri, GetExtensionHint(targetUri), "Direct Stream Link")];
        }

        using HttpResponseMessage response = await _httpClient.GetAsync(targetUri, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        string? contentType = response.Content.Headers.ContentType?.MediaType;
        if (contentType is not null && (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ||
                                        contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) ||
                                        contentType.Contains("mpegurl", StringComparison.OrdinalIgnoreCase)))
        {
            return [new DiscoveredStream(targetUri, contentType, "HTTP Content-Type Header")];
        }

        string htmlDocument = await response.Content.ReadAsStringAsync(ct);
        return ScanDomForMediaStreams(targetUri, htmlDocument);
    }

    private static List<DiscoveredStream> ScanDomForMediaStreams(Uri basePageUri, string htmlContent)
    {
        var discovered = new List<DiscoveredStream>();
        var seenUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryRegisterCandidate(string rawUrlCandidate, string sourceLabel)
        {
            string cleaned = System.Net.WebUtility.HtmlDecode(rawUrlCandidate)
                .Replace("\\/", "/")
                .Trim('"', '\'', ' ');

            if (string.IsNullOrWhiteSpace(cleaned) || cleaned.StartsWith("blob:", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            if (Uri.TryCreate(basePageUri, cleaned, out Uri? resolvedUri) &&
                (resolvedUri.Scheme == Uri.UriSchemeHttp || resolvedUri.Scheme == Uri.UriSchemeHttps))
            {
                if (seenUris.Add(resolvedUri.AbsoluteUri))
                {
                    discovered.Add(new DiscoveredStream(resolvedUri, GetExtensionHint(resolvedUri), sourceLabel));
                }
            }
        }

        foreach (Match match in HtmlVideoOrSourceTagRegex().Matches(htmlContent))
        {
            TryRegisterCandidate(match.Groups["url"].Value, "HTML5 Video/Source Tag");
        }

        foreach (Match match in OpenGraphVideoMetaRegex().Matches(htmlContent))
        {
            TryRegisterCandidate(match.Groups["url"].Value, "OpenGraph / Meta Stream");
        }

        foreach (Match match in InlineStreamUrlRegex().Matches(htmlContent))
        {
            TryRegisterCandidate(match.Groups["url"].Value, "Embedded Script/JSON Stream");
        }

        return discovered;
    }

    private static bool HasDirectMediaExtension(Uri uri)
    {
        string path = uri.AbsolutePath;
        foreach (string ext in DirectVideoExtensions)
        {
            if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase)) return true;
        }
        return false;
    }

    private static string GetExtensionHint(Uri uri)
    {
        string ext = System.IO.Path.GetExtension(uri.AbsolutePath);
        return string.IsNullOrEmpty(ext) ? "STREAM" : ext.TrimStart('.').ToUpperInvariant();
    }

    [GeneratedRegex(@"<(?:video|source)\b[^>]*?\bsrc\s*=\s*[""'](?<url>[^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex HtmlVideoOrSourceTagRegex();

    [GeneratedRegex(@"<meta\b[^>]*?\b(?:property|name)\s*=\s*[""'](?:og:video(?::url|:secure_url)?|twitter:player:stream)[""'][^>]*?\bcontent\s*=\s*[""'](?<url>[^""']+)[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex OpenGraphVideoMetaRegex();

    [GeneratedRegex(@"(?<url>https?:\\?/\\?/[^\s""'<>\\]+?\.(?:mp4|m3u8|webm|mkv)(?:\?[^\s""'<>\\]*)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex InlineStreamUrlRegex();

    public void Dispose() => _httpClient.Dispose();
}