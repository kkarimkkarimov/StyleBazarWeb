using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Stylebazar.DAL.Migrations
{
    /// <inheritdoc />
    public partial class StoryVideoSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Stories",
                newName: "MediaUrl");

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "Stories",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "Stories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "Stories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "Stories");

            migrationBuilder.RenameColumn(
                name: "MediaUrl",
                table: "Stories",
                newName: "ImageUrl");
        }
    }
}
