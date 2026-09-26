using SysTools.Data.Support;
using SysTools.Entities.Support;

namespace SysTools.Configuration.Tests.Support;

public sealed class ManagedSupportLogStoreSecurityTests
{
    [Theory]
    [InlineData("../outside.log")] [InlineData("..\\outside.log")] [InlineData("nested/a.log")] [InlineData("nested\\a.log")]
    [InlineData("a.txt")] [InlineData("a.log.exe")] [InlineData("")] [InlineData("C:\\outside.log")]
    public async Task Unsafe_identifiers_are_rejected(string id)
    {
        using var directory=new SupportLogTestDirectory();
        Assert.Equal(LogPreviewStatus.InvalidIdentifier,(await new ManagedSupportLogStore(directory.Path).ReadAsync(id)).Status);
    }
    [Fact] public async Task Fifty_generated_traversal_variants_never_read_outside()
    {
        using var directory=new SupportLogTestDirectory();var store=new ManagedSupportLogStore(directory.Path);
        for(var i=0;i<50;i++) Assert.Equal(LogPreviewStatus.InvalidIdentifier,(await store.ReadAsync($"../secret-{i}.log")).Status);
    }
}

