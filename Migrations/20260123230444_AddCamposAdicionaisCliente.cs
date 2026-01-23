using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaIR.Migrations
{
    /// <inheritdoc />
    public partial class AddCamposAdicionaisCliente : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorDaPasta",
                table: "Clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Observacoes",
                table: "Clientes",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefoneComercial",
                table: "Clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefoneResidencial",
                table: "Clientes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$Ie.Hv2iNR8M/O4fjMg0gsOgHVm9i/y1PIz/K0uPL0AsrH4QxIoAZy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorDaPasta",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "Observacoes",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TelefoneComercial",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TelefoneResidencial",
                table: "Clientes");

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$Ka6kbcg7eEwN7HvmysLtjewPEKgoxgkGVAnmhcoN7oOmhp/MKDTrG");
        }
    }
}
