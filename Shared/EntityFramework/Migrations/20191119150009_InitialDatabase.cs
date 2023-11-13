using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Shared.Migrations
{
    using System.Diagnostics.CodeAnalysis;

    [ExcludeFromCodeCoverage]
    public partial class InitialDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConnectionStringType",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionStringType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectionStringConfiguration",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    externalIdentifier = table.Column<string>(nullable: false),
                    ConnectionStringTypeId = table.Column<int>(nullable: false),
                    connectionString = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectionStringConfiguration", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ConnectionStringConfiguration_ConnectionStringType_ConnectionStringTypeId",
                        column: x => x.ConnectionStringTypeId,
                        principalTable: "ConnectionStringType",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionStringConfiguration_ConnectionStringTypeId",
                table: "ConnectionStringConfiguration",
                column: "ConnectionStringTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectionStringConfiguration_externalIdentifier_ConnectionStringTypeId",
                table: "ConnectionStringConfiguration",
                columns: new[] { "externalIdentifier", "ConnectionStringTypeId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConnectionStringConfiguration");

            migrationBuilder.DropTable(
                name: "ConnectionStringType");
        }
    }
}
