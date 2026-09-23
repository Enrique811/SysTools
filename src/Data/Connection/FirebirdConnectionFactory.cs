using System.Data.Common;
using FirebirdSql.Data.FirebirdClient;
using SysTools.Entities.Configuration;

namespace SysTools.Data.Connection;

public sealed class FirebirdConnectionFactory : IFirebirdConnectionFactory
{
    public DbConnection Create(AppConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var builder = new FbConnectionStringBuilder
        {
            DataSource = configuration.IpEmpresa,
            Database = configuration.RutaEmpresa,
            UserID = configuration.Usuario,
            Password = configuration.Password,
            Port = FirebirdConnectionDefaults.Port,
            Charset = FirebirdConnectionDefaults.Charset,
            Dialect = FirebirdConnectionDefaults.Dialect,
            ConnectionTimeout = FirebirdConnectionDefaults.ConnectionTimeoutSeconds,
            Pooling = FirebirdConnectionDefaults.Pooling
        };

        return new FbConnection(builder.ConnectionString);
    }
}
