using System.Windows;
using System.Windows.Input;

namespace SysTools.Presentation.Modules.Labels;

public partial class LabelPreviewWindow : Window
{
    public LabelPreviewWindow(LabelPreviewViewModel viewModel) { InitializeComponent(); DataContext = viewModel; }
    private void OnClose(object sender, RoutedEventArgs e) => Close();
    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) { e.Handled = true; Close(); }
        else if (DataContext is LabelPreviewViewModel vm && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Key is Key.Add or Key.OemPlus) { vm.Zoom += .1; e.Handled = true; }
            else if (e.Key is Key.Subtract or Key.OemMinus) { vm.Zoom -= .1; e.Handled = true; }
            else if (e.Key == Key.D0) { vm.Zoom = 1; e.Handled = true; }
        }
    }
}
