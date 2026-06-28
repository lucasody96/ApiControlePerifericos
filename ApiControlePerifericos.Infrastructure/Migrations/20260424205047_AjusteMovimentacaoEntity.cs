using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiControlePerifericos.Migrations
{
    /// <inheritdoc />
    public partial class AjusteMovimentacaoEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Movimentacoes",
                newName: "MovimentacaoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "MovimentacaoId",
                table: "Movimentacoes",
                newName: "Id");
        }
    }
}
