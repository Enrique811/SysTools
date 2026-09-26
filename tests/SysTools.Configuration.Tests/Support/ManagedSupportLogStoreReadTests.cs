using SysTools.Data.Support;
using SysTools.Entities.Support;
using System.Diagnostics;

namespace SysTools.Configuration.Tests.Support;

public sealed class ManagedSupportLogStoreReadTests
{
    [Fact] public async Task Reads_only_last_five_hundred_lines_and_marks_truncation()
    {
        using var directory=new SupportLogTestDirectory();directory.Write("large.log",string.Join('\n',Enumerable.Range(1,800).Select(i=>$"line-{i}")));
        var stopwatch=Stopwatch.StartNew();var result=await new ManagedSupportLogStore(directory.Path).ReadAsync("large.log");stopwatch.Stop();
        Assert.Equal(LogPreviewStatus.Loaded,result.Status);Assert.True(result.IsTruncated);
        Assert.DoesNotContain("line-1"+Environment.NewLine,result.Content,StringComparison.Ordinal);
        Assert.Contains("line-800",result.Content,StringComparison.Ordinal);
        Assert.True(result.Content.Count(c=>c=='\n')+1<=500);
        Assert.True(stopwatch.Elapsed<TimeSpan.FromSeconds(2),$"Preview took {stopwatch.Elapsed}.");
    }
    [Fact] public async Task Rejects_file_larger_than_fifty_mibibytes()
    {
        using var directory=new SupportLogTestDirectory();var path=Path.Combine(directory.Path,"huge.log");
        using(var stream=File.Create(path))stream.SetLength(LogFileSummary.MaximumFileBytes+1);
        Assert.Equal(LogPreviewStatus.TooLarge,(await new ManagedSupportLogStore(directory.Path).ReadAsync("huge.log")).Status);
    }
    [Fact] public async Task Invalid_utf8_is_reported_without_throwing()
    {
        using var directory=new SupportLogTestDirectory();File.WriteAllBytes(Path.Combine(directory.Path,"bad.log"),[0xC3,0x28]);
        Assert.Equal(LogPreviewStatus.InvalidContent,(await new ManagedSupportLogStore(directory.Path).ReadAsync("bad.log")).Status);
    }
}
