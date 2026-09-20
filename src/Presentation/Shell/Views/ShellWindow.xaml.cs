using System.Windows;
using SysTools.Presentation.Shell.ViewModels;
namespace SysTools.Presentation.Shell.Views;
public partial class ShellWindow : Window
{
    public ShellWindow(ShellViewModel viewModel) { InitializeComponent(); DataContext = viewModel; }
}
