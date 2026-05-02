using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Traffic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "experiment_runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Policy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Scenario = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    StartedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FinishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_experiment_runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "signal_decision_commands",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntersectionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SelectedSignalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Policy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    GreenDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    YellowDurationSeconds = table.Column<int>(type: "integer", nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signal_decision_commands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "signal_state_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntersectionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CapturedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signal_state_snapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "traffic_measurements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntersectionId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SignalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    QueueLength = table.Column<int>(type: "integer", nullable: false),
                    Arrivals = table.Column<int>(type: "integer", nullable: false),
                    Departures = table.Column<int>(type: "integer", nullable: false),
                    MeasuredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_traffic_measurements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "signal_state_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    SignalId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    State = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ElapsedSeconds = table.Column<int>(type: "integer", nullable: false),
                    QueueLength = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_signal_state_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_signal_state_items_signal_state_snapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "signal_state_snapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_experiment_runs_StartedAtUtc",
                table: "experiment_runs",
                column: "StartedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_signal_decision_commands_IntersectionId_IssuedAtUtc",
                table: "signal_decision_commands",
                columns: new[] { "IntersectionId", "IssuedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_signal_decision_commands_MessageId",
                table: "signal_decision_commands",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_signal_decision_commands_RunId",
                table: "signal_decision_commands",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_signal_state_items_SnapshotId",
                table: "signal_state_items",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_signal_state_snapshots_IntersectionId_CapturedAtUtc",
                table: "signal_state_snapshots",
                columns: new[] { "IntersectionId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_signal_state_snapshots_MessageId",
                table: "signal_state_snapshots",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_signal_state_snapshots_RunId",
                table: "signal_state_snapshots",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_traffic_measurements_IntersectionId_SignalId_MeasuredAtUtc",
                table: "traffic_measurements",
                columns: new[] { "IntersectionId", "SignalId", "MeasuredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_traffic_measurements_MessageId",
                table: "traffic_measurements",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_traffic_measurements_RunId",
                table: "traffic_measurements",
                column: "RunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "experiment_runs");

            migrationBuilder.DropTable(
                name: "signal_decision_commands");

            migrationBuilder.DropTable(
                name: "signal_state_items");

            migrationBuilder.DropTable(
                name: "traffic_measurements");

            migrationBuilder.DropTable(
                name: "signal_state_snapshots");
        }
    }
}
