using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedbackSorter.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FeatureCategories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Name = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FeatureCategories", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserFeedbacks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                Text = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                SubmittedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                AnalysisStatus = table.Column<string>(type: "TEXT", nullable: false),
                RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                AnalysisResultTitle = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                AnalysisResultSentiment = table.Column<string>(type: "TEXT", nullable: true),
                AnalysisResultAnalyzedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastFailureDetailsReason = table.Column<string>(type: "TEXT", nullable: true),
                LastFailureDetailsMessage = table.Column<string>(type: "TEXT", nullable: true),
                LastFailureDetailsOccurredAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                LastFailureDetailsAttemptNumber = table.Column<int>(type: "INTEGER", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserFeedbacks", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "UserFeedbackAnalysisResultFeatureCategories",
            columns: table => new
            {
                AnalysisResultFeatureCategoriesId = table.Column<Guid>(type: "TEXT", nullable: false),
                UserFeedbackDbId = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserFeedbackAnalysisResultFeatureCategories", x => new { x.AnalysisResultFeatureCategoriesId, x.UserFeedbackDbId });
                table.ForeignKey(
                    name: "FK_UserFeedbackAnalysisResultFeatureCategories_FeatureCategories_AnalysisResultFeatureCategoriesId",
                    column: x => x.AnalysisResultFeatureCategoriesId,
                    principalTable: "FeatureCategories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserFeedbackAnalysisResultFeatureCategories_UserFeedbacks_UserFeedbackDbId",
                    column: x => x.UserFeedbackDbId,
                    principalTable: "UserFeedbacks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserFeedbackSelectedCategories",
            columns: table => new
            {
                UserFeedbackDbId = table.Column<Guid>(type: "TEXT", nullable: false),
                FeedbackCategoryValue = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserFeedbackSelectedCategories", x => new { x.UserFeedbackDbId, x.FeedbackCategoryValue });
                table.ForeignKey(
                    name: "FK_UserFeedbackSelectedCategories_UserFeedbacks_UserFeedbackDbId",
                    column: x => x.UserFeedbackDbId,
                    principalTable: "UserFeedbacks",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserFeedbackAnalysisResultFeatureCategories_UserFeedbackDbId",
            table: "UserFeedbackAnalysisResultFeatureCategories",
            column: "UserFeedbackDbId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "UserFeedbackAnalysisResultFeatureCategories");

        migrationBuilder.DropTable(
            name: "UserFeedbackSelectedCategories");

        migrationBuilder.DropTable(
            name: "FeatureCategories");

        migrationBuilder.DropTable(
            name: "UserFeedbacks");
    }
}
