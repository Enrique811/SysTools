using SysTools.Entities.Barcodes;

namespace SysTools.Business.Barcodes;

public sealed class BarcodeClassifier
{
    private static readonly int[] Ean8Weights = [3, 1, 3, 1, 3, 1, 3];
    private static readonly int[] Ean13Weights = [1, 3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3];
    private static readonly int[] UpcAWeights = [3, 1, 3, 1, 3, 1, 3, 1, 3, 1, 3];

    public BarcodeType DetectType(string? input)
    {
        var value = Normalize(input);
        if (!IsAsciiDigits(value))
        {
            return BarcodeType.Code128;
        }

        return value.Length switch
        {
            8 when HasValidCheckDigit(value, Ean8Weights) => BarcodeType.Ean8,
            12 when HasValidCheckDigit(value, UpcAWeights) => BarcodeType.UpcA,
            13 when HasValidCheckDigit(value, Ean13Weights) => BarcodeType.Ean13,
            _ => BarcodeType.Code128
        };
    }

    internal static string Normalize(string? input)
    {
        if (input is null)
        {
            return string.Empty;
        }

        var start = 0;
        while (start < input.Length && input[start] <= '\u0020')
        {
            start++;
        }

        var end = input.Length - 1;
        while (end >= start && input[end] <= '\u0020')
        {
            end--;
        }

        return start > end ? string.Empty : input[start..(end + 1)];
    }

    private static bool IsAsciiDigits(string value) =>
        value.Length > 0 && value.All(character => character is >= '0' and <= '9');

    private static bool HasValidCheckDigit(string value, IReadOnlyList<int> weights)
    {
        var sum = 0;
        for (var index = 0; index < weights.Count; index++)
        {
            sum += (value[index] - '0') * weights[index];
        }

        var expected = (10 - (sum % 10)) % 10;
        return expected == value[^1] - '0';
    }
}
