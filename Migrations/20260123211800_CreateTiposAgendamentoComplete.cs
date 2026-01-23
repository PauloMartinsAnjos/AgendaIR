using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace AgendaIR.Migrations
{
    /// <inheritdoc />
    public partial class CreateTiposAgendamentoComplete : Migration
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
                defaultValue: 8);

            migrationBuilder.CreateTable(
                name: "TiposAgendamento",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nome = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "boolean", nullable: false),
                    DataCriacao = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposAgendamento", x => x.Id);
                });

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
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "$2a$11$Ka6kbcg7eEwN7HvmysLtjewPEKgoxgkGVAnmhcoN7oOmhp/MKDTrG" });

            migrationBuilder.InsertData(
                table: "TiposAgendamento",
                columns: new[] { "Id", "Ativo", "DataCriacao", "Descricao", "Nome" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Declaração de Imposto de Renda", "Declaração IR" },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Retificação de declaração de IR", "Declaração IR Retificadora" },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Consultoria sobre questões tributárias", "Consultoria Tributária" },
                    { 4, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Abertura de Microempreendedor Individual", "Abertura de MEI" },
                    { 5, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Serviços contábeis mensais", "Contabilidade Mensal" },
                    { 6, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Regularização de pendências fiscais", "Regularização Fiscal" },
                    { 7, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Planejamento estratégico tributário", "Planejamento Tributário" },
                    { 8, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Outros serviços", "Outros" }
                });

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

            migrationBuilder.DropTable(
                name: "TiposAgendamento");

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
                value: new DateTime(2026, 1, 23, 19, 52, 58, 426, DateTimeKind.Utc).AddTicks(5528));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 2,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 19, 52, 58, 426, DateTimeKind.Utc).AddTicks(5530));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 3,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 19, 52, 58, 426, DateTimeKind.Utc).AddTicks(5532));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 4,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 23, 19, 52, 58, 426, DateTimeKind.Utc).AddTicks(5533));

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DataCriacao", "SenhaHash" },
                values: new object[] { new DateTime(2026, 1, 23, 19, 52, 58, 426, DateTimeKind.Utc).AddTicks(5001), "$2a$11$ZpylGtm/qtxJ6J3/BgJMZOK94CU9Wz4C7sbyGJ5PfrpfcZPVKdTYG" });
        }
    }
}
