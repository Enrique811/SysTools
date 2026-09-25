using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

public sealed class ConfigurationEditorValidationTests
{
    [Fact]
    public async Task Save_returns_all_invalid_fields_in_one_evaluation_and_does_not_write()
    {
        var configuration = new EditorConfigurationServiceStub();
        var workflow = EditorWorkflowFactory.Create(configuration);
        await workflow.OpenAsync();
        var invalid = new ConfigurationDraft
        {
            IpEmpresa = string.Empty,
            RutaEmpresa = string.Empty,
            Usuario = string.Empty,
            Ambiente = "invalid",
            FormatoPrecio = "invalid",
            Columnas = 9
        };

        var result = await workflow.SaveAsync(invalid, string.Empty, 1, null, LicenseChange.KeepExisting);

        Assert.Equal(ConfigurationEditorSaveStatus.ValidationFailed, result.Status);
        Assert.Equal(
            ["ambiente", "columnas", "formatoPrecio", "ipEmpresa", "rutaEmpresa", "usuario"],
            result.Issues.Select(issue => issue.Field!).Order(StringComparer.Ordinal).ToArray());
        Assert.Equal(0, configuration.SaveCalls);
    }
}
