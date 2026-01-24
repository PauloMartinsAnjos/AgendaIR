using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaIR.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarDocumentosObrigatorios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentosObrigatoriosJson",
                table: "TiposAgendamento",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$RnsBIEkDIvh8PwHKChFHJOKKKYmesEE0V03rQdb7o3xXBjRw6Q26i");

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 1,
                column: "DocumentosObrigatoriosJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 2,
                column: "DocumentosObrigatoriosJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 3,
                column: "DocumentosObrigatoriosJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 4,
                column: "DocumentosObrigatoriosJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 5,
                column: "DocumentosObrigatoriosJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 6,
                column: "DocumentosObrigatoriosJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 7,
                column: "DocumentosObrigatoriosJson",
                value: null);

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 8,
                column: "DocumentosObrigatoriosJson",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DocumentosObrigatoriosJson",
                table: "TiposAgendamento");

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$77F2vr86WOTzfJjX.nqFueEr2T4rsciqSb0lG6GBeZ3HRq39ltx12");
        }
    }
}
