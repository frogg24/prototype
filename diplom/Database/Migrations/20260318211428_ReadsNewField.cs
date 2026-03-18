using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Database.Migrations
{
    /// <inheritdoc />
    public partial class ReadsNewField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseOrder",
                table: "Reads",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QualityValuesJson",
                table: "Reads",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TraceDataJson",
                table: "Reads",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseOrder",
                table: "Reads");

            migrationBuilder.DropColumn(
                name: "QualityValuesJson",
                table: "Reads");

            migrationBuilder.DropColumn(
                name: "TraceDataJson",
                table: "Reads");
        }
    }
}
