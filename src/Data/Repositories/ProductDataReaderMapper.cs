using System.Data.Common;
using System.Globalization;
using SysTools.Entities.Products;

namespace SysTools.Data.Repositories;

internal static class ProductDataReaderMapper
{
    internal static Product Map(DbDataReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var id = Convert.ToInt32(ReadRequired(reader, "CODIGO"), CultureInfo.InvariantCulture);
        var barcode = Convert.ToString(
            ReadRequired(reader, "CODIGO_BARRAS"),
            CultureInfo.InvariantCulture) ?? string.Empty;
        var description = ReadOptionalText(reader, "DESCRIPCION") ?? string.Empty;
        var presentation = ReadOptionalText(reader, "PRESENTACION");
        var priceValue = ReadOptional(reader, "PRECIO_IVA");
        var stockValue = ReadOptional(reader, "STOCK");

        decimal? priceWithTax = priceValue is null
            ? null
            : Convert.ToDecimal(priceValue, CultureInfo.InvariantCulture);
        var stock = stockValue is null
            ? Product.StockNotRegistered
            : Convert.ToString(stockValue, CultureInfo.InvariantCulture);

        return new Product(
            id,
            barcode,
            description,
            presentation,
            priceWithTax,
            stock);
    }

    private static object ReadRequired(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal)
            ? throw new InvalidDataException($"La columna requerida {column} no contiene un valor.")
            : reader.GetValue(ordinal);
    }

    private static object? ReadOptional(DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
    }

    private static string? ReadOptionalText(DbDataReader reader, string column)
    {
        var value = ReadOptional(reader, column);
        return value is null ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
    }
}
