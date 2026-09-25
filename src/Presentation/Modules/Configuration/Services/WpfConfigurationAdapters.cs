using Microsoft.Win32;
using System.Windows;

namespace SysTools.Presentation.Modules.Configuration.Services;

public sealed class WpfConfigurationFilePicker : IConfigurationFilePicker
{
    public string? PickDatabase() => Pick("Bases Firebird (*.fdb;*.gdb)|*.fdb;*.gdb|Todos los archivos (*.*)|*.*");
    public string? PickLicense() => Pick("Licencias (*.lic)|*.lic", true);
    private static string? Pick(string filter, bool checkExists = false)
    {
        var dialog = new OpenFileDialog { Filter = filter, CheckFileExists = checkExists };
        return dialog.ShowDialog(Application.Current?.MainWindow) == true ? dialog.FileName : null;
    }
}

public sealed class WpfClipboardService : IClipboardService
{
    public bool TrySetText(string value)
    {
        try { Clipboard.SetText(value); return true; }
        catch (Exception exception) when (exception is System.Runtime.InteropServices.COMException or InvalidOperationException) { return false; }
    }
}

public sealed class WpfConfirmationService : IConfirmationService
{
    public bool ConfirmDiscard() => MessageBox.Show(
        "Hay cambios sin guardar. ¿Desea descartarlos?", "Configuración",
        MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;

    public bool ConfirmRecovery() => MessageBox.Show(
        "La configuración existente no puede leerse. ¿Desea crear una nueva y conservar un respaldo?",
        "Recuperar configuración", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
}
