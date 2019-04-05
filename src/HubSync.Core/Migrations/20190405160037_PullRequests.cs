using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HubSync.Migrations
{
    public partial class PullRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPullRequest",
                table: "Issues",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Draft",
                table: "Issues",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MergeCommitSha",
                table: "Issues",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "MergedAt",
                table: "Issues",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Base_Ref",
                table: "Issues",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Base_Sha",
                table: "Issues",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Head_Ref",
                table: "Issues",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Head_Sha",
                table: "Issues",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ReviewRequest",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    PullRequestId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewRequest_Issues_PullRequestId",
                        column: x => x.PullRequestId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ReviewRequest_Actors_UserId",
                        column: x => x.UserId,
                        principalTable: "Actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewRequest_PullRequestId",
                table: "ReviewRequest",
                column: "PullRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewRequest_UserId",
                table: "ReviewRequest",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReviewRequest");

            migrationBuilder.DropColumn(
                name: "IsPullRequest",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Draft",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "MergeCommitSha",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "MergedAt",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Base_Ref",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Base_Sha",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Head_Ref",
                table: "Issues");

            migrationBuilder.DropColumn(
                name: "Head_Sha",
                table: "Issues");
        }
    }
}
