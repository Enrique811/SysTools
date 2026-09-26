using SysTools.Entities.Support;
using SysTools.Presentation.Modules.Support;

namespace SysTools.Presentation.Tests.Support;

public sealed class SupportLicenseViewModelTests
{
    [Fact] public async Task Activation_copy_prepare_and_open_are_explicit()
    {
        var workflow=new SupportWorkflowStub();var clipboard=new ClipboardStub();var vm=new SupportViewModel(workflow,clipboard);
        await vm.ActivateAsync();
        Assert.True(vm.HardwareAvailable);Assert.Equal(workflow.Initialization.HardwareId,vm.HardwareId);
        vm.CopyHardwareIdCommand.Execute(null);Assert.Equal(vm.HardwareId,clipboard.Value);
        await vm.PrepareLicenseCommand.ExecuteAsync();Assert.NotNull(vm.CurrentDraft);
        vm.SelectedChannel=SupportChannel.GmailWeb;await vm.OpenDraftCommand.ExecuteAsync();Assert.Equal(1,workflow.OpenCalls);
    }
    [Fact] public async Task Missing_hardware_disables_copy_and_license()
    {
        var vm=new SupportViewModel(new SupportWorkflowStub{Initialization=new(false,null,"missing")},new ClipboardStub());
        await vm.ActivateAsync();
        Assert.False(vm.CopyHardwareIdCommand.CanExecute(null));Assert.False(vm.PrepareLicenseCommand.CanExecute(null));
    }
    [Fact] public async Task Deactivation_invalidates_workflow()
    {
        var workflow=new SupportWorkflowStub();var vm=new SupportViewModel(workflow,new ClipboardStub());await vm.ActivateAsync();vm.Deactivate();
        Assert.Equal(1,workflow.InvalidateCalls);Assert.Null(vm.CurrentDraft);
    }
    [Fact] public async Task Non_cooperative_late_response_does_not_publish_after_deactivation()
    {
        var completion=new TaskCompletionSource<SupportActionResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workflow=new SupportWorkflowStub{LicenseHandler=_=>completion.Task};var vm=new SupportViewModel(workflow,new ClipboardStub());await vm.ActivateAsync();
        var pending=vm.PrepareLicenseCommand.ExecuteAsync();vm.Deactivate();completion.SetResult(SupportWorkflowStub.Ready(SupportMailKind.LicenseRequest));await pending;
        Assert.Null(vm.CurrentDraft);Assert.False(vm.IsBusy);
    }
}
