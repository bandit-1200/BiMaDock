using System.Diagnostics;
using System.Windows.Threading;
using BiMaDock.Core.Model;
using Microsoft.Extensions.Logging;

namespace BiMaDock.Services;

/// <summary>
/// Holt fehlende Symbole nach dem Start getaktet im Voraus, damit das erste Öffnen einer Kategorie nicht ruckelt.
/// Ein Symbol pro Takt auf dem UI-Thread bei Leerlauf. Das Takten schont auch Anti-Ransomware-Software, weil
/// nicht viele Dateien in kurzer Zeit geschrieben werden. Ziele, die nicht rechtzeitig antworten (z. B. getrennte
/// Netzlaufwerke), werden vorher im Hintergrund erkannt und übersprungen.
/// </summary>
public sealed class IconPreloader(IconService icons, ILogger<IconPreloader> logger) : IDisposable
{
    /// <summary>Wartezeit nach dem Start, bevor das Vorladen beginnt.</summary>
    internal static readonly TimeSpan StartDelay = TimeSpan.FromSeconds(5);

    /// <summary>Abstand zwischen zwei Symbolen.</summary>
    internal static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(75);

    /// <summary>So lange darf die Erreichbarkeitsprüfung eines Ziels dauern, sonst wird es übersprungen.</summary>
    internal static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    /// <summary>Ab dieser Dauer gilt das Laden eines Symbols als langsam (Warnung im Protokoll).</summary>
    internal static readonly TimeSpan SlowIcon = TimeSpan.FromMilliseconds(250);

    private const int MaxParallelProbes = 8;

    private readonly CancellationTokenSource _cts = new();

    /// <summary>Startet das Vorladen im Hintergrund (aufrufen auf dem UI-Thread, Reihenfolge wie von <c>IconPreloadOrder</c>).</summary>
    public void Start(IReadOnlyList<DockEntry> ordered)
    {
        ArgumentNullException.ThrowIfNull(ordered);
        _ = RunSafeAsync(ordered, Dispatcher.CurrentDispatcher);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }

    private async Task RunSafeAsync(IReadOnlyList<DockEntry> ordered, Dispatcher dispatcher)
    {
        try
        {
            await RunAsync(ordered, dispatcher, _cts.Token);
        }
        catch (OperationCanceledException)
        {
            // Programm wird beendet
        }
        catch (Exception ex)
        {
            // Läuft ohne Aufrufer, der den Fehler sehen würde
            logger.LogError(ex, "Vorladen der Symbole fehlgeschlagen");
        }
    }

    private async Task RunAsync(IReadOnlyList<DockEntry> ordered, Dispatcher dispatcher, CancellationToken token)
    {
        await Task.Delay(StartDelay, token);

        var candidates = ordered.Where(icons.NeedsPreload).ToList();
        if (candidates.Count == 0)
        {
            logger.LogInformation("Vorladen der Symbole: nichts zu tun");
            return;
        }

        var reachable = await FindMissingInCacheAsync(candidates, token);
        int skipped = candidates.Count - reachable.Count;
        var total = Stopwatch.StartNew();
        int loaded = 0;
        int slow = 0;
        var loadTime = TimeSpan.Zero;

        foreach (var entry in reachable)
        {
            token.ThrowIfCancellationRequested();
            TimeSpan? duration = await dispatcher.InvokeAsync(() => PreloadOne(entry), DispatcherPriority.ApplicationIdle, token);
            if (duration is { } d)
            {
                loaded++;
                loadTime += d;
                if (d >= SlowIcon)
                {
                    slow++;
                    logger.LogWarning("Symbol für {Name} laden dauerte {Ms} ms", entry.Name, (int)d.TotalMilliseconds);
                }
            }

            await Task.Delay(Interval, token);
        }

        logger.LogInformation("Symbole vorgeladen: {Loaded} geladen, {Skipped} übersprungen (fehlend oder nicht erreichbar), {Slow} langsam, Ladezeit gesamt {Ms} ms, Vorladen insgesamt {Total} ms",
            loaded, skipped, slow, (int)loadTime.TotalMilliseconds, (int)total.ElapsedMilliseconds);
    }

    /// <summary>Lädt ein Symbol, falls es nicht inzwischen schon geladen wurde. Gibt die Dauer zurück, sonst null.</summary>
    private TimeSpan? PreloadOne(DockEntry entry)
    {
        if (!icons.NeedsPreload(entry))
        {
            return null; // Marco hat die Kategorie inzwischen geöffnet
        }

        var watch = Stopwatch.StartNew();
        icons.GetIcon(entry);
        watch.Stop();
        return watch.Elapsed;
    }

    /// <summary>Prüft im Hintergrund mit Zeitlimit, welche Ziele erreichbar sind und noch kein Bild auf der Platte haben.</summary>
    private async Task<List<DockEntry>> FindMissingInCacheAsync(List<DockEntry> candidates, CancellationToken token)
    {
        using var gate = new SemaphoreSlim(MaxParallelProbes);
        var probes = candidates.Select(async entry =>
        {
            await gate.WaitAsync(token);
            try
            {
                var probe = Task.Run(() => icons.IsMissingInDiskCache(entry), token);
                if (await Task.WhenAny(probe, Task.Delay(ProbeTimeout, token)) != probe)
                {
                    logger.LogWarning("Ziel von {Name} antwortet nicht, Symbol wird nicht vorgeladen", entry.Name);
                    return false;
                }

                return await probe;
            }
            finally
            {
                gate.Release();
            }
        }).ToList();

        bool[] results = await Task.WhenAll(probes);
        return candidates.Where((_, i) => results[i]).ToList();
    }
}
