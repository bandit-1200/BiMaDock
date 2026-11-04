using System.Text;
using Microsoft.Extensions.Logging;

namespace BiMaDock.Services;

/// <summary>
/// Schreibt Protokolle in eine Tagesdatei und löscht Dateien, die älter als 14 Tage sind.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _directory;
    private readonly Lock _lock = new();

    public FileLoggerProvider(string directory)
    {
        _directory = directory;
        Directory.CreateDirectory(directory);
        DeleteOldFiles();
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    public void Dispose()
    {
    }

    private void Write(string category, LogLevel level, string message, Exception? exception)
    {
        var line = new StringBuilder()
            .Append(DateTime.Now.ToString("HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture))
            .Append(' ').Append(level.ToString().ToUpperInvariant()[..4])
            .Append(' ').Append(category[(category.LastIndexOf('.') + 1)..])
            .Append(": ").Append(message);
        if (exception is not null)
        {
            line.AppendLine().Append(exception);
        }

        string file = Path.Combine(_directory, $"bimadock-{DateTime.Now:yyyyMMdd}.log");
        lock (_lock)
        {
            try
            {
                File.AppendAllText(file, line.AppendLine().ToString());
            }
            catch (IOException)
            {
                // Protokollieren darf die App nie zum Absturz bringen.
            }
        }
    }

    private void DeleteOldFiles()
    {
        foreach (var file in Directory.EnumerateFiles(_directory, "bimadock-*.log"))
        {
            try
            {
                if (File.GetLastWriteTime(file) < DateTime.Now.AddDays(-14))
                {
                    File.Delete(file);
                }
            }
            catch (IOException)
            {
                // Nächster Start versucht es erneut.
            }
        }
    }

    private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                provider.Write(category, logLevel, formatter(state, exception), exception);
            }
        }
    }
}
