using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiControlePerifericos.Migrations
{
    /// <inheritdoc />
    public partial class MovimentacaoSaldoEstoque : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movimentacoes_Colaboradores_ColaboradorId",
                table: "Movimentacoes");

            migrationBuilder.AlterColumn<int>(
                name: "ColaboradorId",
                table: "Movimentacoes",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "RegistradoPor",
                table: "Movimentacoes",
                type: "varchar(256)",
                maxLength: 256,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddForeignKey(
                name: "FK_Movimentacoes_Colaboradores_ColaboradorId",
                table: "Movimentacoes",
                column: "ColaboradorId",
                principalTable: "Colaboradores",
                principalColumn: "ColaboradorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Movimentacoes_Colaboradores_ColaboradorId",
                table: "Movimentacoes");

            migrationBuilder.DropColumn(
                name: "RegistradoPor",
                table: "Movimentacoes");

            migrationBuilder.AlterColumn<int>(
                name: "ColaboradorId",
                table: "Movimentacoes",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Movimentacoes_Colaboradores_ColaboradorId",
                table: "Movimentacoes",
                column: "ColaboradorId",
                principalTable: "Colaboradores",
                principalColumn: "ColaboradorId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
