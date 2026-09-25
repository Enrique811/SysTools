using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SysTools.Business.PriceVerifier;
using SysTools.Presentation.Modules.PriceVerifier.ViewModels;
using SysTools.Presentation.Modules.PriceVerifier.Search;
using SysTools.Business.ConfigurationEditor;
using SysTools.Presentation.Modules.Configuration;
using SysTools.Presentation.Modules.Configuration.Services;
using SysTools.Presentation.Shell.Models;
using SysTools.Presentation.Shell.ViewModels;
using SysTools.Presentation.Shell.Views;
using SysTools.Presentation.ViewModels;

namespace SysTools.Presentation.Tests.Composition;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void Composition_validates_and_uses_single_workflow_and_view_model_instances()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.ClearProviders());
        App.ConfigureServices(services);
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

        Assert.Same(
            provider.GetRequiredService<IPriceVerifierWorkflow>(),
            provider.GetRequiredService<IPriceVerifierWorkflow>());
        var viewModel = provider.GetRequiredService<PriceVerifierViewModel>();
        Assert.Same(viewModel, provider.GetRequiredService<PriceVerifierViewModel>());
        Assert.Same(viewModel, provider.GetRequiredService<ShellViewModel>().ActiveModuleContent);
        Assert.Same(
            provider.GetRequiredService<IProductSearchDialogService>(),
            provider.GetRequiredService<IProductSearchDialogService>());
        Assert.NotSame(
            provider.GetRequiredService<ProductSearchViewModel>(),
            provider.GetRequiredService<ProductSearchViewModel>());
        Assert.NotSame(provider.GetRequiredService<IConfigurationEditorWorkflow>(), provider.GetRequiredService<IConfigurationEditorWorkflow>());
        Assert.NotSame(provider.GetRequiredService<ConfigurationViewModel>(), provider.GetRequiredService<ConfigurationViewModel>());
        Assert.Same(provider.GetRequiredService<IConfigurationDialogService>(), provider.GetRequiredService<IConfigurationDialogService>());
    }

    [Fact]
    public void Composition_resolves_shell_and_accepts_demo_module_without_external_services()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var services = new ServiceCollection();
                services.AddLogging(builder => builder.ClearProviders());
                App.ConfigureServices(services);
                using var provider = services.BuildServiceProvider();
                var window = provider.GetRequiredService<ShellWindow>();
                var shell = Assert.IsType<ShellViewModel>(window.DataContext);
                var demo = new UtilityModuleItem("demo-module", "Módulo demostrativo", ModuleSection.Utilities, true, new DemoViewModel());
                Assert.True(shell.RegisterModule(demo));
                Assert.True(shell.SelectModule(demo));
                Assert.Same(demo.Content, shell.ActiveModuleContent);
                window.Close();
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    private sealed class DemoViewModel : ViewModelBase;
}
