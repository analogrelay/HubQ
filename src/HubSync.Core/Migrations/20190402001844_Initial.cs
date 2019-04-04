using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HubSync.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actors",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    NodeId = table.Column<string>(nullable: true),
                    GitHubId = table.Column<long>(nullable: false),
                    Login = table.Column<string>(nullable: false),
                    AvatarUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    NodeId = table.Column<string>(nullable: true),
                    GitHubId = table.Column<long>(nullable: false),
                    Owner = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    NodeId = table.Column<string>(nullable: true),
                    RepositoryId = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Color = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Labels_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Milestones",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    NodeId = table.Column<string>(nullable: true),
                    RepositoryId = table.Column<int>(nullable: false),
                    Number = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    DueOn = table.Column<DateTimeOffset>(nullable: true),
                    State = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Milestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Milestones_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SyncLog",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    RepositoryId = table.Column<int>(nullable: false),
                    User = table.Column<string>(nullable: false),
                    Started = table.Column<DateTimeOffset>(nullable: false),
                    WaterMark = table.Column<DateTimeOffset>(nullable: false),
                    Completed = table.Column<DateTimeOffset>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SyncLog_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    NodeId = table.Column<string>(nullable: true),
                    GitHubId = table.Column<long>(nullable: false),
                    RepositoryId = table.Column<int>(nullable: false),
                    Number = table.Column<int>(nullable: false),
                    Title = table.Column<string>(nullable: false),
                    Body = table.Column<string>(nullable: false),
                    State = table.Column<string>(nullable: true),
                    Locked = table.Column<bool>(nullable: false),
                    CommentCount = table.Column<int>(nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(nullable: true),
                    Reactions_TotalCount = table.Column<int>(nullable: false),
                    Reactions_Plus1 = table.Column<int>(nullable: false),
                    Reactions_Minus1 = table.Column<int>(nullable: false),
                    Reactions_Confused = table.Column<int>(nullable: false),
                    Reactions_Heart = table.Column<int>(nullable: false),
                    Reactions_Hooray = table.Column<int>(nullable: false),
                    Reactions_Laugh = table.Column<int>(nullable: false),
                    AuthorId = table.Column<int>(nullable: false),
                    MilestoneId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Issues_Actors_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Issues_Milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "Milestones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Issues_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssueAssignees",
                columns: table => new
                {
                    IssueId = table.Column<int>(nullable: false),
                    AssigneeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueAssignees", x => new { x.IssueId, x.AssigneeId });
                    table.ForeignKey(
                        name: "FK_IssueAssignees_Actors_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "Actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueAssignees_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IssueLabels",
                columns: table => new
                {
                    LabelId = table.Column<int>(nullable: false),
                    IssueId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueLabels", x => new { x.IssueId, x.LabelId });
                    table.ForeignKey(
                        name: "FK_IssueLabels_Issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IssueLabels_Labels_LabelId",
                        column: x => x.LabelId,
                        principalTable: "Labels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actors_GitHubId",
                table: "Actors",
                column: "GitHubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IssueAssignees_AssigneeId",
                table: "IssueAssignees",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueLabels_LabelId",
                table: "IssueLabels",
                column: "LabelId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_AuthorId",
                table: "Issues",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_GitHubId",
                table: "Issues",
                column: "GitHubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issues_MilestoneId",
                table: "Issues",
                column: "MilestoneId");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_RepositoryId",
                table: "Issues",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_RepositoryId",
                table: "Labels",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Milestones_RepositoryId",
                table: "Milestones",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_GitHubId",
                table: "Repositories",
                column: "GitHubId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncLog_RepositoryId",
                table: "SyncLog",
                column: "RepositoryId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IssueAssignees");

            migrationBuilder.DropTable(
                name: "IssueLabels");

            migrationBuilder.DropTable(
                name: "SyncLog");

            migrationBuilder.DropTable(
                name: "Issues");

            migrationBuilder.DropTable(
                name: "Labels");

            migrationBuilder.DropTable(
                name: "Actors");

            migrationBuilder.DropTable(
                name: "Milestones");

            migrationBuilder.DropTable(
                name: "Repositories");
        }
    }
}
