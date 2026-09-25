using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.ConfigurationEditor;
using SysTools.Entities.Configuration;
using SysTools.Entities.ConfigurationEditor;
using SysTools.Presentation.Modules.Configuration;
using SysTools.Presentation.Modules.Configuration.Services;

namespace SysTools.Presentation.Tests.VisualConfiguration;

public sealed class ConfigurationAccessibilityTests
{
    [Fact]
    public void Window_loads_with_empty_password_and_measures_on_sta()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var vm = new ConfigurationViewModel(new WorkflowStub(), new PickerStub(), new ClipboardStub(), new ConfirmationStub(), NullLogger<ConfigurationViewModel>.Instance);
                var window = new ConfigurationWindow(vm) { Width = 980, Height = 680 };
                window.Measure(new Size(980, 680)); window.Arrange(new Rect(0, 0, 980, 680));
                var password = Assert.IsType<PasswordBox>(window.FindName("PasswordInput"));
                Assert.Equal(string.Empty, password.Password);
                Assert.NotNull(window.FindName("HostInput"));
                Assert.True(window.DesiredSize.Width <= 980);
                Assert.True(window.DesiredSize.Height <= 680);
                window.Close();
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        Assert.Null(failure);
    }

    [Fact]
    public void Xaml_has_keyboard_live_region_scroll_and_fixed_actions()
    {
        var root = FindRoot();
        var xaml = File.ReadAllText(Path.Combine(root, "src", "Presentation", "Modules", "Configuration", "ConfigurationWindow.xaml"));
        Assert.Contains("PreviewKeyDown", xaml, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.LiveSetting=\"Polite\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ConfigurationScroll\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid.Row=\"2\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Grid Grid.Row=\"3\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Password=\"{Binding", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void Actions_remain_arranged_at_logical_1280x720_with_125_percent_scaling()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var vm = new ConfigurationViewModel(new WorkflowStub(), new PickerStub(), new ClipboardStub(), new ConfirmationStub(), NullLogger<ConfigurationViewModel>.Instance);
                var window = new ConfigurationWindow(vm) { Width = 1024, Height = 576 };
                var root = Assert.IsAssignableFrom<FrameworkElement>(window.Content);
                root.Measure(new Size(1024, 576));
                root.Arrange(new Rect(0, 0, 1024, 576));

                Assert.True(Assert.IsType<Button>(window.FindName("TestButton")).ActualHeight > 0);
                Assert.True(Assert.IsType<Button>(window.FindName("SaveButton")).ActualHeight > 0);
                Assert.True(Assert.IsType<Button>(window.FindName("CancelButton")).ActualHeight > 0);
                Assert.True(Assert.IsType<ScrollViewer>(window.FindName("ConfigurationScroll")).ActualHeight > 0);
                window.Close();
            }
            catch (Exception exception) { failure = exception; }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join();
        Assert.Null(failure);
    }

    private static string FindRoot() { var d = new DirectoryInfo(AppContext.BaseDirectory); while (d is not null && !File.Exists(Path.Combine(d.FullName, "SysTools.sln"))) d=d.Parent; return d!.FullName; }

    private sealed class WorkflowStub : IConfigurationEditorWorkflow
    {
        public Task<ConfigurationEditorSnapshot> OpenAsync(CancellationToken token = default) => Task.FromResult(new ConfigurationEditorSnapshot(ConfigurationEditorOpenStatus.Ready, new ConfigurationDraft(), true, false, false, [], [], [], HardwareIdentityResult.Unavailable()));
        public ConfigurationValidationResult Validate(ConfigurationDraft draft, bool hasNewPassword) => new(true, true);
        public Task<ConnectionEditorResult> TestConnectionAsync(ConfigurationDraft draft, string password, long revision, CancellationToken token = default) => throw new NotSupportedException();
        public Task<LicenseEditorResult> ValidateLicenseAsync(ConfigurationDraft draft, string password, string path, long revision, CancellationToken token = default) => throw new NotSupportedException();
        public Task<ConfigurationEditorSaveResult> SaveAsync(ConfigurationDraft draft, string password, long revision, string? proof, LicenseChange change, CancellationToken token = default) => throw new NotSupportedException();
        public Task<ConfigurationEditorSaveResult> RecoverAsync(ConfigurationDraft draft, string password, long revision, string? proof, bool confirmed, CancellationToken token = default) => throw new NotSupportedException();
        public void Invalidate() { }
    }
    private sealed class PickerStub : IConfigurationFilePicker { public string? PickDatabase()=>null; public string? PickLicense()=>null; }
    private sealed class ClipboardStub : IClipboardService { public bool TrySetText(string value)=>true; }
    private sealed class ConfirmationStub : IConfirmationService { public bool ConfirmDiscard()=>true; public bool ConfirmRecovery()=>true; }
}
