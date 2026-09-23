namespace SysTools.Data.Connection;

internal static class FirebirdConnectionDefaults
{
    internal const int Port = 3050;
    internal const string Charset = "ISO8859_1";
    internal const int Dialect = 3;
    internal const int ConnectionTimeoutSeconds = 5;
    internal const bool Pooling = true;
}
