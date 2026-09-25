namespace SysTools.Configuration.Tests.Reports;

public sealed class PhysicalPrinterIntegrationTests
{
    [Fact(Skip = "SKIPPED: requiere impresora no productiva, operador presente y protocolo TEST / NO VENDER.")]
    public void Print_test_label_only_under_documented_physical_protocol()
    {
        // Intencionalmente no automatizado: nunca se debe enviar papel desde CI ni usar la impresora predeterminada.
    }
}
