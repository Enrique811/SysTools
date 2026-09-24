using Microsoft.Extensions.Logging;
using SysTools.Business.Repositories;
using SysTools.Entities.Configuration;
using SysTools.Entities.Licensing;

namespace SysTools.Business.Licensing;

public sealed class LicenseService : ILicenseService
{
    private readonly ILicenseSourceReader _sourceReader;
    private readonly IHardwareIdProvider _hardwareIdProvider;
    private readonly IServerClockRepository _serverClockRepository;
    private readonly ILicenseSignatureVerifier _signatureVerifier;
    private readonly ILogger<LicenseService> _logger;

    public LicenseService(
        ILicenseSourceReader sourceReader,
        IHardwareIdProvider hardwareIdProvider,
        IServerClockRepository serverClockRepository,
        ILicenseSignatureVerifier signatureVerifier,
        ILogger<LicenseService> logger)
    {
        ArgumentNullException.ThrowIfNull(sourceReader);
        ArgumentNullException.ThrowIfNull(hardwareIdProvider);
        ArgumentNullException.ThrowIfNull(serverClockRepository);
        ArgumentNullException.ThrowIfNull(signatureVerifier);
        ArgumentNullException.ThrowIfNull(logger);
        _sourceReader = sourceReader;
        _hardwareIdProvider = hardwareIdProvider;
        _serverClockRepository = serverClockRepository;
        _signatureVerifier = signatureVerifier;
        _logger = logger;
    }

    public async Task<LicenseValidationResult> ValidateAsync(
        string? licenseInput,
        AppConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(licenseInput))
        {
            return Result(LicenseValidationStatus.MissingInput, "Source");
        }

        var input = licenseInput.Trim();
        string content;
        if (input.StartsWith('{'))
        {
            content = input;
        }
        else
        {
            LicenseSourceReadResult source;
            try
            {
                source = await _sourceReader.ReadAsync(input, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return Result(LicenseValidationStatus.SourceUnavailable, "Source");
            }

            if (!source.Succeeded || source.Content is null)
            {
                return Result(LicenseValidationStatus.SourceUnavailable, "Source");
            }

            content = source.Content;
        }

        var parse = LicenseDocumentParser.Parse(content);
        if (!parse.Succeeded)
        {
            var status = parse.Failure == LicenseParseFailure.InvalidJson
                ? LicenseValidationStatus.InvalidJson
                : LicenseValidationStatus.InvalidFields;
            return Result(status, "Parse");
        }

        var document = parse.Document!;
        var issuer = _signatureVerifier.Verify(document.SignedPayload, document.Signature);
        if (issuer == LicenseIssuer.None)
        {
            return Result(
                LicenseValidationStatus.InvalidSignature,
                "Signature",
                validFrom: document.ValidFrom,
                validUntil: document.ValidUntil);
        }

        string? localHardwareId;
        try
        {
            localHardwareId = await _hardwareIdProvider
                .GetHardwareIdAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result(
                LicenseValidationStatus.HardwareIdUnavailable,
                "Hardware",
                issuer,
                document.ValidFrom,
                document.ValidUntil);
        }

        if (!TryNormalizeHardwareId(localHardwareId, out var normalizedHardwareId))
        {
            return Result(
                LicenseValidationStatus.HardwareIdUnavailable,
                "Hardware",
                issuer,
                document.ValidFrom,
                document.ValidUntil);
        }

        if (!string.Equals(
                document.HardwareId,
                normalizedHardwareId,
                StringComparison.Ordinal))
        {
            return Result(
                LicenseValidationStatus.HardwareMismatch,
                "Hardware",
                issuer,
                document.ValidFrom,
                document.ValidUntil);
        }

        DateTime serverTime;
        try
        {
            serverTime = await _serverClockRepository
                .GetCurrentAsync(configuration, cancellationToken)
                .ConfigureAwait(false);
            serverTime = DateTime.SpecifyKind(serverTime, DateTimeKind.Unspecified);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return Result(
                LicenseValidationStatus.ServerTimeUnavailable,
                "ServerTime",
                issuer,
                document.ValidFrom,
                document.ValidUntil);
        }

        if (serverTime < document.ValidFrom)
        {
            return Result(
                LicenseValidationStatus.NotYetValid,
                "ServerTime",
                issuer,
                document.ValidFrom,
                document.ValidUntil,
                serverTime);
        }

        if (serverTime > document.ValidUntil)
        {
            return Result(
                LicenseValidationStatus.Expired,
                "ServerTime",
                issuer,
                document.ValidFrom,
                document.ValidUntil,
                serverTime);
        }

        return Result(
            LicenseValidationStatus.Valid,
            "Complete",
            issuer,
            document.ValidFrom,
            document.ValidUntil,
            serverTime);
    }

    internal static bool TryNormalizeHardwareId(string? value, out string normalized)
    {
        normalized = string.Empty;
        if (string.IsNullOrWhiteSpace(value)
            || !Guid.TryParseExact(value.Trim(), "D", out var parsed))
        {
            return false;
        }

        normalized = parsed.ToString("D").ToUpperInvariant();
        return true;
    }

    private LicenseValidationResult Result(
        LicenseValidationStatus status,
        string stage,
        LicenseIssuer issuer = LicenseIssuer.None,
        DateTime? validFrom = null,
        DateTime? validUntil = null,
        DateTime? serverTime = null)
    {
        _logger.LogInformation(
            "License validation completed at {Stage} with {Status} and {Issuer}",
            stage,
            status,
            issuer);
        return new LicenseValidationResult(
            status,
            issuer,
            validFrom,
            validUntil,
            serverTime);
    }
}
