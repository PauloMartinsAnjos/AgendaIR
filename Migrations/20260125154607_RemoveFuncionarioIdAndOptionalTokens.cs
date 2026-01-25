using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AgendaIR.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFuncionarioIdAndOptionalTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Funcionarios_FuncionarioId",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Funcionarios_FuncionarioResponsavelId",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_FuncionarioId",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_MagicToken",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "FuncionarioId",
                table: "Clientes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TokenGeradoEm",
                table: "Clientes",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<string>(
                name: "MagicToken",
                table: "Clientes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<int>(
                name: "FuncionarioResponsavelId",
                table: "Clientes",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$dqrRGa54R4OWDWoeHCyK0O.AECI5gzmX2oXPlJseufQJjwSz.IDO.");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_MagicToken",
                table: "Clientes",
                column: "MagicToken");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Funcionarios_FuncionarioResponsavelId",
                table: "Clientes",
                column: "FuncionarioResponsavelId",
                principalTable: "Funcionarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_Funcionarios_FuncionarioResponsavelId",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_MagicToken",
                table: "Clientes");

            migrationBuilder.AlterColumn<DateTime>(
                name: "TokenGeradoEm",
                table: "Clientes",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MagicToken",
                table: "Clientes",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FuncionarioResponsavelId",
                table: "Clientes",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "FuncionarioId",
                table: "Clientes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Funcionarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "SenhaHash",
                value: "$2a$11$SEBiEJ1CHcbuMGYbVnq50.GKVtsGivIz3eB8M9OXWhfYeZBExHCp2");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_FuncionarioId",
                table: "Clientes",
                column: "FuncionarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_MagicToken",
                table: "Clientes",
                column: "MagicToken",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Funcionarios_FuncionarioId",
                table: "Clientes",
                column: "FuncionarioId",
                principalTable: "Funcionarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_Funcionarios_FuncionarioResponsavelId",
                table: "Clientes",
                column: "FuncionarioResponsavelId",
                principalTable: "Funcionarios",
                principalColumn: "Id");
        }
    }
}
