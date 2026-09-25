using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

public sealed class ConfigurationEditorSecureEditTests
{
    [Fact]
    public async Task Fifty_operational_edits_preserve_stored_password_without_retest()
    {
        var config = new EditorConfigurationServiceStub();
        var workflow = EditorWorkflowFactory.Create(config);
        await workflow.OpenAsync();

        for (var index = 0; index < 50; index++)
        {
            var draft = EditorWorkflowFactory.Draft() with { Informacion = $"Texto {index}" };
            var result = await workflow.SaveAsync(draft, string.Empty, index, null, LicenseChange.KeepExisting);
            Assert.True(result.IsSaved);
            Assert.Equal("stored-secret", config.Saved!.Password);
        }

        Assert.Equal(50, config.SaveCalls);
    }
}
