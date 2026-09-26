using SysTools.Entities.Support;
using SysTools.Presentation.Modules.Support;

namespace SysTools.Presentation.Tests.Support;

public sealed class SupportErrorReportViewModelTests
{
    [Fact] public async Task Report_fields_prepare_draft_and_open_selected_channel()
    {
        var workflow=new SupportWorkflowStub();var vm=new SupportViewModel(workflow,new ClipboardStub());await vm.ActivateAsync();
        vm.ErrorSummary="Resumen";vm.ErrorDetail="Detalle";await vm.PrepareErrorCommand.ExecuteAsync();
        Assert.Equal(SupportMailKind.ErrorReport,vm.CurrentDraft!.Kind);
        vm.SelectedChannel=SupportChannel.OutlookWeb;await vm.OpenDraftCommand.ExecuteAsync();Assert.Equal(1,workflow.OpenCalls);
    }
    [Fact] public async Task Invalid_result_clears_previous_draft()
    {
        var workflow=new SupportWorkflowStub();var vm=new SupportViewModel(workflow,new ClipboardStub());await vm.ActivateAsync();
        await vm.PrepareErrorCommand.ExecuteAsync();Assert.NotNull(vm.CurrentDraft);
        workflow.Error=new(SupportActionStatus.InvalidInput,"invalid");await vm.PrepareErrorCommand.ExecuteAsync();
        Assert.Null(vm.CurrentDraft);Assert.Equal("invalid",vm.StatusText);
    }
}

