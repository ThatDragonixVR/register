using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Windows;
using MediaScanner.Services;
using MediaScanner.ViewModels;

namespace MediaScanner;

public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                // Services
                services.AddSingleton<IDriveScanner, DriveScanner>();
                services.AddSingleton<IMetadataService, MetadataService>();
                services.AddSingleton<IMediaLibraryService, MediaLibraryService>();
                services.AddSingleton<IDatabaseService, DatabaseService>();
                
                // ViewModels
                services.AddTransient<MainViewModel>();
                
                // Windows
                services.AddTransient<MainWindow>();
            })
            .Build();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}