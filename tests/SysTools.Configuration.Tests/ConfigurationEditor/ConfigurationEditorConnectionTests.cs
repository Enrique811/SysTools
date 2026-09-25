using SysTools.Entities.Connection;
using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

public sealed class ConfigurationEditorConnectionTests
{
    [Theory]
    [InlineData(ConnectionTestStatus.Success)]
    [InlineData(ConnectionTestStatus.InvalidConfiguration)]
    [InlineData(ConnectionTestStatus.AuthenticationFailed)]
    [InlineData(ConnectionTestStatus.ServerUnavailable)]
    [InlineData(ConnectionTestStatus.DatabaseUnavailable)]
    [InlineData(ConnectionTestStatus.Timeout)]
    [InlineData(ConnectionTestStatus.Canceled)]
    [InlineData(ConnectionTestStatus.UnexpectedFailure)]
    public async Task Every_connection_category_is_preserved_as_a_controlled_result(ConnectionTestStatus status)
    {
        var connection = new EditorConnectionStub { Status = status };
        var workflow = EditorWorkflowFactory.Create(new EditorConfigurationServiceStub(), connection);
        await workflow.OpenAsync();

        var result = await workflow.TestConnectionAsync(EditorWorkflowFactory.Draft(), string.Empty, 1);

        Assert.Equal(status, result.Result.Status);
        Assert.Equal(status == ConnectionTestStatus.Success, result.ProofToken is not null);
        Assert.False(string.IsNullOrWhiteSpace(result.Result.Message));
    }

    [Fact]
    public async Task Change_and_reversion_with_a_new_revision_invalidates_old_proof()
    {
        var config = new EditorConfigurationServiceStub
        {
            LoadResult = new SysTools.Entities.Configuration.ConfigurationLoadResult(
                SysTools.Entities.Configuration.ConfigurationLoadStatus.DefaultCreated,
                SysTools.Entities.Configuration.AppConfiguration.CreateDefault(),
                new SysTools.Entities.Configuration.ConfigurationValidationResult(true, false))
        };
        var workflow = EditorWorkflowFactory.Create(config);
        await workflow.OpenAsync();
        var draft = EditorWorkflowFactory.Draft();
        var proof = await workflow.TestConnectionAsync(draft, "secret", 1);

        var result = await workflow.SaveAsync(draft, "secret", 3, proof.ProofToken, LicenseChange.KeepExisting);

        Assert.Equal(ConfigurationEditorSaveStatus.TestRequired, result.Status);
        Assert.Equal(0, config.SaveCalls);
    }

    [Fact]
    public async Task Result_from_an_invalidated_epoch_cannot_create_a_proof()
    {
        var completion = new TaskCompletionSource<ConnectionTestResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var connection = new EditorConnectionStub { Handler = (_, _) => completion.Task };
        var workflow = EditorWorkflowFactory.Create(new EditorConfigurationServiceStub(), connection);
        await workflow.OpenAsync();

        var operation = workflow.TestConnectionAsync(EditorWorkflowFactory.Draft(), string.Empty, 1);
        workflow.Invalidate();
        completion.SetResult(new ConnectionTestResult(ConnectionTestStatus.Success, "Disponible", TimeSpan.Zero));
        var result = await operation;

        Assert.Null(result.ProofToken);
    }
}
