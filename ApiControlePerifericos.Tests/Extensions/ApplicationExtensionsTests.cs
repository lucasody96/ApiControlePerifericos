using ApiControlePerifericos.Extensions;
using ApiControlePerifericos.Interfaces;
using ApiControlePerifericos.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ApiControlePerifericos.Tests.Extensions
{
    // Os serviços de aplicação saíram da Program.cs junto com a infraestrutura (issue #32),
    // mas ficaram em AddApplication: o EstoqueService é regra de negócio, não detalhe de
    // infraestrutura. Aqui só o registro importa — o comportamento está em EstoqueServiceTests.
    public class ApplicationExtensionsTests
    {
        [Fact]
        public void AddApplication_RegistraOEstoqueServiceComoScoped()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddApplication();

            // Assert — scoped porque depende do IUnitOfWork, que vive no escopo da requisição
            var registro = Assert.Single(services, s => s.ServiceType == typeof(IEstoqueService));
            Assert.Equal(typeof(EstoqueService), registro.ImplementationType);
            Assert.Equal(ServiceLifetime.Scoped, registro.Lifetime);
        }
    }
}
