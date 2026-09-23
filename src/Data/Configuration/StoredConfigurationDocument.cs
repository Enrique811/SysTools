using System.Text.Json;
using System.Text.Json.Serialization;
using SysTools.Entities.Configuration;

namespace SysTools.Data.Configuration;

internal sealed class StoredConfigurationDocument
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 1;

    [JsonPropertyName("ipEmpresa")]
    public string? IpEmpresa { get; set; } = string.Empty;

    [JsonPropertyName("rutaEmpresa")]
    public string? RutaEmpresa { get; set; } = string.Empty;

    [JsonPropertyName("usuario")]
    public string? Usuario { get; set; } = "SYSDBA";

    [JsonPropertyName("passwordProtegido")]
    public string? PasswordProtegido { get; set; } = string.Empty;

    [JsonPropertyName("ambiente")]
    public string? Ambiente { get; set; } = "a";

    [JsonPropertyName("impresora")]
    public string? Impresora { get; set; } = string.Empty;

    [JsonPropertyName("formatoPrecio")]
    public string? FormatoPrecio { get; set; } = "MX";

    [JsonPropertyName("reporte")]
    public string? Reporte { get; set; } = string.Empty;

    [JsonPropertyName("columnas")]
    public int Columnas { get; set; } = 1;

    [JsonPropertyName("informacion")]
    public string? Informacion { get; set; } = string.Empty;

    [JsonPropertyName("licencia")]
    public string? Licencia { get; set; } = string.Empty;

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    public AppConfiguration ToConfiguration(string password) => new()
    {
        IpEmpresa = IpEmpresa ?? string.Empty,
        RutaEmpresa = RutaEmpresa ?? string.Empty,
        Usuario = Usuario ?? string.Empty,
        Password = password,
        Ambiente = Ambiente ?? string.Empty,
        Impresora = Impresora ?? string.Empty,
        FormatoPrecio = FormatoPrecio ?? string.Empty,
        Reporte = Reporte ?? string.Empty,
        Columnas = Columnas,
        Informacion = Informacion ?? string.Empty,
        Licencia = Licencia ?? string.Empty
    };

    public static StoredConfigurationDocument FromConfiguration(
        AppConfiguration configuration,
        string protectedPassword,
        int schemaVersion = 1,
        Dictionary<string, JsonElement>? extensionData = null) => new()
    {
        SchemaVersion = schemaVersion < 1 ? 1 : schemaVersion,
        IpEmpresa = configuration.IpEmpresa,
        RutaEmpresa = configuration.RutaEmpresa,
        Usuario = configuration.Usuario,
        PasswordProtegido = protectedPassword,
        Ambiente = configuration.Ambiente,
        Impresora = configuration.Impresora,
        FormatoPrecio = configuration.FormatoPrecio,
        Reporte = configuration.Reporte,
        Columnas = configuration.Columnas,
        Informacion = configuration.Informacion,
        Licencia = configuration.Licencia,
        ExtensionData = extensionData
    };
}
