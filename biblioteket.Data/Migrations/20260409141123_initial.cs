using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace biblioteket.Data.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Författare",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Namn = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Författare", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inläsare",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Namn = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inläsare", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Böcker",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    LegimusUrl = table.Column<string>(type: "TEXT", nullable: false),
                    HämtatLegimusInformationTidpunkt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Titel = table.Column<string>(type: "TEXT", nullable: true),
                    InläsareId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Böcker", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Böcker_Inläsare_InläsareId",
                        column: x => x.InläsareId,
                        principalTable: "Inläsare",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "BokFörfattare",
                columns: table => new
                {
                    BöckerId = table.Column<int>(type: "INTEGER", nullable: false),
                    FörfattareId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BokFörfattare", x => new { x.BöckerId, x.FörfattareId });
                    table.ForeignKey(
                        name: "FK_BokFörfattare_Böcker_BöckerId",
                        column: x => x.BöckerId,
                        principalTable: "Böcker",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BokFörfattare_Författare_FörfattareId",
                        column: x => x.FörfattareId,
                        principalTable: "Författare",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Nedladdningar",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Tidpunkt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    BokId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nedladdningar", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Nedladdningar_Böcker_BokId",
                        column: x => x.BokId,
                        principalTable: "Böcker",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BokFörfattare_FörfattareId",
                table: "BokFörfattare",
                column: "FörfattareId");

            migrationBuilder.CreateIndex(
                name: "IX_Böcker_InläsareId",
                table: "Böcker",
                column: "InläsareId");

            migrationBuilder.CreateIndex(
                name: "IX_Nedladdningar_BokId",
                table: "Nedladdningar",
                column: "BokId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BokFörfattare");

            migrationBuilder.DropTable(
                name: "Nedladdningar");

            migrationBuilder.DropTable(
                name: "Författare");

            migrationBuilder.DropTable(
                name: "Böcker");

            migrationBuilder.DropTable(
                name: "Inläsare");
        }
    }
}
