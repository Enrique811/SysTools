using System.Collections;
using SysTools.Entities.Support;

namespace SysTools.BusinessRules.Tests.Support;

public sealed class SupportEntityTests
{
    [Fact] public void Draft_rejects_empty_or_oversized_fields()
    {
        var id=Guid.NewGuid().ToString("N");
        Assert.Throws<ArgumentException>(()=>new SupportMailDraft(SupportMailKind.ErrorReport,""," ","body",id));
        Assert.Throws<ArgumentException>(()=>new SupportMailDraft(SupportMailKind.ErrorReport,"",new string('s',161),"body",id));
        Assert.Throws<ArgumentException>(()=>new SupportMailDraft(SupportMailKind.ErrorReport,"","subject",new string('b',6001),id));
    }
    [Fact] public void Catalog_is_read_only_and_limited()
    {
        var catalog=new LogCatalogResult(LogCatalogStatus.Available,[new("a.log",DateTime.UtcNow,1)]);
        Assert.Throws<NotSupportedException>(()=>((IList)catalog.Files).Clear());
        Assert.Throws<ArgumentOutOfRangeException>(()=>new LogCatalogResult(LogCatalogStatus.Available,Enumerable.Range(0,21).Select(i=>new LogFileSummary($"{i}.log",DateTime.UtcNow,1))));
    }
    [Fact] public void Preview_rejects_more_than_five_hundred_lines() =>
        Assert.Throws<ArgumentOutOfRangeException>(()=>new LogPreviewResult(LogPreviewStatus.Loaded,"a.log",string.Join('\n',Enumerable.Repeat("x",501))));
    [Fact] public void Failed_results_cannot_expose_sensitive_payloads()
    {
        var draft=new SupportMailDraft(SupportMailKind.ErrorReport,"","s","b",Guid.NewGuid().ToString("N"));
        Assert.Throws<ArgumentException>(()=>new SupportActionResult(SupportActionStatus.LaunchFailed,"failed",draft:draft));
        Assert.Throws<ArgumentException>(()=>new LogPreviewResult(LogPreviewStatus.Failed,"a.log","secret"));
    }
}

