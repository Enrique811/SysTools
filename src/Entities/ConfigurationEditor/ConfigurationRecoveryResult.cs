namespace SysTools.Entities.ConfigurationEditor;

public enum ConfigurationRecoveryStatus { Recovered, NotConfirmed, ProtectionFailure, AccessDenied, StorageFailure, Canceled }
public sealed record ConfigurationRecoveryResult(ConfigurationRecoveryStatus Status);
