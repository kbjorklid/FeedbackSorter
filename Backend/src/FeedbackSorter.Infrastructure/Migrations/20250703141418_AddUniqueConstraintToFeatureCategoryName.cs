using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FeedbackSorter.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintToFeatureCategoryName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_FeatureCategories_Name",
                table: "FeatureCategories",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeatureCategories_Name",
                table: "FeatureCategories");
        }
    }
}
