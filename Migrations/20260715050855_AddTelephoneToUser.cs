using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlfactiveParfum.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddTelephoneToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Telephone",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Telephone",
                table: "Users");
        }
    }
}
