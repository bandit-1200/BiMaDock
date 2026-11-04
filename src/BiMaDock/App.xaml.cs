using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using BiMaDock.Core;
using BiMaDock.Core.Settings;
using BiMaDock.Core.Storage;
using BiMaDock.Services;
using BiMaDock.ViewModels;
using BiMaDock.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BiMaDock;

public partial class App : Application
{
    private Mutex? _singleInstance;
    private ServiceProvider? _services;
    private ILogger<App>? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Pro Datenordner nur eine Instanz (Tests mit eigenem Ordner laufen parallel zur normalen App).
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(AppPaths.DataDirectory.ToUpperInvariant())))[..16];
        _singleInstance = new Mutex(initiallyOwned: true, $@"Local\BiMaDock2-{hash}", out bool createdNew);
        if (!createdNew)
        {
            _singleInstance.Dispose();
            _singleInstance = null;
            Shutdown();
            return;
        }

        _services = ConfigureServices();
        _logger = _services.GetRequiredService<ILogger<App>>();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            _logger.LogCritical(args.ExceptionObject as Exception, "Unbehandelter Fehler, App wird beendet");
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            _logger.LogError(args.Exception, "Unbeobachteter Task-Fehler");
            args.SetObserved();
        };

        _logger.LogInformation("BiMaDock {Version} startet, Daten in {Dir}",
            typeof(App).Assembly.GetName().Version, AppPaths.DataDirectory);

        try
        {
            _services.GetRequiredService<DockViewModel>().Load();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "Dock-Daten konnten nicht geladen werden");
            MessageBox.Show($"Die Dock-Daten konnten nicht geladen werden:\n{ex.Message}", "BiMaDock",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        MainWindow = _services.GetRequiredService<DockWindow>();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _logger?.LogInformation("BiMaDock wird beendet");
        _services?.Dispose();
        if (_singleInstance is not null)
        {
            _singleInstance.ReleaseMutex();
            _singleInstance.Dispose();
        }

        base.OnExit(e);
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Information)
            .AddProvider(new FileLoggerProvider(AppPaths.LogDirectory)));
        services.AddSingleton(sp => new DockStore(AppPaths.DockFile, AppPaths.LegacyDockFile, sp.GetRequiredService<ILogger<DockStore>>()));
        services.AddSingleton<IconService>();
        services.AddSingleton<StartupService>();
        services.AddSingleton(new SettingsStore(AppPaths.SettingsFile));
        services.AddSingleton<ILauncher, ShellLauncher>();
        services.AddSingleton<DockViewModel>();
        services.AddSingleton<DockWindow>();
        return services.BuildServiceProvider();
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Ein Fehler in einer einzelnen Aktion soll das Dock nicht beenden.
        _logger?.LogError(e.Exception, "Unbehandelter Fehler im UI-Thread");
        e.Handled = true;
    }
}
