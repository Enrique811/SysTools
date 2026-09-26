namespace SysTools.Configuration.Tests.Support;

internal sealed class SupportLogTestDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(),"SysTools.Support",Guid.NewGuid().ToString("N"));
    public SupportLogTestDirectory(bool create=true) { if(create) Directory.CreateDirectory(Path); }
    public string Write(string name,string content)
    { var path=System.IO.Path.Combine(Path,name);File.WriteAllText(path,content);return path; }
    public void Dispose() { if(Directory.Exists(Path)) Directory.Delete(Path,true); }
}

internal sealed class ProcessStarterStub(bool result=true) : SysTools.Data.Support.IExternalProcessStarter
{
    public List<string> Targets { get; }=[];
    public bool Start(string target) { Targets.Add(target);return result; }
}

