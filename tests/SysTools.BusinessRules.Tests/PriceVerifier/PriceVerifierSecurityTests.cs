using SysTools.Business.PriceVerifier;
using SysTools.BusinessRules.Tests.TestDoubles;
using SysTools.Entities.Configuration;

namespace SysTools.BusinessRules.Tests.PriceVerifier;

public sealed class PriceVerifierSecurityTests
{
    [Fact]
    public async Task Workflow_logs_only_safe_structured_diagnostics()
    {
        var logger = new CollectingLogger<PriceVerifierWorkflow>();
        var products = new ProductServiceStub
        {
            GetHandler = (_, _, _) => throw new InvalidOperationException("SECRET_EXCEPTION")
        };
        var configuration = new ConfigurationServiceStub
        {
            LoadHandler = _ => Task.FromResult(PriceVerifierTestData.Loaded(new AppConfiguration
            {
                IpEmpresa = "SECRET_HOST",
                RutaEmpresa = "SECRET_PATH",
                Usuario = "SECRET_USER",
                Password = "SECRET_PASSWORD",
                Licencia = "SECRET_LICENSE",
                FormatoPrecio = "MX"
            }))
        };
        var workflow = PriceVerifierLookupTests.Create(products, new PriceFormatterServiceStub(), logger, configuration);
        await workflow.PrepareAsync();
        var result = await workflow.LookupAsync("SECRET_BARCODE");

        var text = string.Join('|', logger.Entries.SelectMany(entry =>
            entry.Properties.Select(pair => $"{pair.Key}={pair.Value}")));
        Assert.DoesNotContain("SECRET_BARCODE", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_EXCEPTION", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_HOST", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_PATH", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_USER", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_PASSWORD", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET_LICENSE", text, StringComparison.Ordinal);
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
        Assert.NotEmpty(result.Message);
    }
}
