using SysTools.Business.Barcodes;
using SysTools.Entities.Barcodes;

namespace SysTools.Barcodes.Tests.Compatibility;

public sealed class BarcodeClassificationMatrixTests
{
    public static IEnumerable<object[]> ClassificationCases()
    {
        var numericCases = new List<(string Value, BarcodeType Type)>();
        for (var index = 0; index < 20; index++)
        {
            numericCases.Add((WithCheckDigit($"{index:D7}", [3, 1, 3, 1, 3, 1, 3]), BarcodeType.Ean8));
            numericCases.Add((WithCheckDigit($"1000000000{index:D2}", [1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3]), BarcodeType.Ean13));
            numericCases.Add((WithCheckDigit($"036000291{index:D2}", [3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3]), BarcodeType.UpcA));
        }

        foreach (var item in numericCases)
        {
            yield return [item.Value, item.Type];
        }

        foreach (var item in numericCases.Take(20))
        {
            yield return [InvalidateCheckDigit(item.Value), BarcodeType.Code128];
        }

        for (var index = 0; index < 20; index++)
        {
            yield return [$"SKU-{index:D3}", BarcodeType.Code128];
        }
    }

    [Fact]
    public void Matrix_contains_at_least_one_hundred_cases_with_required_distribution()
    {
        var cases = ClassificationCases().Select(item => (BarcodeType)item[1]).ToArray();
        Assert.True(cases.Length >= 100);
        Assert.True(cases.Count(type => type == BarcodeType.Ean8) >= 20);
        Assert.True(cases.Count(type => type == BarcodeType.Ean13) >= 20);
        Assert.True(cases.Count(type => type == BarcodeType.UpcA) >= 20);
        Assert.True(cases.Count(type => type == BarcodeType.Code128) >= 20);
    }

    [Theory]
    [MemberData(nameof(ClassificationCases))]
    public void DetectType_matches_documented_rules(string value, BarcodeType expected)
    {
        var classifier = new BarcodeClassifier();
        Assert.Equal(expected, classifier.DetectType(value));
    }

    private static string WithCheckDigit(string body, int[] weights)
    {
        var sum = body.Select((character, index) => (character - '0') * weights[index]).Sum();
        return body + ((10 - (sum % 10)) % 10);
    }

    private static string InvalidateCheckDigit(string value) =>
        value[..^1] + (value[^1] == '9' ? '0' : (char)(value[^1] + 1));
}
