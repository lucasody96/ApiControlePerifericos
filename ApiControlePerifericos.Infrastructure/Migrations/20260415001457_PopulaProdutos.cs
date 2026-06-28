using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiControlePerifericos.Migrations
{
    /// <inheritdoc />
    public partial class PopulaProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder mb)
        {
            mb.Sql("INSERT INTO Produtos (ProdutoId, Descricao, SaldoAtual, EstoqueMinimo) VALUES (1, 'TESTE', 40, 2);");
            mb.Sql("INSERT INTO Produtos (ProdutoId, Descricao, SaldoAtual, EstoqueMinimo) VALUES (2, 'PILHA DURACELL AA', 19, 4);");
            mb.Sql("INSERT INTO Produtos (ProdutoId, Descricao, SaldoAtual, EstoqueMinimo) VALUES (3, 'PILHA DURACEL AAA (PALITO)', 4, 0);");
            mb.Sql("INSERT INTO Produtos (ProdutoId, Descricao, SaldoAtual, EstoqueMinimo) VALUES (4, 'MOUSE SEM FIO', 5, 0);");
            mb.Sql("INSERT INTO Produtos (ProdutoId, Descricao, SaldoAtual, EstoqueMinimo) VALUES (5, 'HEADSET', 1, 2);");
            mb.Sql("INSERT INTO Produtos (ProdutoId, Descricao, SaldoAtual, EstoqueMinimo) VALUES (6, 'MOUSE COM FIO', 6, 0);");
            mb.Sql("INSERT INTO Produtos (ProdutoId, Descricao, SaldoAtual, EstoqueMinimo) VALUES (7, 'ADAPTADOR HDMI/VGA', 1, 0);");
            mb.Sql("INSERT INTO Produtos (ProdutoId, Descricao, SaldoAtual, EstoqueMinimo) VALUES (8, 'IMPRESSORA BEMATECH', 1, 1);");
            mb.Sql("INSERT INTO Produtos (ProdutoId, Descricao, SaldoAtual, EstoqueMinimo) VALUES (9, 'CARREGADOR NOTEBOOK (DELL PRETO)', 1, 1);");
            mb.Sql("INSERT INTO Produtos (ProdutoId, Descricao, SaldoAtual, EstoqueMinimo) VALUES (10, 'TECLADO', 6, 2);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder mb)
        {
            mb.Sql("DELETE FROM Produtos WHERE ProdutoId IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10);");
        }
    }
}
