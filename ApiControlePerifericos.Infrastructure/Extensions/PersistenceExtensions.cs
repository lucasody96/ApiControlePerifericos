using ApiControlePerifericos.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ApiControlePerifericos.Extensions
{
    /// <summary>
    /// Registro do acesso a dados. Concentra aqui a escolha do provider para que a
    /// camada de apresentacao nao precise conhecer qual banco esta por tras.
    /// </summary>
    public static class PersistenceExtensions
    {
        public static IServiceCollection AddPersistence(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // TiDB Cloud e MySQL-compativel e reporta o protocolo MySQL 8.x. Fixamos a versao
            // (em vez de ServerVersion.AutoDetect) para nao depender de uma conexao no startup
            // so para detecta-la e evitar atrito com a string de versao propria do TiDB.
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 11));

            services.AddDbContext<AppDbContext>(options =>
                options.UseMySql(connectionString, serverVersion));

            return services;
        }
    }
}
