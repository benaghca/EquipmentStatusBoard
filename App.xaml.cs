using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using EquipmentStatusTracker.WPF.Services;
using EquipmentStatusTracker.WPF.ViewModels;
using EquipmentStatusTracker.WPF.Views;

namespace EquipmentStatusTracker.WPF;

public partial class App : Application
{
    private readonly ServiceProvider _serviceProvider;

    public App()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register services
        services.AddSingleton<IProjectService, ProjectService>();
        services.AddSingleton<IUndoRedoService, UndoRedoService>();

        // Register ViewModels
        services.AddSingleton<MainViewModel>();

        // Register Views
        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Dispose the service provider to clean up resources
        _serviceProvider.Dispose();
        base.OnExit(e);
    }
}
