using SysTools.Entities.ConfigurationEditor;
using SysTools.Entities.Licensing;

namespace SysTools.Configuration.Tests.ConfigurationEditor;

public sealed class ConfigurationEditorLicenseTests
{
    public static IEnumerable<object[]> LicenseStatuses() =>
        Enum.GetValues<LicenseValidationStatus>().Select(status => new object[] { status });

    [Theory]
    [MemberData(nameof(LicenseStatuses))]
    public async Task Every_license_status_is_reported_without_exposing_the_candidate_path(
        LicenseValidationStatus status)
    {
        var result = status == LicenseValidationStatus.Valid
            ? new LicenseValidationResult(
                status,
                LicenseIssuer.Developer,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 12, 31),
                new DateTime(2026, 9, 24))
            : new LicenseValidationResult(status);
        var license = new EditorLicenseStub { Result = result };
        var workflow = EditorWorkflowFactory.Create(new EditorConfigurationServiceStub(), license: license);
        await workflow.OpenAsync();

        var actual = await workflow.ValidateLicenseAsync(
            EditorWorkflowFactory.Draft(),
            string.Empty,
            "C:\\private\\SENSITIVE_LICENSE.lic",
            1);

        Assert.Equal(status, actual.Summary.Status);
        Assert.DoesNotContain("SENSITIVE_LICENSE", actual.Summary.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Valid_license_can_replace_reference_without_exposing_path_in_summary()
    {
        var license = new EditorLicenseStub
        {
            Result = new LicenseValidationResult(
                LicenseValidationStatus.Valid,
                LicenseIssuer.Developer,
                new DateTime(2026, 1, 1),
                new DateTime(2026, 12, 31),
                new DateTime(2026, 9, 24))
        };
        var config = new EditorConfigurationServiceStub();
        var workflow = EditorWorkflowFactory.Create(config, license: license);
        await workflow.OpenAsync();
        var draft = EditorWorkflowFactory.Draft();

        var validation = await workflow.ValidateLicenseAsync(draft, string.Empty, "C:\\private\\new.lic", 4);
        var saved = await workflow.SaveAsync(draft, string.Empty, 4, null, LicenseChange.ReplaceValidated);

        Assert.True(validation.Summary.IsValid);
        Assert.DoesNotContain("private", validation.Summary.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.True(saved.IsSaved);
        Assert.Equal("C:\\private\\new.lic", config.Saved!.Licencia);
    }

    [Fact]
    public async Task Invalid_license_does_not_replace_existing_reference()
    {
        var config = new EditorConfigurationServiceStub();
        var workflow = EditorWorkflowFactory.Create(config);
        await workflow.OpenAsync();
        await workflow.ValidateLicenseAsync(EditorWorkflowFactory.Draft(), string.Empty, "invalid.lic", 1);

        await workflow.SaveAsync(EditorWorkflowFactory.Draft(), string.Empty, 1, null, LicenseChange.ReplaceValidated);

        Assert.Equal("old.lic", config.Saved!.Licencia);
    }

    [Fact]
    public async Task Explicit_clear_removes_existing_license_reference()
    {
        var config = new EditorConfigurationServiceStub();
        var workflow = EditorWorkflowFactory.Create(config);
        await workflow.OpenAsync();

        var result = await workflow.SaveAsync(
            EditorWorkflowFactory.Draft(), string.Empty, 1, null, LicenseChange.ClearExplicitly);

        Assert.True(result.IsSaved);
        Assert.Equal(string.Empty, config.Saved!.Licencia);
    }
}
