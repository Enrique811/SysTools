using SysTools.Entities.Configuration;
using SysTools.Entities.ConfigurationEditor;

namespace SysTools.Business.ConfigurationEditor;

public interface IConfigurationEditorWorkflow
{
    Task<ConfigurationEditorSnapshot> OpenAsync(CancellationToken cancellationToken = default);
    ConfigurationValidationResult Validate(ConfigurationDraft draft, bool hasNewPassword);
    Task<ConnectionEditorResult> TestConnectionAsync(ConfigurationDraft draft, string newPassword, long revision, CancellationToken cancellationToken = default);
    Task<LicenseEditorResult> ValidateLicenseAsync(ConfigurationDraft draft, string newPassword, string candidatePath, long revision, CancellationToken cancellationToken = default);
    Task<ConfigurationEditorSaveResult> SaveAsync(ConfigurationDraft draft, string newPassword, long revision, string? proofToken, LicenseChange licenseChange, CancellationToken cancellationToken = default);
    Task<ConfigurationEditorSaveResult> RecoverAsync(ConfigurationDraft draft, string newPassword, long revision, string? proofToken, bool confirmed, CancellationToken cancellationToken = default);
    void Invalidate();
}
