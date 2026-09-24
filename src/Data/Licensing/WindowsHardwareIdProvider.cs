using System.Diagnostics;
using Microsoft.Extensions.Logging;
using SysTools.Business.Licensing;

namespace SysTools.Data.Licensing;

internal sealed record HardwareCommand(string FileName, IReadOnlyList<string> Arguments);

internal sealed record HardwareCommandResult(bool Succeeded, bool TimedOut, string Output);

internal interface IHardwareCommandRunner
{
    Task<HardwareCommandResult> RunAsync(
        HardwareCommand command,
        CancellationToken cancellationToken);
}

public sealed class WindowsHardwareIdProvider : IHardwareIdProvider
{
    private readonly IHardwareCommandRunner _runner;
    private readonly ILogger<WindowsHardwareIdProvider> _logger;

    public WindowsHardwareIdProvider(ILogger<WindowsHardwareIdProvider> logger)
        : this(new ProcessHardwareCommandRunner(), logger)
    {
    }

    internal WindowsHardwareIdProvider(
        IHardwareCommandRunner runner,
        ILogger<WindowsHardwareIdProvider> logger)
    {
        ArgumentNullException.ThrowIfNull(runner);
        ArgumentNullException.ThrowIfNull(logger);
        _runner = runner;
        _logger = logger;
    }

    public async Task<string?> GetHardwareIdAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var commands = BuildCommands();
        for (var index = 0; index < commands.Count; index++)
        {
            var result = await _runner.RunAsync(commands[index], cancellationToken)
                .ConfigureAwait(false);
            var hardwareId = ParseHardwareId(result.Output);
            if (result.Succeeded && hardwareId is not null)
            {
                _logger.LogInformation(
                    "Hardware identity resolved using candidate {Candidate}",
                    index + 1);
                return hardwareId;
            }

            _logger.LogWarning(
                "Hardware identity candidate {Candidate} failed with {Outcome}",
                index + 1,
                result.TimedOut ? "Timeout" : "Unavailable");
        }

        return null;
    }

    internal static string? ParseHardwareId(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            return null;
        }

        foreach (var rawLine in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var line = rawLine.Trim();
            if (line.Equals("UUID", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (line.StartsWith("UUID", StringComparison.OrdinalIgnoreCase))
            {
                line = line[4..].Trim();
            }

            if (TryNormalizeHardwareId(line, out var normalized))
            {
                return normalized;
            }
        }

        return null;
    }

    private static bool TryNormalizeHardwareId(string value, out string normalized)
    {
        normalized = string.Empty;
        if (!Guid.TryParseExact(value, "D", out var parsed))
        {
            return false;
        }

        normalized = parsed.ToString("D").ToUpperInvariant();
        return true;
    }

    private static IReadOnlyList<HardwareCommand> BuildCommands()
    {
        var windows = Environment.GetEnvironmentVariable("WINDIR");
        if (string.IsNullOrWhiteSpace(windows))
        {
            windows = @"C:\Windows";
        }

        var powershellArguments = new[]
        {
            "-NoProfile",
            "-NonInteractive",
            "-Command",
            "(Get-CimInstance Win32_ComputerSystemProduct).UUID"
        };
        var wmicArguments = new[] { "csproduct", "get", "uuid" };

        return
        [
            new(Path.Combine(windows, "System32", "WindowsPowerShell", "v1.0", "powershell.exe"), powershellArguments),
            new(Path.Combine(windows, "Sysnative", "WindowsPowerShell", "v1.0", "powershell.exe"), powershellArguments),
            new(Path.Combine(windows, "System32", "wbem", "WMIC.exe"), wmicArguments),
            new(Path.Combine(windows, "Sysnative", "wbem", "WMIC.exe"), wmicArguments)
        ];
    }

    private sealed class ProcessHardwareCommandRunner : IHardwareCommandRunner
    {
        private const int MaximumOutputCharacters = 4096;
        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(5);

        public async Task<HardwareCommandResult> RunAsync(
            HardwareCommand command,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(Timeout);
            using var process = new Process
            {
                StartInfo = CreateStartInfo(command)
            };

            try
            {
                if (!process.Start())
                {
                    return new HardwareCommandResult(false, false, string.Empty);
                }

                var standardOutput = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var standardError = process.StandardError.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);
                var output = await standardOutput.ConfigureAwait(false);
                await standardError.ConfigureAwait(false);
                if (output.Length > MaximumOutputCharacters)
                {
                    output = output[..MaximumOutputCharacters];
                }

                return new HardwareCommandResult(process.ExitCode == 0, false, output);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                TryKill(process);
                throw;
            }
            catch (OperationCanceledException)
            {
                TryKill(process);
                return new HardwareCommandResult(false, true, string.Empty);
            }
            catch (Exception exception) when (
                exception is InvalidOperationException
                    or System.ComponentModel.Win32Exception
                    or IOException)
            {
                return new HardwareCommandResult(false, false, string.Empty);
            }
        }

        private static ProcessStartInfo CreateStartInfo(HardwareCommand command)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = command.FileName,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (var argument in command.Arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            return startInfo;
        }

        private static void TryKill(Process process)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
