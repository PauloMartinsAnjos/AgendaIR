using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaIR.Migrations
{
    /// <inheritdoc />
    public partial class AdicionarConteudoComprimido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CaminhoArquivo",
                table: "DocumentosAnexados",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<byte[]>(
                name: "ConteudoComprimido",
                table: "DocumentosAnexados",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<long>(
                name: "TamanhoComprimidoBytes",
                table: "DocumentosAnexados",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "TamanhoOriginalBytes",
                table: "DocumentosAnexados",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 1,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 22, 20, 1, 11, 41, DateTimeKind.Utc).AddTicks(5240));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 2,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 22, 20, 1, 11, 41, DateTimeKind.Utc).AddTicks(5242));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 3,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 22, 20, 1, 11, 41, DateTimeKind.Utc).AddTicks(5243));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 4,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 22, 20, 1, 11, 41, DateTimeKind.Utc).AddTicks(5245));

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DataCriacao", "SenhaHash" },
                values: new object[] { new DateTime(2026, 1, 22, 20, 1, 11, 41, DateTimeKind.Utc).AddTicks(4965), "$2a$11$677f2ojxVRcGoscmh/kVKezZ.25H48bnkvvyUZuH7yOOkWWX/cGU2" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConteudoComprimido",
                table: "DocumentosAnexados");

            migrationBuilder.DropColumn(
                name: "TamanhoComprimidoBytes",
                table: "DocumentosAnexados");

            migrationBuilder.DropColumn(
                name: "TamanhoOriginalBytes",
                table: "DocumentosAnexados");

            migrationBuilder.AlterColumn<string>(
                name: "CaminhoArquivo",
                table: "DocumentosAnexados",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 1,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 22, 15, 37, 34, 881, DateTimeKind.Utc).AddTicks(9664));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 2,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 22, 15, 37, 34, 881, DateTimeKind.Utc).AddTicks(9666));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 3,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 22, 15, 37, 34, 881, DateTimeKind.Utc).AddTicks(9668));

            migrationBuilder.UpdateData(
                table: "DocumentosSolicitados",
                keyColumn: "Id",
                keyValue: 4,
                column: "DataCriacao",
                value: new DateTime(2026, 1, 22, 15, 37, 34, 881, DateTimeKind.Utc).AddTicks(9670));

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "DataCriacao", "SenhaHash" },
                values: new object[] { new DateTime(2026, 1, 22, 15, 37, 34, 881, DateTimeKind.Utc).AddTicks(9426), "$2a$11$tZz81Ot26TnIqGqIXFGgTeJe4Uiw8RGNyDq1MJ4CE6ojrFnvrE.VG" });
        }
    }
}
