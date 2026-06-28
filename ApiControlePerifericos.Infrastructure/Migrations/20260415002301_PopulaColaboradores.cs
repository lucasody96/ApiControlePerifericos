using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiControlePerifericos.Migrations
{
    /// <inheritdoc />
    public partial class PopulaColaboradores : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (1, 'ADMINISTRADOR');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (2, 'ALICE OLIVEIRA');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (3, 'ALVARO FAGUNDES');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (4, 'ARIANA FREITAG');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (5, 'CAINAM RIBEIRO');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (6, 'FERNANDA VALLE');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (7, 'GABRIELLA PIZZOL');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (8, 'HENRIQUE JAEGER');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (9, 'JESSICA GARCIA');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (10, 'JUAN DA SILVA');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (11, 'LARISSA PACHECO');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (12, 'LUCAS DA ROCHA');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (13, 'LUCAS ODY');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (14, 'MAICON DA SILVA');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (15, 'MARCOS KRANZ');");   
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (16, 'MARIA GABRIELLA');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (17, 'NICHOLAS SIMÃO');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (18, 'NICOLAS LERMEN');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (19, 'PETERSON GEHLEN');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (20, 'RYAN WAGNER');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (21, 'SAMUEL ZANATTA');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (22, 'TAMIRES FONSECA');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (23, 'TASSYANA SIMOES');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (24, 'THAYLLA SOARES');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (25, 'TIAGO PRESOTTO');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (26, 'VICTOR AMORIM');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (27, 'VINÍCIOS SCHONS');");
            migrationBuilder.Sql("INSERT INTO Colaboradores (ColaboradorId, Nome) VALUES (28, 'YAGO BARRETO');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM Colaboradores WHERE ColaboradorId IN (1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28);");
        }
    }
}
