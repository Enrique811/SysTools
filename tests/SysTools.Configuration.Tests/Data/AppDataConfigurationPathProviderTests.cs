using SysTools.Data.Configuration;

namespace SysTools.Configuration.Tests.Data;

public sealed class AppDataConfigurationPathProviderTests
{
    [Fact]
    public void GetConfigurationFilePath_AppendsApprovedRelativePath()
    {
        var provider = new AppDataConfigurationPathProvider(() => @"C:\Users\Test\AppData\Roaming");

        var result = provider.GetConfigurationFilePath();

        Assert.Equal(@"C:\Users\Test\AppData\Roaming\SysUtilerias\configuracion.json", result);
    }

    [Fact]
    public void GetConfigurationFilePath_WhenAppDataIsEmpty_DoesNotFallback()
    {
        var provider = new AppDataConfigurationPathProvider(() => " ");

        Assert.Throws<InvalidOperationException>(provider.GetConfigurationFilePath);
    }
}
