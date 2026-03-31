using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class updAssembly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QualityValuesJson",
                table: "Assemblies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceDataJson",
                table: "Assemblies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QualityValuesJson",
                table: "Assemblies");

            migrationBuilder.DropColumn(
                name: "TraceDataJson",
                table: "Assemblies");
        }
    }
}
