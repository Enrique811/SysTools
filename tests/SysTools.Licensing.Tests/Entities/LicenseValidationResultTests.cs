using System.Reflection;
using SysTools.Entities.Licensing;

namespace SysTools.Licensing.Tests.Entities;

public sealed class LicenseValidationResultTests
{
    [Fact]
    public void Valid_result_requires_issuer_dates_and_server_time()
    {
        var from = new DateTime(2026, 1, 1);
        var until = new DateTime(2026, 12, 31, 23, 59, 59);

        var result = new LicenseValidationResult(
            LicenseValidationStatus.Valid,
            LicenseIssuer.Developer,
            from,
            until,
            from);

        Assert.True(result.IsValid);
        Assert.Equal("La licencia es valida.", result.Message);
        Assert.Throws<ArgumentException>(() => new LicenseValidationResult(
            LicenseValidationStatus.Valid,
            LicenseIssuer.None,
            from,
            until,
            from));
    }

    [Theory]
    [InlineData(LicenseValidationStatus.MissingInput, "No se configuro la licencia.")]
    [InlineData(LicenseValidationStatus.SourceUnavailable, "No fue posible leer el archivo de licencia.")]
    [InlineData(LicenseValidationStatus.InvalidJson, "El contenido de la licencia no es JSON valido.")]
    [InlineData(LicenseValidationStatus.InvalidFields, "La licencia contiene campos invalidos.")]
    [InlineData(LicenseValidationStatus.InvalidSignature, "La firma de la licencia no es valida.")]
    [InlineData(LicenseValidationStatus.HardwareIdUnavailable, "No fue posible identificar este equipo.")]
    [InlineData(LicenseValidationStatus.HardwareMismatch, "La licencia no corresponde a este equipo.")]
    [InlineData(LicenseValidationStatus.NotYetValid, "La licencia aun no ha iniciado.")]
    [InlineData(LicenseValidationStatus.Expired, "La licencia esta vencida.")]
    [InlineData(LicenseValidationStatus.ServerTimeUnavailable, "No fue posible comprobar la vigencia con el servidor.")]
    public void Failure_status_has_exact_safe_message(
        LicenseValidationStatus status,
        string message)
    {
        var result = new LicenseValidationResult(status);

        Assert.False(result.IsValid);
        Assert.Equal(message, result.Message);
    }

    [Fact]
    public void Public_shape_never_exposes_sensitive_values()
    {
        var forbidden = new[]
        {
            "Uuid", "HardwareId", "Signature", "Firma", "Path", "Content",
            "Configuration", "Exception", "Password", "ConnectionString"
        };

        var names = typeof(LicenseValidationResult)
            .GetMembers(BindingFlags.Instance | BindingFlags.Public)
            .Select(member => member.Name)
            .ToArray();

        foreach (var value in forbidden)
        {
            Assert.DoesNotContain(names, name =>
                name.Contains(value, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public void Date_range_must_be_ordered()
    {
        Assert.Throws<ArgumentException>(() => new LicenseValidationResult(
            LicenseValidationStatus.InvalidSignature,
            LicenseIssuer.None,
            new DateTime(2026, 2, 1),
            new DateTime(2026, 1, 1)));
    }
}
