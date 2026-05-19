using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vitalis.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TB_TUTOR",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    NOME = table.Column<string>(type: "NVARCHAR2(150)", maxLength: 150, nullable: false),
                    CPF = table.Column<string>(type: "NVARCHAR2(11)", maxLength: 11, nullable: false),
                    EMAIL = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    SENHA = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ATIVO = table.Column<bool>(type: "NUMBER(1)", nullable: false),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_TUTOR", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "TB_LEMBRETE",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TUTOR_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    PET_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TIPO = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    DATA_AGENDADA = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    MENSAGEM = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    STATUS = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    REFERENCIA_ID = table.Column<long>(type: "NUMBER(19)", nullable: true),
                    REFERENCIA_TIPO = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    CREATED_AT = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false, defaultValueSql: "SYSDATE")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_LEMBRETE", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TB_LEMBRETE_TB_TUTOR_TUTOR_ID",
                        column: x => x.TUTOR_ID,
                        principalTable: "TB_TUTOR",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_TUTOR_CONTATO",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TUTOR_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    TIPO = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    VALOR = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    PRINCIPAL = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_TUTOR_CONTATO", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TB_TUTOR_CONTATO_TB_TUTOR_TUTOR_ID",
                        column: x => x.TUTOR_ID,
                        principalTable: "TB_TUTOR",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TB_TUTOR_ENDERECO",
                columns: table => new
                {
                    ID = table.Column<long>(type: "NUMBER(19)", nullable: false)
                        .Annotation("Oracle:Identity", "START WITH 1 INCREMENT BY 1"),
                    TUTOR_ID = table.Column<long>(type: "NUMBER(19)", nullable: false),
                    LOGRADOURO = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    NUMERO = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    COMPLEMENTO = table.Column<string>(type: "NVARCHAR2(2000)", nullable: true),
                    BAIRRO = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    CIDADE = table.Column<string>(type: "NVARCHAR2(2000)", nullable: false),
                    ESTADO = table.Column<string>(type: "NVARCHAR2(2)", maxLength: 2, nullable: false),
                    CEP = table.Column<string>(type: "NVARCHAR2(8)", maxLength: 8, nullable: false),
                    PRINCIPAL = table.Column<bool>(type: "NUMBER(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TB_TUTOR_ENDERECO", x => x.ID);
                    table.ForeignKey(
                        name: "FK_TB_TUTOR_ENDERECO_TB_TUTOR_TUTOR_ID",
                        column: x => x.TUTOR_ID,
                        principalTable: "TB_TUTOR",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TB_LEMBRETE_TUTOR_ID",
                table: "TB_LEMBRETE",
                column: "TUTOR_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TB_TUTOR_CONTATO_TUTOR_ID",
                table: "TB_TUTOR_CONTATO",
                column: "TUTOR_ID");

            migrationBuilder.CreateIndex(
                name: "IX_TB_TUTOR_ENDERECO_TUTOR_ID",
                table: "TB_TUTOR_ENDERECO",
                column: "TUTOR_ID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TB_LEMBRETE");

            migrationBuilder.DropTable(
                name: "TB_TUTOR_CONTATO");

            migrationBuilder.DropTable(
                name: "TB_TUTOR_ENDERECO");

            migrationBuilder.DropTable(
                name: "TB_TUTOR");
        }
    }
}
