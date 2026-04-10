using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyFinance_App.Migrations
{
    /// <inheritdoc />
    public partial class AddFeesToBank : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Fees",
                table: "Banks",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Fees",
                table: "Banks");
        }
    }
}
