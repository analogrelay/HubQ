using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace HubSync.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    GitHubId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Owner = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                    table.UniqueConstraint("AK_Repositories_GitHubId", x => x.GitHubId);
                    table.UniqueConstraint("AK_Repositories_Owner_Name", x => new { x.Owner, x.Name });
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    AvatarUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GitHubId = table.Column<int>(type: "int", nullable: false),
                    Login = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.UniqueConstraint("AK_Users_GitHubId", x => x.GitHubId);
                    table.UniqueConstraint("AK_Users_Login", x => x.Login);
                });

            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RepositoryId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.Id);
                    table.UniqueConstraint("AK_Labels_RepositoryId_Name", x => new { x.RepositoryId, x.Name });
                    table.ForeignKey(
                        name: "FK_Labels_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Number = table.Column<int>(type: "int", nullable: false),
                    RepositoryId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                    table.UniqueConstraint("AK_Milestones_RepositoryId_Number", x => new { x.RepositoryId, x.Number });
                    table.ForeignKey(
                        name: "FK_Milestones_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Agent = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompletedUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepositoryId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncHistory_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ClosedById = table.Column<int>(type: "int", nullable: true),
                    CommentCount = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    GitHubId = table.Column<int>(type: "int", nullable: false),
                    HtmlUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsPr = table.Column<bool>(type: "bit", nullable: false),
                    Locked = table.Column<bool>(type: "bit", nullable: false),
                    MilestoneId = table.Column<int>(type: "int", nullable: true),
                    Number = table.Column<int>(type: "int", nullable: false),
                    PullRequestUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RepositoryId = table.Column<int>(type: "int", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    PullRequest_BaseRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PullRequest_BaseSha = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PullRequest_ChangedFiles = table.Column<int>(type: "int", nullable: false),
                    PullRequest_HeadRef = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PullRequest_HeadSha = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PullRequest_IsPr = table.Column<bool>(type: "bit", nullable: false),
                    PullRequest_Mergeable = table.Column<bool>(type: "bit", nullable: true),
                    PullRequest_MergedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PullRequest_MergedById = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Id);
                    table.UniqueConstraint("AK_Issues_GitHubId", x => x.GitHubId);
                    table.UniqueConstraint("AK_Issues_Number_RepositoryId", x => new { x.Number, x.RepositoryId });
                    table.ForeignKey(
                        name: "FK_Issues_Users_ClosedById",
                        column: x => x.ClosedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Issues_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_Users_PullRequest_MergedById",
                        column: x => x.PullRequest_MergedById,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueAssignees",
                columns: table => new
                {
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueAssignees", x => new { x.IssueId, x.UserId });
                    table.ForeignKey(
                        name: "FK_IssueAssignees_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueAssignees_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueLabels",
                columns: table => new
                {
                    IssueId = table.Column<int>(type: "int", nullable: false),
                    LabelId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueLabels", x => new { x.IssueId, x.LabelId });
                    table.ForeignKey(
                        name: "FK_IssueLabels_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueLabels_Labels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "Labels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignees_UserId",
                table: "IssueAssignees",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueLabels_LabelId",
                table: "IssueLabels",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ClosedById",
                table: "Issues",
                column: "ClosedById");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_MilestoneId",
                table: "Issues",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_RepositoryId",
                table: "Issues",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_UserId",
                table: "Issues",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_PullRequest_MergedById",
                table: "Issues",
                column: "PullRequest_MergedById");

            migrationBuilder.CreateIndex(
                name: "IX_SyncHistory_RepositoryId",
                table: "SyncHistory",
                column: "RepositoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueAssignees");

            migrationBuilder.DropTable(
                name: "IssueLabels");

            migrationBuilder.DropTable(
                name: "SyncHistory");

            migrationBuilder.DropTable(
                name: "Issues");

            migrationBuilder.DropTable(
                name: "Labels");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "Repositories");
        }
    }
}
