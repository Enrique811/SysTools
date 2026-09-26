using SysTools.Entities.Support;
using SysTools.Presentation.Modules.Support;

namespace SysTools.Presentation.Tests.Support;

public sealed class SupportLogsViewModelTests
{
    [Fact] public async Task Refresh_preserves_existing_selection_and_loads_preview()
    {
        var file=new LogFileSummary("a.log",DateTime.UtcNow,10);var workflow=new SupportWorkflowStub
        {Catalog=new(LogCatalogStatus.Available,[file]),Preview=new(LogPreviewStatus.Loaded,"a.log","line",false)};
        var vm=new SupportViewModel(workflow,new ClipboardStub());await vm.ActivateAsync();
        Assert.Single(vm.Logs);vm.SelectedLog=vm.Logs[0];await vm.LoadSelectedLogCommand.ExecuteAsync();
        Assert.Equal("line",vm.LogPreview);
        await vm.RefreshLogsCommand.ExecuteAsync();Assert.Equal("a.log",vm.SelectedLog?.Id);
    }
    [Fact] public async Task Empty_catalog_and_failed_preview_are_controlled()
    {
        var workflow=new SupportWorkflowStub();var vm=new SupportViewModel(workflow,new ClipboardStub());await vm.ActivateAsync();
        Assert.Empty(vm.Logs);Assert.Contains("vacía",vm.StatusText,StringComparison.Ordinal);
    }
}

