using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SysTools.Business.Licensing;

internal enum LicenseParseFailure
{
    None = 0,
    InvalidJson = 1,
    InvalidFields = 2
}

internal sealed record ParsedLicenseDocument(
    string HardwareId,
    DateTime ValidFrom,
    DateTime ValidUntil,
    byte[] Signature,
    byte[] SignedPayload);

internal sealed record LicenseDocumentParseResult(
    ParsedLicenseDocument? Document,
    LicenseParseFailure Failure)
{
    internal bool Succeeded => Document is not null && Failure == LicenseParseFailure.None;

    internal static LicenseDocumentParseResult Success(ParsedLicenseDocument document) =>
        new(document, LicenseParseFailure.None);

    internal static LicenseDocumentParseResult Failed(LicenseParseFailure failure) =>
        new(null, failure);
}

internal static partial class LicenseDocumentParser
{
    private const string DateFormat = "yyyy-MM-dd HH:mm:ss";
    private static readonly string[] RequiredNames = ["uuid", "inicio", "fin", "firma"];

    internal static LicenseDocumentParseResult Parse(string content)
    {
        try
        {
            using var json = JsonDocument.Parse(content);
            if (json.RootElement.ValueKind != JsonValueKind.Object)
            {
                return LicenseDocumentParseResult.Failed(LicenseParseFailure.InvalidJson);
            }

            var values = new Dictionary<string, string?>(StringComparer.Ordinal);
            foreach (var property in json.RootElement.EnumerateObject())
            {
                if (!RequiredNames.Contains(property.Name, StringComparer.Ordinal))
                {
                    continue;
                }

                if (values.ContainsKey(property.Name)
                    || property.Value.ValueKind != JsonValueKind.String)
                {
                    return LicenseDocumentParseResult.Failed(LicenseParseFailure.InvalidFields);
                }

                values[property.Name] = property.Value.GetString();
            }

            if (RequiredNames.Any(name =>
                    !values.TryGetValue(name, out var value)
                    || string.IsNullOrWhiteSpace(value)))
            {
                return LicenseDocumentParseResult.Failed(LicenseParseFailure.InvalidFields);
            }

            var hardwareId = values["uuid"]!.Trim().ToUpperInvariant();
            if (!HardwareIdPattern().IsMatch(hardwareId)
                || !TryParseDate(values["inicio"]!, out var parsedStart)
                || !TryParseDate(values["fin"]!, out var parsedEnd)
                || parsedStart > parsedEnd)
            {
                return LicenseDocumentParseResult.Failed(LicenseParseFailure.InvalidFields);
            }

            byte[] signature;
            try
            {
                signature = Convert.FromBase64String(values["firma"]!.Trim());
            }
            catch (FormatException)
            {
                return LicenseDocumentParseResult.Failed(LicenseParseFailure.InvalidFields);
            }

            if (signature.Length == 0)
            {
                return LicenseDocumentParseResult.Failed(LicenseParseFailure.InvalidFields);
            }

            DateTime validUntil;
            try
            {
                validUntil = parsedEnd.Date.AddDays(1).AddSeconds(-1);
            }
            catch (ArgumentOutOfRangeException)
            {
                return LicenseDocumentParseResult.Failed(LicenseParseFailure.InvalidFields);
            }

            var validFrom = parsedStart.Date;
            var payload = string.Create(
                CultureInfo.InvariantCulture,
                $"uuid={hardwareId}\ninicio={validFrom:yyyy-MM-dd HH:mm:ss}\nfin={validUntil:yyyy-MM-dd HH:mm:ss}");

            return LicenseDocumentParseResult.Success(new ParsedLicenseDocument(
                hardwareId,
                DateTime.SpecifyKind(validFrom, DateTimeKind.Unspecified),
                DateTime.SpecifyKind(validUntil, DateTimeKind.Unspecified),
                signature,
                Encoding.UTF8.GetBytes(payload)));
        }
        catch (JsonException)
        {
            return LicenseDocumentParseResult.Failed(LicenseParseFailure.InvalidJson);
        }
        catch (ArgumentException)
        {
            return LicenseDocumentParseResult.Failed(LicenseParseFailure.InvalidJson);
        }
    }

    private static bool TryParseDate(string value, out DateTime date) =>
        DateTime.TryParseExact(
            value.Trim(),
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);

    [GeneratedRegex("^[0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12}$", RegexOptions.CultureInvariant)]
    private static partial Regex HardwareIdPattern();
}
