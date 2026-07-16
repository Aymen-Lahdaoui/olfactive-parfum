using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace OlfactiveParfum.Backend.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserEmail   = table.Column<string>(type: "text", nullable: false),
                    Titre       = table.Column<string>(type: "text", nullable: false),
                    Message     = table.Column<string>(type: "text", nullable: false),
                    Type        = table.Column<string>(type: "text", nullable: false, defaultValue: "info"),
                    CommandeId  = table.Column<int>(type: "integer", nullable: true),
                    IsRead      = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt   = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserEmail",
                table: "Notifications",
                column: "UserEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Notifications");
        }
    }
}
