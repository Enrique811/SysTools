using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.TestDoubles;

internal static class ProductTestFactory
{
    internal static Product Create(
        int id = 1,
        string barcode = "000123",
        decimal? priceWithTax = 123.45m,
        string description = "Árbol de prueba",
        string? presentation = "Pieza",
        string stock = "7") =>
        new(id, barcode, description, presentation, priceWithTax, stock);
}
