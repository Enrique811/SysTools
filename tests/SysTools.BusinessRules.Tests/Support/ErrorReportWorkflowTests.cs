using SysTools.Entities.Support;

namespace SysTools.BusinessRules.Tests.Support;

public sealed class ErrorReportWorkflowTests
{
    [Theory] [InlineData("", "detail")] [InlineData("summary", "")] [InlineData("summary\nextra", "detail")]
    public void Invalid_input_does_not_create_draft(string summary,string detail) =>
        Assert.Equal(SupportActionStatus.InvalidInput,SupportFactory.Create().PrepareErrorReport(summary,detail).Status);

    [Fact] public void Report_contains_explicit_text_version_and_opaque_id_but_not_hardware()
    {
        var result=SupportFactory.Create("SECRET-UUID").PrepareErrorReport("Resumen á", "Detalle & seguro");
        Assert.Equal(SupportActionStatus.Ready,result.Status);
        Assert.Contains("Resumen á",result.Draft!.Body,StringComparison.Ordinal);
        Assert.Contains("1.2.3",result.Draft.Body,StringComparison.Ordinal);
        Assert.DoesNotContain("SECRET-UUID",result.Draft.Body,StringComparison.Ordinal);
        Assert.Matches("^[0-9a-f]{32}$",result.Draft.DiagnosticId);
    }
    [Fact] public void Maximum_documented_limits_are_accepted()
    {
        var result=SupportFactory.Create().PrepareErrorReport(new string('s',120),new string('d',4000));
        Assert.Equal(SupportActionStatus.Ready,result.Status);
    }
}

