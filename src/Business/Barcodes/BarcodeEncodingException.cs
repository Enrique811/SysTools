namespace SysTools.Business.Barcodes;

public sealed class BarcodeEncodingException : Exception
{
    public BarcodeEncodingException()
        : base("No fue posible codificar la imagen del código de barras.")
    {
    }

    public BarcodeEncodingException(Exception innerException)
        : base("No fue posible codificar la imagen del código de barras.", innerException)
    {
    }
}
