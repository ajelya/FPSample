using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FPSample.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceManagementNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adds the Description column
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Services",
                type: "nvarchar(max)",
                nullable: true);
        }

        // Adds the IsEnabled column with a default value of true (1)
        /*
    migrationBuilder.AddColumn<bool>(
        name: "IsEnabled",
                table: "Services",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }
        */        

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "IsEnabled",
                table: "Services");
        }
    }
}
