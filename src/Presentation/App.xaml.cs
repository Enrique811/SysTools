using System.Windows;
using System.Windows.Threading;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using SysTools.Business.Configuration;
using SysTools.Business.Connection;
using SysTools.Business.Labels;
using SysTools.Business.Products;
using SysTools.Business.Repositories;
using SysTools.Data.Configuration;
using SysTools.Data.Connection;
using SysTools.Data.Repositories;
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
        services.AddSingleton<ConfigurationValidator>();
        services.AddSingleton<IConfigurationPathProvider, AppDataConfigurationPathProvider>();
        services.AddSingleton<ISecretProtector, DpapiSecretProtector>();
        services.AddSingleton<IAtomicFileWriter, AtomicFileWriter>();
        services.AddSingleton<IConfigurationRepository, JsonConfigurationRepository>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<ConnectionConfigurationValidator>();
        services.AddSingleton<IFirebirdConnectionFactory, FirebirdConnectionFactory>();
        services.AddSingleton<IFirebirdConnectionProbe, FirebirdConnectionProbe>();
        services.AddSingleton<IConnectionTestService, ConnectionTestService>();
        services.AddSingleton<IProductRepository, FirebirdProductRepository>();
        services.AddSingleton<IServerClockRepository, FirebirdServerClockRepository>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<IPriceFormatterService, PriceFormatterService>();
        services.AddSingleton<ILabelQueueService, LabelQueueService>();
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
