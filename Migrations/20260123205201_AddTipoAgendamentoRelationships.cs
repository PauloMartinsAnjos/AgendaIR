using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaIR.Migrations
{
    /// <inheritdoc />
    public partial class AddTipoAgendamentoRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoAgendamentoId",
                table: "DocumentosSolicitados",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoAgendamentoId",
                table: "Agendamentos",
                type: "integer",
                nullable: false,
                defaultValue: 8); // Default to "Outros" type

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DataCriacao", "TipoAgendamentoId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null });

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "DataCriacao", "TipoAgendamentoId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null });

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "DataCriacao", "TipoAgendamentoId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null });

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "DataCriacao", "TipoAgendamentoId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), null });

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DataCriacao", "SenhaHash" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "$2a$11$YNvtn6qLFbBdglbhvBoWqOrPeR0qXl1nnScc7pn8.0rbD4ZPiZO2C" });

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 1,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 2,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 3,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 4,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 5,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 6,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 7,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 8,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_DocumentosSolicitados_TipoAgendamentoId",
                table: "DocumentosSolicitados",
                column: "TipoAgendamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Agendamentos_TipoAgendamentoId",
                table: "Agendamentos",
                column: "TipoAgendamentoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Agendamentos_TiposAgendamento_TipoAgendamentoId",
                table: "Agendamentos",
                column: "TipoAgendamentoId",
                principalTable: "TiposAgendamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentosSolicitados_TiposAgendamento_TipoAgendamentoId",
                table: "DocumentosSolicitados",
                column: "TipoAgendamentoId",
                principalTable: "TiposAgendamento",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Agendamentos_TiposAgendamento_TipoAgendamentoId",
                table: "Agendamentos");

            migrationBuilder.DropForeignKey(
                name: "FK_DocumentosSolicitados_TiposAgendamento_TipoAgendamentoId",
                table: "DocumentosSolicitados");

            migrationBuilder.DropIndex(
                name: "IX_DocumentosSolicitados_TipoAgendamentoId",
                table: "DocumentosSolicitados");

            migrationBuilder.DropIndex(
                name: "IX_Agendamentos_TipoAgendamentoId",
                table: "Agendamentos");

            migrationBuilder.DropColumn(
                name: "TipoAgendamentoId",
                table: "DocumentosSolicitados");

            migrationBuilder.DropColumn(
                name: "TipoAgendamentoId",
                table: "Agendamentos");

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 1,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(488));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 2,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(491));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 3,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(493));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 4,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(506));

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DataCriacao", "SenhaHash" },
                values: new object[] { new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(69), "$2a$11$.7N/rN4z8a0pNrocFXzu7.mnmB7IbBNeoPRG/bbfwkQ7QeXMV3G8." });

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 1,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(574));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 2,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(576));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 3,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(578));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 4,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(579));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 5,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(580));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 6,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(581));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 7,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(583));

            migrationBuilder.UpdateData(
                table: "TiposAgendamento",
                keyColumn: "Id",
                keyValue: 8,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 20, 21, 52, 861, DateTimeKind.Utc).AddTicks(584));
        }
    }
}
