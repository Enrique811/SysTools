namespace SysTools.Entities.Licensing;

public enum LicenseValidationStatus
{
    Valid = 0,
    MissingInput = 1,
    SourceUnavailable = 2,
    InvalidJson = 3,
    InvalidFields = 4,
    InvalidSignature = 5,
    HardwareIdUnavailable = 6,
    HardwareMismatch = 7,
    NotYetValid = 8,
    Expired = 9,
    ServerTimeUnavailable = 10
}
