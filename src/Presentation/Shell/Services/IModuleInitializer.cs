namespace SysTools.Presentation.Shell.Services;

public interface IModuleInitializer
{
    void Initialize(string moduleId);
}

public sealed class DefaultModuleInitializer : IModuleInitializer
{
    public void Initialize(string moduleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleId);
    }
}
