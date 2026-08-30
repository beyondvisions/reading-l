using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReadingList.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueBookIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Books_Title_Author",
                table: "Books",
                columns: new[] { "Title", "Author" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Books_Title_Author",
                table: "Books");
        }
    }
}
