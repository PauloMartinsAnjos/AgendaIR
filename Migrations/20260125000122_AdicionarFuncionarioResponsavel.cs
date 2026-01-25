using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaIR.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarFuncionarioResponsavel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FuncionarioResponsavelId",
                table: "Clientes",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$SEBiEJ1CHcbuMGYbVnq50.GKVtsGivIz3eB8M9OXWhfYeZBExHCp2");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_FuncionarioResponsavelId",
                table: "Clientes",
                column: "FuncionarioResponsavelId");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Funcionarios_FuncionarioResponsavelId",
                table: "Clientes",
                column: "FuncionarioResponsavelId",
                principalTable: "Funcionarios",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Funcionarios_FuncionarioResponsavelId",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_FuncionarioResponsavelId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "FuncionarioResponsavelId",
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
