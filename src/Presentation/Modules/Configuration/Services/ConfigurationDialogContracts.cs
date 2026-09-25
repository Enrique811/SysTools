namespace SysTools.Presentation.Modules.Configuration.Services;

public enum ConfigurationDialogMode { Initial, Edit }
public enum ConfigurationDialogOutcome { Saved, Canceled, Unavailable }
public sealed record ConfigurationDialogResult(ConfigurationDialogOutcome Outcome)
{
    public bool IsSaved => Outcome == ConfigurationDialogOutcome.Saved;
}

public interface IConfigurationDialogService
{
    Task<ConfigurationDialogResult> ShowDialogAsync(ConfigurationDialogMode mode, CancellationToken cancellationToken = default);
    Task<ConfigurationDialogResult> ShowIfRequiredAsync(CancellationToken cancellationToken = default);
    void CloseActive();
}

public interface IConfigurationFilePicker
{
    string? PickDatabase();
    string? PickLicense();
}

public interface IClipboardService { bool TrySetText(string value); }
public interface IConfirmationService
{
    bool ConfirmDiscard();
    bool ConfirmRecovery();
}
