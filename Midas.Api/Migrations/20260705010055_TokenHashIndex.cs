using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Midas.Api.Migrations
{
	/// <inheritdoc />
	public partial class TokenHashIndex : Migration
	{
		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_RefreshTokens_AspNetUsers_ApplicationUserId",
				table: "RefreshTokens");

			migrationBuilder.AlterColumn<string>(
				name: "TokenHash",
				table: "RefreshTokens",
				type: "nvarchar(450)",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "nvarchar(max)");

			migrationBuilder.CreateIndex(
				name: "IX_RefreshTokens_TokenHash",
				table: "RefreshTokens",
				column: "TokenHash",
				unique: true);

			migrationBuilder.AddForeignKey(
				name: "FK_RefreshTokens_AspNetUsers_ApplicationUserId",
				table: "RefreshTokens",
				column: "ApplicationUserId",
				principalTable: "AspNetUsers",
				principalColumn: "Id",
				onDelete: ReferentialAction.Cascade);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.DropForeignKey(
				name: "FK_RefreshTokens_AspNetUsers_ApplicationUserId",
				table: "RefreshTokens");

			migrationBuilder.DropIndex(
				name: "IX_RefreshTokens_TokenHash",
				table: "RefreshTokens");

			migrationBuilder.AlterColumn<string>(
				name: "TokenHash",
				table: "RefreshTokens",
				type: "nvarchar(max)",
				nullable: false,
				oldClrType: typeof(string),
				oldType: "nvarchar(450)");

			migrationBuilder.AddForeignKey(
				name: "FK_RefreshTokens_AspNetUsers_ApplicationUserId",
				table: "RefreshTokens",
				column: "ApplicationUserId",
				principalTable: "AspNetUsers",
				principalColumn: "Id");
		}
	}
}
