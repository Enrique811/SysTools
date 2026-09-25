using System.IO;
using System.Windows.Media.Imaging;
using SysTools.Entities.Labels;
using SysTools.Presentation.ViewModels;

namespace SysTools.Presentation.Modules.Labels;

public sealed class LabelPreviewViewModel : ViewModelBase
{
    private double _zoom = 1;
    public LabelPreviewViewModel(LabelPreviewDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        using var stream = new MemoryStream(document.GetPngBytes(), writable: false);
        var image = new BitmapImage();
        image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.StreamSource = stream; image.EndInit(); image.Freeze();
        Image = image;
        Summary = $"Vista previa de {document.WidthMm:0.##} × {document.HeightMm:0.##} mm";
    }
    public BitmapSource Image { get; }
    public string Summary { get; }
    public double Zoom { get => _zoom; set => SetProperty(ref _zoom, Math.Clamp(value, .25, 3)); }
}
