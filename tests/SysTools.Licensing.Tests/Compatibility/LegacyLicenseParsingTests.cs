using SysTools.Business.Licensing;

namespace SysTools.Licensing.Tests.Compatibility;

public sealed class LegacyLicenseParsingTests
{
    [Fact]
    public void Parses_required_strings_and_ignores_additional_fields()
    {
        var json = LicenseJson.Valid(
            extra: "\"cliente\":\"demo\",",
            uuid: " 00112233-4455-6677-8899-aabbccddeeff ");

        var result = LicenseDocumentParser.Parse(json);

        Assert.True(result.Succeeded);
        Assert.Equal("00112233-4455-6677-8899-AABBCCDDEEFF", result.Document!.HardwareId);
        Assert.Equal(new DateTime(2026, 1, 1), result.Document.ValidFrom);
        Assert.Equal(new DateTime(2026, 12, 31, 23, 59, 59), result.Document.ValidUntil);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("{\"uuid\":")]
    public void Rejects_invalid_json_root(string json)
    {
        var result = LicenseDocumentParser.Parse(json);

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseParseFailure.InvalidJson, result.Failure);
    }

    [Theory]
    [InlineData("{\"inicio\":\"2026-01-01 00:00:00\",\"fin\":\"2026-12-31 23:59:59\",\"firma\":\"AQID\"}")]
    [InlineData("{\"uuid\":12,\"inicio\":\"2026-01-01 00:00:00\",\"fin\":\"2026-12-31 23:59:59\",\"firma\":\"AQID\"}")]
    [InlineData("{\"uuid\":\"bad\",\"inicio\":\"2026-01-01 00:00:00\",\"fin\":\"2026-12-31 23:59:59\",\"firma\":\"AQID\"}")]
    [InlineData("{\"uuid\":\"00112233-4455-6677-8899-AABBCCDDEEFF\",\"inicio\":\"2026-02-30 00:00:00\",\"fin\":\"2026-12-31 23:59:59\",\"firma\":\"AQID\"}")]
    [InlineData("{\"uuid\":\"00112233-4455-6677-8899-AABBCCDDEEFF\",\"inicio\":\"2026-12-31 00:00:00\",\"fin\":\"2026-01-01 23:59:59\",\"firma\":\"AQID\"}")]
    [InlineData("{\"uuid\":\"00112233-4455-6677-8899-AABBCCDDEEFF\",\"uuid\":\"00112233-4455-6677-8899-AABBCCDDEEFF\",\"inicio\":\"2026-01-01 00:00:00\",\"fin\":\"2026-12-31 23:59:59\",\"firma\":\"AQID\"}")]
    public void Rejects_missing_wrong_duplicate_or_out_of_range_fields(string json)
    {
        var result = LicenseDocumentParser.Parse(json);

        Assert.False(result.Succeeded);
        Assert.Equal(LicenseParseFailure.InvalidFields, result.Failure);
    }
}

internal static class LicenseJson
{
    internal const string HardwareId = "00112233-4455-6677-8899-AABBCCDDEEFF";

    internal static string Valid(
        string signature = "AQID",
        string uuid = HardwareId,
        string start = "2026-01-01 13:45:21",
        string end = "2026-12-31 01:02:03",
        string extra = "") =>
        $$"""
        {
          {{extra}}
          "uuid": "{{uuid}}",
          "inicio": "{{start}}",
          "fin": "{{end}}",
          "firma": "{{signature}}"
        }
        """;
}
