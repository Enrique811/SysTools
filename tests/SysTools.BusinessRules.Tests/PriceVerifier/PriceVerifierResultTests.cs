using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;
using SysTools.Entities.PriceVerifier;
using SysTools.Entities.Products;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierResultTests
{
    [Fact]
    public void Preparation_ready_requires_successful_connection_and_valid_license()
    {
        var result = new PriceVerifierPreparationResult(
            PriceVerifierPreparationStatus.Ready,
            "Listo.",
            ConnectionTestStatus.Success,
            LicenseValidationStatus.Valid,
            "Informacion");

        Assert.True(result.IsReady);
        Assert.Equal("Informacion", result.AdditionalInformation);
        Assert.Throws<ArgumentException>(() => new PriceVerifierPreparationResult(
            PriceVerifierPreparationStatus.Ready,
            "Listo.",
            ConnectionTestStatus.Timeout,
            LicenseValidationStatus.Valid));
    }

    [Theory]
    [InlineData(PriceVerifierPreparationStatus.ConfigurationUnavailable, null, null)]
    [InlineData(PriceVerifierPreparationStatus.ConnectionUnavailable, ConnectionTestStatus.Timeout, null)]
    [InlineData(PriceVerifierPreparationStatus.LicenseUnavailable, ConnectionTestStatus.Success, LicenseValidationStatus.Expired)]
    public void Preparation_blocked_states_enforce_their_boundary(
        PriceVerifierPreparationStatus status,
        ConnectionTestStatus? connection,
        LicenseValidationStatus? license)
    {
        var result = new PriceVerifierPreparationResult(status, "No disponible.", connection, license);
        Assert.False(result.IsReady);
    }

    [Fact]
    public void Preparation_rejects_undefined_status_or_empty_message()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PriceVerifierPreparationResult(
            (PriceVerifierPreparationStatus)999,
            "Mensaje"));
        Assert.Throws<ArgumentException>(() => new PriceVerifierPreparationResult(
            PriceVerifierPreparationStatus.ConfigurationUnavailable,
            " "));
    }

    [Fact]
    public void Lookup_success_requires_product_and_consistent_price()
    {
        var product = new Product(1, "000123", "Producto", "Pieza", 12.5m, "3");
        var result = new PriceVerifierLookupResult(
            PriceVerifierLookupStatus.Success,
            "Encontrado.",
            product,
            "$12.50");

        Assert.True(result.IsSuccess);
        Assert.Equal("000123", result.Product!.Barcode);
        Assert.Equal("$12.50", result.FormattedPrice);
        Assert.Throws<ArgumentException>(() => new PriceVerifierLookupResult(
            PriceVerifierLookupStatus.Success,
            "Encontrado."));
    }

    [Theory]
    [InlineData(PriceVerifierLookupStatus.MissingInput)]
    [InlineData(PriceVerifierLookupStatus.InputTooLong)]
    [InlineData(PriceVerifierLookupStatus.NotFound)]
    [InlineData(PriceVerifierLookupStatus.OperationalFailure)]
    public void Lookup_non_success_never_contains_commercial_data(PriceVerifierLookupStatus status)
    {
        var result = new PriceVerifierLookupResult(status, "Mensaje seguro.");
        Assert.False(result.IsSuccess);
        Assert.Null(result.Product);
        Assert.Null(result.FormattedPrice);
    }
}

