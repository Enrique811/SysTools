namespace SysTools.Business.Labels;

public interface IPriceFormatterService
{
    string Format(decimal amount, string? formatCode);
}
