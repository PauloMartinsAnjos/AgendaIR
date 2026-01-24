using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AgendaIR.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarParticipantesAgendamento : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "BloqueiaHorario",
                table: "TiposAgendamento",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "CorCalendario",
                table: "TiposAgendamento",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "CriarGoogleMeet",
                table: "TiposAgendamento",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Local",
                table: "TiposAgendamento",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ConferenciaUrl",
                table: "Agendamentos",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgendamentoParticipantes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AgendamentoId = table.Column<int>(type: "integer", nullable: false),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Nome = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgendamentoParticipantes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgendamentoParticipantes_Agendamentos_AgendamentoId",
                        column: x => x.AgendamentoId,
                        principalTable: "Agendamentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$k54frV3GiDz/1AM5D98uXuYRYB6qkC.htKGNr8nMJITslBls5C07.");

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "BloqueiaHorario", "CorCalendario", "CriarGoogleMeet", "Local" },
                values: new object[] { true, 6, false, null });

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "BloqueiaHorario", "CorCalendario", "CriarGoogleMeet", "Local" },
                values: new object[] { true, 6, false, null });

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "BloqueiaHorario", "CorCalendario", "CriarGoogleMeet", "Local" },
                values: new object[] { true, 6, false, null });

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "BloqueiaHorario", "CorCalendario", "CriarGoogleMeet", "Local" },
                values: new object[] { true, 6, false, null });

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "BloqueiaHorario", "CorCalendario", "CriarGoogleMeet", "Local" },
                values: new object[] { true, 6, false, null });

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "BloqueiaHorario", "CorCalendario", "CriarGoogleMeet", "Local" },
                values: new object[] { true, 6, false, null });

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "BloqueiaHorario", "CorCalendario", "CriarGoogleMeet", "Local" },
                values: new object[] { true, 6, false, null });

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "BloqueiaHorario", "CorCalendario", "CriarGoogleMeet", "Local" },
                values: new object[] { true, 6, false, null });

            migrationBuilder.CreateIndex(
                name: "IX_AgendamentoParticipantes_AgendamentoId",
                table: "AgendamentoParticipantes",
                column: "AgendamentoId");
            
            migrationBuilder.CreateIndex(
                name: "IX_AgendamentoParticipantes_Email",
                table: "AgendamentoParticipantes",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgendamentoParticipantes");

            migrationBuilder.DropColumn(
                name: "BloqueiaHorario",
                table: "TiposAgendamento");

            migrationBuilder.DropColumn(
                name: "CorCalendario",
                table: "TiposAgendamento");

            migrationBuilder.DropColumn(
                name: "CriarGoogleMeet",
                table: "TiposAgendamento");

            migrationBuilder.DropColumn(
                name: "Local",
                table: "TiposAgendamento");

            migrationBuilder.DropColumn(
                name: "ConferenciaUrl",
                table: "Agendamentos");

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$Ie.Hv2iNR8M/O4fjMg0gsOgHVm9i/y1PIz/K0uPL0AsrH4QxIoAZy");
        }
    }
}
