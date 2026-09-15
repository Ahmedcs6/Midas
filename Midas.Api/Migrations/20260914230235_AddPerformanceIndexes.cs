using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Midas.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notification_UserId",
                table: "Notification");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_UserId_RevokedAt",
                table: "Sessions",
                columns: new[] { "UserId", "RevokedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_Post_PublishDate",
                table: "Post",
                column: "PublishDate");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserId_IsRead_CreatedAt",
                table: "Notification",
                columns: new[] { "UserId", "IsRead", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_UserId_RevokedAt",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_ExpiresAt",
                table: "RefreshTokens");

            migrationBuilder.DropIndex(
                name: "IX_Post_PublishDate",
                table: "Post");

            migrationBuilder.DropIndex(
                name: "IX_Notification_UserId_IsRead_CreatedAt",
                table: "Notification");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_UserId",
                table: "Notification",
                column: "UserId");
        }
    }
}
