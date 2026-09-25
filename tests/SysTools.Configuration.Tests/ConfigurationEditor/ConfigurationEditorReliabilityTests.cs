using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

public sealed class ConfigurationEditorReliabilityTests
{
    [Fact]
    public async Task One_hundred_tests_and_saves_have_exact_call_counts_and_no_crossed_proofs()
    {
        var config = new EditorConfigurationServiceStub();
        var connection = new EditorConnectionStub();
        var workflow = EditorWorkflowFactory.Create(config, connection);
        await workflow.OpenAsync();

        for (var index = 0; index < 100; index++)
        {
            var draft = EditorWorkflowFactory.Draft() with { IpEmpresa = $"server-{index}" };
            var proof = await workflow.TestConnectionAsync(draft, string.Empty, index);
            var saved = await workflow.SaveAsync(draft, string.Empty, index, proof.ProofToken, LicenseChange.KeepExisting);
            Assert.True(saved.IsSaved);
            Assert.Equal($"server-{index}", config.Saved!.IpEmpresa);
        }

        Assert.Equal(100, connection.Calls);
        Assert.Equal(100, config.SaveCalls);
    }
}
