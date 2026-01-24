using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaIR.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleMeetAndCalendarFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adicionar campos em TiposAgendamento
            migrationBuilder.AddColumn<string>(
                name: "Local",
                table: "TiposAgendamento",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CriarGoogleMeet",
                table: "TiposAgendamento",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CorCalendario",
                table: "TiposAgendamento",
                type: "integer",
                nullable: false,
                defaultValue: 6);

            migrationBuilder.AddColumn<bool>(
                name: "BloqueiaHorario",
                table: "TiposAgendamento",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Adicionar campo em Agendamentos
            migrationBuilder.AddColumn<string>(
                name: "ConferenciaUrl",
                table: "Agendamentos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Local",
                table: "TiposAgendamento");

            migrationBuilder.DropColumn(
                name: "CriarGoogleMeet",
                table: "TiposAgendamento");

            migrationBuilder.DropColumn(
                name: "CorCalendario",
                table: "TiposAgendamento");

            migrationBuilder.DropColumn(
                name: "BloqueiaHorario",
                table: "TiposAgendamento");

            migrationBuilder.DropColumn(
                name: "ConferenciaUrl",
                table: "Agendamentos");
        }
    }
}
