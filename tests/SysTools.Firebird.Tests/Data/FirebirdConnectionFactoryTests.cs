using System.Data;
using FirebirdSql.Data.FirebirdClient;
using SysTools.Data.Connection;
using SysTools.Firebird.Tests.TestDoubles;

namespace SysTools.Firebird.Tests.Data;

public sealed class FirebirdConnectionFactoryTests
{
    [Fact]
    public void Create_maps_configuration_and_fixed_connection_settings()
    {
        var configuration = FirebirdTestConfiguration.Valid("  password with spaces  ");
        var factory = new FirebirdConnectionFactory();

        using var connection = factory.Create(configuration);
        var builder = new FbConnectionStringBuilder(connection.ConnectionString);

        Assert.Equal(configuration.IpEmpresa, builder.DataSource);
        Assert.Equal(configuration.RutaEmpresa, builder.Database);
        Assert.Equal(configuration.Usuario, builder.UserID);
        Assert.Equal(configuration.Password, builder.Password);
        Assert.Equal(3050, builder.Port);
        Assert.Equal("ISO8859_1", builder.Charset);
        Assert.Equal(3, builder.Dialect);
        Assert.Equal(5, builder.ConnectionTimeout);
        Assert.True(builder.Pooling);
        Assert.Equal(ConnectionState.Closed, connection.State);
    }

    [Fact]
    public void Create_returns_independent_closed_instances()
    {
        var factory = new FirebirdConnectionFactory();
        var configuration = FirebirdTestConfiguration.Valid();

        using var first = factory.Create(configuration);
        using var second = factory.Create(configuration);

        Assert.NotSame(first, second);
        Assert.IsType<FbConnection>(first);
        Assert.IsType<FbConnection>(second);
        Assert.Equal(ConnectionState.Closed, first.State);
        Assert.Equal(ConnectionState.Closed, second.State);
    }
}
