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
        /// <summary>
        /// Nome da connection string esperada na configuracao. Publico porque os testes
        /// verificam que a mensagem de erro cita a chave que o operador precisa corrigir.
        /// </summary>
        public const string NomeConnectionString = "DefaultConnection";

        public static IServiceCollection AddPersistence(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString(NomeConnectionString);

            // Falhar no startup, e nao no primeiro acesso ao banco: no Cloud Run a string
            // vem de variavel de ambiente e, quando ela falta, o driver so reclama la na
            // frente com uma mensagem que nao deixa claro que o problema e configuracao.
            // IsNullOrWhiteSpace (e nao IsNullOrEmpty) porque uma env definida como vazia
            // ou so com espacos cai exatamente no mesmo buraco.
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"A connection string '{NomeConnectionString}' nao foi configurada. " +
                    $"Defina 'ConnectionStrings:{NomeConnectionString}' no appsettings ou nos " +
                    $"user-secrets, ou a variavel de ambiente " +
                    $"'ConnectionStrings__{NomeConnectionString}' no ambiente de deploy.");
            }

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
