using SysTools.Entities.Configuration;
using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

public sealed class ConfigurationEditorInitialTests
{
    [Fact]
    public async Task Twenty_initial_flows_require_an_exact_test_and_then_save_once()
    {
        for (var index = 0; index < 20; index++)
        {
            var config = new EditorConfigurationServiceStub
            {
                LoadResult = new ConfigurationLoadResult(
                    ConfigurationLoadStatus.DefaultCreated,
                    AppConfiguration.CreateDefault(),
                    new ConfigurationValidationResult(true, false))
            };
            var workflow = EditorWorkflowFactory.Create(config);
            await workflow.OpenAsync();
            var draft = EditorWorkflowFactory.Draft() with { Informacion = $"initial-{index}" };

            var rejected = await workflow.SaveAsync(draft, "secret", index, null, LicenseChange.KeepExisting);
            var proof = await workflow.TestConnectionAsync(draft, "secret", index);
            var saved = await workflow.SaveAsync(draft, "secret", index, proof.ProofToken, LicenseChange.KeepExisting);

            Assert.Equal(ConfigurationEditorSaveStatus.TestRequired, rejected.Status);
            Assert.True(saved.IsSaved);
            Assert.Equal(1, config.SaveCalls);
        }
    }

    [Fact]
    public async Task Open_returns_safe_snapshot_and_unavailable_persisted_options()
    {
        var config = new EditorConfigurationServiceStub();
        config.LoadResult = EditorConfigurationServiceStub.Loaded("SECRET_PASSWORD");
        var workflow = EditorWorkflowFactory.Create(config);

        var snapshot = await workflow.OpenAsync();

        Assert.True(snapshot.HasStoredPassword);
        Assert.True(snapshot.HasStoredLicense);
        Assert.DoesNotContain(snapshot.GetType().GetProperties(), p => p.PropertyType == typeof(AppConfiguration));
        Assert.DoesNotContain("SECRET_PASSWORD", snapshot.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Initial_save_requires_exact_successful_test()
    {
        var config = new EditorConfigurationServiceStub
        {
            LoadResult = new ConfigurationLoadResult(ConfigurationLoadStatus.DefaultCreated,
                AppConfiguration.CreateDefault(), new ConfigurationValidationResult(true, false))
        };
        var connection = new EditorConnectionStub();
        var workflow = EditorWorkflowFactory.Create(config, connection);
        await workflow.OpenAsync();
        var draft = EditorWorkflowFactory.Draft();

        var before = await workflow.SaveAsync(draft, "new-secret", 0, null, LicenseChange.KeepExisting);
        var tested = await workflow.TestConnectionAsync(draft, "new-secret", 0);
        var after = await workflow.SaveAsync(draft, "new-secret", 0, tested.ProofToken, LicenseChange.KeepExisting);

        Assert.Equal(ConfigurationEditorSaveStatus.TestRequired, before.Status);
        Assert.True(after.IsSaved);
        Assert.Equal("new-secret", config.Saved!.Password);
        Assert.Equal(1, config.SaveCalls);
    }

    [Fact]
    public async Task Changed_connection_rejects_old_proof_even_if_revision_is_reused()
    {
        var config = new EditorConfigurationServiceStub
        {
            LoadResult = new ConfigurationLoadResult(ConfigurationLoadStatus.DefaultCreated,
                AppConfiguration.CreateDefault(), new ConfigurationValidationResult(true, false))
        };
        var workflow = EditorWorkflowFactory.Create(config);
        await workflow.OpenAsync();
        var original = EditorWorkflowFactory.Draft();
        var proof = await workflow.TestConnectionAsync(original, "secret", 1);
        var changed = original with { IpEmpresa = "other" };

        var result = await workflow.SaveAsync(changed, "secret", 1, proof.ProofToken, LicenseChange.KeepExisting);

        Assert.Equal(ConfigurationEditorSaveStatus.TestRequired, result.Status);
        Assert.Equal(0, config.SaveCalls);
    }
}
