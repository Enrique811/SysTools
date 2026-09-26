using SysTools.Data.Support;
using SysTools.Entities.Support;
using System.Diagnostics;

namespace SysTools.Configuration.Tests.Support;

public sealed class ManagedSupportLogStoreCatalogTests
{
    [Fact] public async Task Missing_and_empty_directories_have_distinct_statuses()
    {
        using var missing=new SupportLogTestDirectory(false);
        Assert.Equal(LogCatalogStatus.DirectoryUnavailable,(await new ManagedSupportLogStore(missing.Path).GetCatalogAsync()).Status);
        Directory.CreateDirectory(missing.Path);
        Assert.Equal(LogCatalogStatus.Empty,(await new ManagedSupportLogStore(missing.Path).GetCatalogAsync()).Status);
    }
    [Fact] public async Task Returns_twenty_newest_top_level_logs_with_safe_metadata()
    {
        using var directory=new SupportLogTestDirectory();
        for(var i=0;i<25;i++){var path=directory.Write($"log-{i:00}.log",i.ToString());File.SetLastWriteTimeUtc(path,new DateTime(2026,1,1).AddMinutes(i));}
        directory.Write("ignored.txt","x");Directory.CreateDirectory(Path.Combine(directory.Path,"nested"));File.WriteAllText(Path.Combine(directory.Path,"nested","hidden.log"),"x");
        var stopwatch=Stopwatch.StartNew();var result=await new ManagedSupportLogStore(directory.Path).GetCatalogAsync();stopwatch.Stop();
        Assert.Equal(LogCatalogStatus.Available,result.Status);Assert.Equal(20,result.Files.Count);
        Assert.Equal("log-24.log",result.Files[0].Id);Assert.DoesNotContain(result.Files,file=>file.Id.Contains(directory.Path,StringComparison.Ordinal));
        Assert.True(stopwatch.Elapsed<TimeSpan.FromMilliseconds(500),$"Catalog took {stopwatch.Elapsed}.");
    }
}
