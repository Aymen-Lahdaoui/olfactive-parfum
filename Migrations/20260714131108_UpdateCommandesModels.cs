using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OlfactiveParfum.Backend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCommandesModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticlesCommandes_Commandes_CommandeId",
                table: "ArticlesCommandes");

            migrationBuilder.AddColumn<string>(
                name: "ClientNom",
                table: "Commandes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "Commandes",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AlterColumn<string>(
                name: "CommandeId",
                table: "ArticlesCommandes",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddForeignKey(
                name: "FK_ArticlesCommandes_Commandes_CommandeId",
                table: "ArticlesCommandes",
                column: "CommandeId",
                principalTable: "Commandes",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ArticlesCommandes_Commandes_CommandeId",
                table: "ArticlesCommandes");

            migrationBuilder.DropColumn(
                name: "ClientNom",
                table: "Commandes");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "Commandes");

            migrationBuilder.AlterColumn<string>(
                name: "CommandeId",
                table: "ArticlesCommandes",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ArticlesCommandes_Commandes_CommandeId",
                table: "ArticlesCommandes",
                column: "CommandeId",
                principalTable: "Commandes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
