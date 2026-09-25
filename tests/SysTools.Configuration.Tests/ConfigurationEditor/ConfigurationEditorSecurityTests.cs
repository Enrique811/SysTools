using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

public sealed class ConfigurationEditorSecurityTests
{
    [Fact]
    public void Public_editor_entities_do_not_expose_password_or_license_path()
    {
        var publicTypes = new[]
        {
            typeof(ConfigurationDraft), typeof(ConfigurationEditorSnapshot),
            typeof(ConfigurationEditorSaveResult), typeof(ConnectionEditorResult), typeof(LicenseSummary)
        };
        foreach (var type in publicTypes)
        {
            Assert.DoesNotContain(type.GetProperties(), property =>
                (property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase) && property.PropertyType == typeof(string))
                || property.Name.Contains("LicensePath", StringComparison.OrdinalIgnoreCase));
        }
    }
}
