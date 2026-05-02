using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Traffic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSignalDecisionCommandPhaseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "selected_phase_id",
                table: "signal_decision_commands",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "selected_signal_ids_json",
                table: "signal_decision_commands",
                type: "text",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "selected_phase_id",
                table: "signal_decision_commands");

            migrationBuilder.DropColumn(
                name: "selected_signal_ids_json",
                table: "signal_decision_commands");
        }
    }
}
