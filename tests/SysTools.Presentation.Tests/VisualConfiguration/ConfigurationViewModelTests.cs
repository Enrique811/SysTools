using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Business.ConfigurationEditor;
using SysTools.Entities.Configuration;
using SysTools.Entities.ConfigurationEditor;
using SysTools.Entities.Connection;
using SysTools.Entities.Licensing;
using SysTools.Presentation.Modules.Configuration;
using SysTools.Presentation.Modules.Configuration.Services;

namespace SysTools.Presentation.Tests.VisualConfiguration;

public sealed class ConfigurationViewModelTests
{
    [Fact]
    public async Task Initialize_publishes_only_safe_fields_and_available_options()
    {
        var workflow = new WorkflowStub
        {
            Snapshot = Snapshot(new ConfigurationDraft
            {
                IpEmpresa = "server",
                RutaEmpresa = "database.fdb",
                Usuario = "SYSDBA",
                Columnas = 2
            })
        };
        var viewModel = Create(workflow);

        await viewModel.InitializeAsync();

        Assert.Equal("server", viewModel.IpEmpresa);
        Assert.True(viewModel.HasStoredPassword);
        Assert.False(viewModel.IsBusy);
        Assert.True(viewModel.IsIdle);
        Assert.DoesNotContain(viewModel.GetType().GetProperties(), property =>
            property.Name.Contains("Password", StringComparison.OrdinalIgnoreCase)
            && property.PropertyType == typeof(string));
    }

    [Fact]
    public async Task Busy_state_prevents_duplicate_connection_tests()
    {
        var completion = new TaskCompletionSource<ConnectionEditorResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workflow = new WorkflowStub { ConnectionHandler = (_, _, _, _) => completion.Task };
        var viewModel = Create(workflow);
        await viewModel.InitializeAsync();

        var first = viewModel.TestConnectionAsync("secret");
        await viewModel.TestConnectionAsync("secret");

        Assert.True(viewModel.IsBusy);
        Assert.Equal(1, workflow.ConnectionCalls);
        completion.SetResult(new ConnectionEditorResult(
            new ConnectionTestResult(ConnectionTestStatus.Success, "Disponible", TimeSpan.Zero),
            "proof"));
        await first;
        Assert.True(viewModel.IsIdle);
    }

    [Fact]
    public async Task Deactivation_suppresses_a_late_connection_result()
    {
        var completion = new TaskCompletionSource<ConnectionEditorResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var workflow = new WorkflowStub { ConnectionHandler = (_, _, _, _) => completion.Task };
        var viewModel = Create(workflow);
        await viewModel.InitializeAsync();

        var operation = viewModel.TestConnectionAsync("secret");
        viewModel.Deactivate();
        completion.SetResult(new ConnectionEditorResult(
            new ConnectionTestResult(ConnectionTestStatus.Success, "LATE-SENSITIVE-RESULT", TimeSpan.Zero),
            "proof"));
        await operation;

        Assert.DoesNotContain("LATE-SENSITIVE-RESULT", viewModel.StatusMessage, StringComparison.Ordinal);
        Assert.Equal(1, workflow.InvalidateCalls);
    }

    [Fact]
    public async Task Successful_save_requests_close_and_failed_save_keeps_editor_open()
    {
        var workflow = new WorkflowStub();
        var viewModel = Create(workflow);
        await viewModel.InitializeAsync();
        ConfigurationDialogResult? close = null;
        viewModel.CloseRequested += (_, result) => close = result;

        workflow.SaveResult = new(
            ConfigurationEditorSaveStatus.ValidationFailed,
            "Corrija los campos.",
            [new ConfigurationIssue(
                ConfigurationIssueCode.RequiredForConnection,
                "ipEmpresa",
                "Es requerido.",
                ConfigurationIssueSeverity.Error)]);
        await viewModel.SaveAsync(string.Empty);
        Assert.Null(close);
        Assert.Contains("Servidor: Es requerido.", viewModel.ValidationIssues);

        workflow.SaveResult = new(ConfigurationEditorSaveStatus.Saved, "Guardado.");
        await viewModel.SaveAsync(string.Empty);
        Assert.Equal(ConfigurationDialogOutcome.Saved, close?.Outcome);
    }

    [Fact]
    public async Task Invalid_license_never_marks_the_session_as_having_a_replacement()
    {
        var workflow = new WorkflowStub
        {
            LicenseResult = new(new LicenseSummary(
                LicenseValidationStatus.InvalidSignature,
                "Firma inválida."))
        };
        var viewModel = Create(workflow, new PickerStub { License = "C:\\private\\candidate.lic" });
        await viewModel.InitializeAsync();

        await viewModel.ImportLicenseAsync(string.Empty);

        Assert.False(viewModel.HasStoredLicense);
        Assert.False(viewModel.IsDirty);
        Assert.DoesNotContain("private", viewModel.StatusMessage, StringComparison.OrdinalIgnoreCase);
    }

    private static ConfigurationViewModel Create(WorkflowStub workflow, PickerStub? picker = null) =>
        new(workflow, picker ?? new PickerStub(), new ClipboardStub(), new ConfirmationStub(), NullLogger<ConfigurationViewModel>.Instance);

    private static ConfigurationEditorSnapshot Snapshot(ConfigurationDraft? draft = null) => new(
        ConfigurationEditorOpenStatus.Ready,
        draft ?? new ConfigurationDraft { Ambiente = "a", FormatoPrecio = "MX", Columnas = 1 },
        true,
        false,
        false,
        [],
        [new OperationalOption("printer", "Printer")],
        [new OperationalOption("template.frx", "Template")],
        HardwareIdentityResult.Available("hardware"));

    private sealed class WorkflowStub : IConfigurationEditorWorkflow
    {
        internal ConfigurationEditorSnapshot Snapshot { get; set; } = ConfigurationViewModelTests.Snapshot();
        internal Func<ConfigurationDraft, string, long, CancellationToken, Task<ConnectionEditorResult>> ConnectionHandler { get; set; } =
            static (_, _, _, _) => Task.FromResult(new ConnectionEditorResult(
                new ConnectionTestResult(ConnectionTestStatus.Success, "Disponible", TimeSpan.Zero), "proof"));
        internal ConfigurationEditorSaveResult SaveResult { get; set; } = new(ConfigurationEditorSaveStatus.Saved, "Guardado.");
        internal LicenseEditorResult LicenseResult { get; set; } = new(new LicenseSummary(LicenseValidationStatus.MissingInput, "Sin licencia."));
        internal int ConnectionCalls { get; private set; }
        internal int InvalidateCalls { get; private set; }

        public Task<ConfigurationEditorSnapshot> OpenAsync(CancellationToken token = default) => Task.FromResult(Snapshot);
        public ConfigurationValidationResult Validate(ConfigurationDraft draft, bool hasNewPassword) => new(true, true);
        public Task<ConnectionEditorResult> TestConnectionAsync(ConfigurationDraft draft, string password, long revision, CancellationToken token = default)
        { ConnectionCalls++; return ConnectionHandler(draft, password, revision, token); }
        public Task<LicenseEditorResult> ValidateLicenseAsync(ConfigurationDraft draft, string password, string path, long revision, CancellationToken token = default) => Task.FromResult(LicenseResult);
        public Task<ConfigurationEditorSaveResult> SaveAsync(ConfigurationDraft draft, string password, long revision, string? proof, LicenseChange change, CancellationToken token = default) => Task.FromResult(SaveResult);
        public Task<ConfigurationEditorSaveResult> RecoverAsync(ConfigurationDraft draft, string password, long revision, string? proof, bool confirmed, CancellationToken token = default) => Task.FromResult(SaveResult);
        public void Invalidate() => InvalidateCalls++;
    }

    private sealed class PickerStub : IConfigurationFilePicker
    {
        internal string? Database { get; set; }
        internal string? License { get; set; }
        public string? PickDatabase() => Database;
        public string? PickLicense() => License;
    }
    private sealed class ClipboardStub : IClipboardService { public bool TrySetText(string value) => true; }
    private sealed class ConfirmationStub : IConfirmationService { public bool ConfirmDiscard() => true; public bool ConfirmRecovery() => true; }
}
