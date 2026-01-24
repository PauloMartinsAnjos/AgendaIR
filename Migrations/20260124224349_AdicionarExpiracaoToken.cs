using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaIR.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarExpiracaoToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "TokenAtivo",
                table: "Clientes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TokenExpiracao",
                table: "Clientes",
                type: "timestamp without time zone",
                nullable: true);

            // Atualizar tokens existentes (8 horas a partir de agora)
            migrationBuilder.Sql(@"
                UPDATE ""Clientes"" 
                SET 
                    ""TokenExpiracao"" = NOW() + INTERVAL '8 hours',
                    ""TokenAtivo"" = true
                WHERE ""MagicToken"" IS NOT NULL;
            ");

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$77F2vr86WOTzfJjX.nqFueEr2T4rsciqSb0lG6GBeZ3HRq39ltx12");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TokenAtivo",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TokenExpiracao",
                table: "Clientes");

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$k54frV3GiDz/1AM5D98uXuYRYB6qkC.htKGNr8nMJITslBls5C07.");
        }
    }
}
