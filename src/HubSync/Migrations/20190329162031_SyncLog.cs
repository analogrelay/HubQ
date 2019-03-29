using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HubSync.Migrations
{
    public partial class SyncLog : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SyncLog",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Owner = table.Column<string>(nullable: false),
                    Repository = table.Column<string>(nullable: false),
                    User = table.Column<string>(nullable: false),
                    Started = table.Column<DateTimeOffset>(nullable: false),
                    WaterMark = table.Column<DateTimeOffset>(nullable: false),
                    Completed = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLog", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SyncLog");
        }
    }
}
