namespace SysTools.Entities.ConfigurationEditor;

public sealed record ConfigurationDraft
{
    private string _ipEmpresa = string.Empty;
    private string _rutaEmpresa = string.Empty;
    private string _usuario = "SYSDBA";
    private string _ambiente = "a";
    private string _impresora = string.Empty;
    private string _formatoPrecio = "MX";
    private string _reporte = string.Empty;
    private string _informacion = string.Empty;

    public string IpEmpresa { get => _ipEmpresa; init => _ipEmpresa = value ?? string.Empty; }
    public string RutaEmpresa { get => _rutaEmpresa; init => _rutaEmpresa = value ?? string.Empty; }
    public string Usuario { get => _usuario; init => _usuario = value ?? string.Empty; }
    public string Ambiente { get => _ambiente; init => _ambiente = value ?? string.Empty; }
    public string Impresora { get => _impresora; init => _impresora = value ?? string.Empty; }
    public string FormatoPrecio { get => _formatoPrecio; init => _formatoPrecio = value ?? string.Empty; }
    public string Reporte { get => _reporte; init => _reporte = value ?? string.Empty; }
    public int Columnas { get; init; } = 1;
    public string Informacion { get => _informacion; init => _informacion = value ?? string.Empty; }
}
