using System.Reflection;
using SysTools.Business.Support;

namespace SysTools.Data.Support;

public sealed class AssemblyApplicationVersionProvider : IApplicationVersionProvider
{
    public string Version => Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.0.0";
}

