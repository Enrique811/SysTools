using System.Data.Common;
using SysTools.Entities.Configuration;

namespace SysTools.Data.Connection;

public interface IFirebirdConnectionFactory
{
    DbConnection Create(AppConfiguration configuration);
}
