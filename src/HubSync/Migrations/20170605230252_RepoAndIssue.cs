using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace HubSync.Migrations
{
    public partial class RepoAndIssue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncStatus",
                table: "SyncStatus");

            migrationBuilder.DropIndex(
                name: "IX_SyncStatus_Repo",
                table: "SyncStatus");

            migrationBuilder.DropColumn(
                name: "Repo",
                table: "SyncStatus");

            migrationBuilder.RenameTable(
                name: "SyncStatus",
                newName: "SyncHistory");

            migrationBuilder.AddColumn<int>(
                name: "RepositoryId",
                table: "SyncHistory",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncHistory",
                table: "SyncHistory",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                    table.UniqueConstraint("AK_Repositories_Owner_Name", x => new { x.Owner, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Number = table.Column<int>(type: "int", nullable: false),
                    RepositoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Id);
                    table.UniqueConstraint("AK_Issues_Number_RepositoryId", x => new { x.Number, x.RepositoryId });
                    table.ForeignKey(
                        name: "FK_Issues_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SyncHistory_RepositoryId",
                table: "SyncHistory",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_RepositoryId",
                table: "Issues",
                column: "RepositoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_SyncHistory_Repositories_RepositoryId",
                table: "SyncHistory",
                column: "RepositoryId",
                principalTable: "Repositories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SyncHistory_Repositories_RepositoryId",
                table: "SyncHistory");

            migrationBuilder.DropTable(
                name: "Issues");

            migrationBuilder.DropTable(
                name: "Repositories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SyncHistory",
                table: "SyncHistory");

            migrationBuilder.DropIndex(
                name: "IX_SyncHistory_RepositoryId",
                table: "SyncHistory");

            migrationBuilder.DropColumn(
                name: "RepositoryId",
                table: "SyncHistory");

            migrationBuilder.RenameTable(
                name: "SyncHistory",
                newName: "SyncStatus");

            migrationBuilder.AddColumn<string>(
                name: "Repo",
                table: "SyncStatus",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SyncStatus",
                table: "SyncStatus",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_SyncStatus_Repo",
                table: "SyncStatus",
                column: "Repo");
        }
    }
}
