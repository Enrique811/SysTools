using Microsoft.Extensions.Logging.Abstractions;
using SysTools.Presentation.Modules.PriceVerifier.ViewModels;
using SysTools.Presentation.Shell.Services;
using SysTools.Presentation.Shell.ViewModels;

namespace SysTools.Presentation.Tests;

internal static class TestDoubles
{
    public static ShellViewModel CreateShell(IModuleInitializer? initializer = null) =>
        new(new PriceVerifierViewModel(), initializer ?? new DefaultModuleInitializer(), NullLogger<ShellViewModel>.Instance);
}

internal sealed class ThrowingInitializer : IModuleInitializer
{
    public void Initialize(string moduleId) => throw new InvalidOperationException("C:\\private\\source.cs internal stack trace");
}
