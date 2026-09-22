using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CvManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionJobDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "Positions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EmploymentType",
                table: "Positions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Experience",
                table: "Positions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "WorkMode",
                table: "Positions",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Department",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "EmploymentType",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "Experience",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "WorkMode",
                table: "Positions");
        }
    }
}
