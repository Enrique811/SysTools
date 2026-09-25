namespace SysTools.Presentation.Modules.PriceVerifier.Search;

public interface IProductSearchDialogService
{
    ProductSearchDialogResult ShowDialog(CancellationToken cancellationToken = default);
    void CloseActive();
}
