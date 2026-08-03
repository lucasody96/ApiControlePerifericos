using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApiControlePerifericos.Extensions
{
    /// <summary>
    /// Registro dos serviços de aplicação. Separado do AddInfrastructure porque estes
    /// são regras de negócio, não detalhe de infraestrutura — a camada que os declara
    /// é a mesma que os implementa.
    /// </summary>
    public static class ApplicationExtensions
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddScoped<IEstoqueService, EstoqueService>();

            return services;
        }
    }
}
