namespace SysTools.Presentation.Shell.Services;

public interface IAsyncModuleLifecycle
{
    Task ActivateAsync(CancellationToken cancellationToken = default);
    void Deactivate();
}

