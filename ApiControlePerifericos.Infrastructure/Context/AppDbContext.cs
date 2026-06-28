using ApiControlePerifericos.Models;
using ApiControlePerifericos.Models.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApiControlePerifericos.Context
{
    public class AppDbContext : IdentityDbContext<ApplicationUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<Colaborador> Colaboradores { get; set; }
        public DbSet<Produto> Produtos { get; set; }
        public DbSet<Movimentacao> Movimentacoes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // A busca por nome/descricao deve ser case-insensitive. O TiDB cria colunas
            // de texto com collation utf8mb4_bin (case-sensitive) por padrao, diferente do
            // MySQL local; fixamos um collation case-insensitive nessas colunas para o
            // comportamento da busca ser igual em qualquer banco.
            builder.Entity<Colaborador>()
                .Property(c => c.Nome)
                .UseCollation("utf8mb4_general_ci");

            builder.Entity<Produto>()
                .Property(p => p.Descricao)
                .UseCollation("utf8mb4_general_ci");
        }
    }
}
