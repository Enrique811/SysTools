using System.Windows;
using System.Windows.Threading;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SysTools.Presentation.Modules.PriceVerifier.ViewModels;
using SysTools.Presentation.Shell.Services;
using SysTools.Presentation.Shell.ViewModels;
using SysTools.Presentation.Shell.Views;

namespace SysTools.Presentation;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += HandleDispatcherUnhandledException;
        var logDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SysTools", "Logs");
        Log.Logger = new LoggerConfiguration().MinimumLevel.Information().Enrich.WithProperty("Application", "SysTools")
            .WriteTo.File(Path.Combine(logDirectory, "systools-.log"), rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14).CreateLogger();
        try
        {
            var builder = Host.CreateApplicationBuilder(e.Args);
            builder.Services.AddSerilog(Log.Logger, dispose: true);
            ConfigureServices(builder.Services);
            _host = builder.Build();
            await _host.StartAsync();
            Log.Information("Application starting at {Stage}", "Startup");
            var shell = _host.Services.GetRequiredService<ShellWindow>();
            MainWindow = shell;
            shell.Show();
            Log.Information("Shell loaded at {Stage} with {ModuleId}", "Initialization", ShellViewModel.PriceVerifierModuleId);
        }
        catch (Exception exception)
        {
            Log.Fatal(exception, "Fatal application startup failure at {Stage}", "Startup");
            Shutdown(-1);
        }
    }

    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IModuleInitializer, DefaultModuleInitializer>();
        services.AddSingleton<PriceVerifierViewModel>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<ShellWindow>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        Log.Information("Application closing at {Stage}", "Shutdown");
        if (_host is not null) { await _host.StopAsync(TimeSpan.FromSeconds(2)); _host.Dispose(); }
        await Log.CloseAndFlushAsync();
        base.OnExit(e);
    }

    private void HandleDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Log.Fatal(e.Exception, "Fatal UI error at {Stage}", "Runtime");
        e.Handled = true;
        Shutdown(-1);
    }
}
