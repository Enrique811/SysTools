using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Data.Licensing;

namespace SysTools.Licensing.Tests.Data;

public sealed class WindowsHardwareIdProviderTests
{
    [Theory]
    [InlineData("UUID\r\n00112233-4455-6677-8899-aabbccddeeff\r\n")]
    [InlineData("UUID  00112233-4455-6677-8899-AABBCCDDEEFF")]
    [InlineData("noise\n00112233-4455-6677-8899-AABBCCDDEEFF\n")]
    public async Task Parses_canonical_uuid_from_supported_outputs(string output)
    {
        var runner = new StubHardwareCommandRunner(
            new HardwareCommandResult(true, false, output));
        var provider = new WindowsHardwareIdProvider(
            runner,
            NullLogger<WindowsHardwareIdProvider>.Instance);

        var value = await provider.GetHardwareIdAsync();

        Assert.Equal("00112233-4455-6677-8899-AABBCCDDEEFF", value);
        Assert.Single(runner.Commands);
        Assert.Contains("powershell", runner.Commands[0].FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-NoProfile", runner.Commands[0].Arguments);
    }

    [Fact]
    public async Task Falls_back_through_fixed_commands_after_timeout_or_invalid_output()
    {
        var runner = new StubHardwareCommandRunner(
            new HardwareCommandResult(false, true, string.Empty),
            new HardwareCommandResult(true, false, "invalid"),
            new HardwareCommandResult(true, false, "00112233-4455-6677-8899-AABBCCDDEEFF"));
        var provider = new WindowsHardwareIdProvider(
            runner,
            NullLogger<WindowsHardwareIdProvider>.Instance);

        var value = await provider.GetHardwareIdAsync();

        Assert.NotNull(value);
        Assert.Equal(3, runner.Commands.Count);
        Assert.Contains("WMIC", runner.Commands[2].FileName, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Returns_null_when_all_commands_fail_without_logging_output()
    {
        var runner = new StubHardwareCommandRunner(
            Enumerable.Repeat(
                new HardwareCommandResult(false, false, "SENTINEL-RAW-OUTPUT"),
                4).ToArray());
        var logger = new TestDoubles.CollectingLogger<WindowsHardwareIdProvider>();
        var provider = new WindowsHardwareIdProvider(runner, logger);

        var value = await provider.GetHardwareIdAsync();

        Assert.Null(value);
        Assert.Equal(4, runner.Commands.Count);
        Assert.DoesNotContain(logger.Messages, message => message.Contains("SENTINEL"));
    }

    [Fact]
    public async Task Propagates_requested_cancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var provider = new WindowsHardwareIdProvider(
            new StubHardwareCommandRunner(),
            NullLogger<WindowsHardwareIdProvider>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            provider.GetHardwareIdAsync(cancellation.Token));
    }

    private sealed class StubHardwareCommandRunner : IHardwareCommandRunner
    {
        private readonly Queue<HardwareCommandResult> _results;

        internal StubHardwareCommandRunner(params HardwareCommandResult[] results) =>
            _results = new Queue<HardwareCommandResult>(results);

        internal List<HardwareCommand> Commands { get; } = [];

        public Task<HardwareCommandResult> RunAsync(
            HardwareCommand command,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Commands.Add(command);
            return Task.FromResult(_results.Count == 0
                ? new HardwareCommandResult(false, false, string.Empty)
                : _results.Dequeue());
        }
    }
}
