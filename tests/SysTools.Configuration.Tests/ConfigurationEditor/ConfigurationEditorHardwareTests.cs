using SysTools.Configuration.Tests.TestDoubles;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

public sealed class ConfigurationEditorHardwareTests
{
    [Fact]
    public async Task Hardware_identifier_is_available_in_snapshot_but_not_written_to_logs()
    {
        const string identifier = "SENSITIVE-HARDWARE-IDENTIFIER";
        var hardware = new EditorHardwareStub { Value = identifier };
        var logger = new ListLogger<SysTools.Business.ConfigurationEditor.ConfigurationEditorWorkflow>();
        var workflow = EditorWorkflowFactory.Create(
            new EditorConfigurationServiceStub(),
            hardware: hardware,
            logger: logger);

        var snapshot = await workflow.OpenAsync();

        Assert.True(snapshot.Hardware.IsAvailable);
        Assert.Equal(identifier, snapshot.Hardware.Value);
        Assert.DoesNotContain(identifier, string.Join(Environment.NewLine, logger.Messages), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Hardware_failure_is_reduced_to_safe_unavailable_state()
    {
        var hardware = new EditorHardwareStub { Failure = new InvalidOperationException("C:\\private\\hardware") };
        var workflow = EditorWorkflowFactory.Create(new EditorConfigurationServiceStub(), hardware: hardware);

        var snapshot = await workflow.OpenAsync();

        Assert.False(snapshot.Hardware.IsAvailable);
        Assert.Null(snapshot.Hardware.Value);
    }
}
