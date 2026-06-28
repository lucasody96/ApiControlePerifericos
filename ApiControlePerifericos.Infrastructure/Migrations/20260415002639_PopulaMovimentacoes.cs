using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiControlePerifericos.Migrations
{
    /// <inheritdoc />
    public partial class PopulaMovimentacoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ColaboradorId original 11 (Lucas da Rocha) agora é 12
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (8, 2, 12, 'S', 1, '2025-11-10 00:00:00.000');");
            // ColaboradorId original 16 (Nicholas Simão) agora é 17
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (9, 2, 17, 'S', 1, '2025-11-25 00:00:00.000');");
            // ColaboradorId original 22 (Tamires) mantém 22
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (10, 2, 22, 'S', 1, '2025-11-19 00:00:00.000');");
            // ColaboradorId original 24 mantém 24
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (11, 2, 24, 'S', 1, '2025-11-10 00:00:00.000');");
            // ColaboradorId original 10 (Larissa) agora é 11
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (12, 2, 11, 'S', 1, '2025-12-02 00:00:00.000');");
            // ColaboradorId original 15 (Maria) agora é 16
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (13, 2, 16, 'S', 1, '2025-12-02 00:00:00.000');");
            // ColaboradorId original 28 mantém 28
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (14, 2, 28, 'S', 1, '2025-12-10 00:00:00.000');");
            // ColaboradorId original 5 (Fernanda) agora é 6
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (15, 2, 6, 'S', 1, '2026-01-12 00:00:00.000');");
            // ColaboradorId original 10 (Larissa) agora é 11
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (16, 2, 11, 'S', 1, '2026-01-07 00:00:00.000');");
            // ColaboradorId original 18 (Peterson) agora é 19
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (17, 2, 19, 'S', 1, '2026-01-20 00:00:00.000');");
            // ColaboradorId original 20 (Samuel) agora é 21
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (18, 2, 21, 'S', 1, '2026-01-23 00:00:00.000');");
            // ColaboradorId original 5 (Fernanda) agora é 6
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (19, 2, 6, 'S', 1, '2026-02-10 00:00:00.000');");
            // ColaboradorId original 9 (Juan) agora é 10
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (20, 2, 10, 'S', 1, '2026-02-03 00:00:00.000');");
            // ColaboradorId original 10 (Larissa) agora é 11
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (21, 2, 11, 'S', 1, '2026-02-11 00:00:00.000');");
            // ColaboradorId original 25 mantém 25
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (22, 2, 25, 'S', 1, '2026-02-05 00:00:00.000');");
            // ColaboradorId original 29 (Administrador) agora é 1
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (34, 6, 1, 'E', 6, '2026-03-05 10:26:52.487');");
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (35, 4, 1, 'E', 5, '2026-03-05 10:28:27.190');");
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (46, 1, 1, 'E', 1, '2026-03-05 13:40:20.997');");
            // ColaboradorId original 12 (Lucas Ody) agora é 13
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (47, 1, 13, 'S', 2, '2026-03-05 13:40:45.203');");
            // ColaboradorId original 29 (Administrador) agora é 1
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (48, 1, 1, 'A', 3, '2026-03-05 13:41:00.330');");
            // ColaboradorId original 5 (Fernanda) agora é 6
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (49, 2, 6, 'S', 1, '2026-03-20 15:08:47.893');");
            // ColaboradorId original 29 (Administrador) agora é 1
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (50, 5, 1, 'E', 3, '2026-03-20 15:22:04.823');");
            // ColaboradorId original 24 mantém 24
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (51, 5, 24, 'S', 1, '2026-03-23 08:13:39.587');");
            // ColaboradorId original 27 mantém 27
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (52, 5, 27, 'S', 1, '2026-03-23 08:13:56.000');");
            // ColaboradorId original 1 (Alice) agora é 2
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (53, 2, 2, 'S', 1, '2026-03-24 14:16:05.393');");
            // ColaboradorId original 4 (Cainam) agora é 5
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (54, 2, 5, 'S', 1, '2026-03-24 14:16:33.753');");
            // ColaboradorId original 19 (Ryan) agora é 20
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (55, 2, 20, 'S', 1, '2026-03-24 14:16:48.960');");
            // ColaboradorId original 26 mantém 26
            migrationBuilder.Sql("INSERT INTO Movimentacoes (Id, ProdutoId, ColaboradorId, Tipo, Quantidade, DataMovimentacao) VALUES (56, 2, 26, 'S', 1, '2026-04-07 00:00:00.000');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Movimentacoes WHERE Id IN (8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 34, 35, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56);");
        }
    }
}
