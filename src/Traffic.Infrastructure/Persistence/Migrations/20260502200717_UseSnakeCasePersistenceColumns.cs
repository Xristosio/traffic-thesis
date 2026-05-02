using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Traffic.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UseSnakeCasePersistenceColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_signal_state_items_signal_state_snapshots_SnapshotId",
                table: "signal_state_items");

            migrationBuilder.RenameColumn(
                name: "Departures",
                table: "traffic_measurements",
                newName: "departures");

            migrationBuilder.RenameColumn(
                name: "Arrivals",
                table: "traffic_measurements",
                newName: "arrivals");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "traffic_measurements",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SignalId",
                table: "traffic_measurements",
                newName: "signal_id");

            migrationBuilder.RenameColumn(
                name: "RunId",
                table: "traffic_measurements",
                newName: "run_id");

            migrationBuilder.RenameColumn(
                name: "QueueLength",
                table: "traffic_measurements",
                newName: "queue_length");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "traffic_measurements",
                newName: "message_id");

            migrationBuilder.RenameColumn(
                name: "MeasuredAtUtc",
                table: "traffic_measurements",
                newName: "measured_at_utc");

            migrationBuilder.RenameColumn(
                name: "IntersectionId",
                table: "traffic_measurements",
                newName: "intersection_id");

            migrationBuilder.RenameIndex(
                name: "IX_traffic_measurements_RunId",
                table: "traffic_measurements",
                newName: "IX_traffic_measurements_run_id");

            migrationBuilder.RenameIndex(
                name: "IX_traffic_measurements_MessageId",
                table: "traffic_measurements",
                newName: "IX_traffic_measurements_message_id");

            migrationBuilder.RenameIndex(
                name: "IX_traffic_measurements_IntersectionId_SignalId_MeasuredAtUtc",
                table: "traffic_measurements",
                newName: "IX_traffic_measurements_intersection_id_signal_id_measured_at_~");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "signal_state_snapshots",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "RunId",
                table: "signal_state_snapshots",
                newName: "run_id");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "signal_state_snapshots",
                newName: "message_id");

            migrationBuilder.RenameColumn(
                name: "IntersectionId",
                table: "signal_state_snapshots",
                newName: "intersection_id");

            migrationBuilder.RenameColumn(
                name: "CapturedAtUtc",
                table: "signal_state_snapshots",
                newName: "captured_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_signal_state_snapshots_RunId",
                table: "signal_state_snapshots",
                newName: "IX_signal_state_snapshots_run_id");

            migrationBuilder.RenameIndex(
                name: "IX_signal_state_snapshots_MessageId",
                table: "signal_state_snapshots",
                newName: "IX_signal_state_snapshots_message_id");

            migrationBuilder.RenameIndex(
                name: "IX_signal_state_snapshots_IntersectionId_CapturedAtUtc",
                table: "signal_state_snapshots",
                newName: "IX_signal_state_snapshots_intersection_id_captured_at_utc");

            migrationBuilder.RenameColumn(
                name: "State",
                table: "signal_state_items",
                newName: "state");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "signal_state_items",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "SnapshotId",
                table: "signal_state_items",
                newName: "snapshot_id");

            migrationBuilder.RenameColumn(
                name: "SignalId",
                table: "signal_state_items",
                newName: "signal_id");

            migrationBuilder.RenameColumn(
                name: "QueueLength",
                table: "signal_state_items",
                newName: "queue_length");

            migrationBuilder.RenameColumn(
                name: "ElapsedSeconds",
                table: "signal_state_items",
                newName: "elapsed_seconds");

            migrationBuilder.RenameIndex(
                name: "IX_signal_state_items_SnapshotId",
                table: "signal_state_items",
                newName: "IX_signal_state_items_snapshot_id");

            migrationBuilder.RenameColumn(
                name: "Policy",
                table: "signal_decision_commands",
                newName: "policy");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "signal_decision_commands",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "YellowDurationSeconds",
                table: "signal_decision_commands",
                newName: "yellow_duration_seconds");

            migrationBuilder.RenameColumn(
                name: "SelectedSignalId",
                table: "signal_decision_commands",
                newName: "selected_signal_id");

            migrationBuilder.RenameColumn(
                name: "RunId",
                table: "signal_decision_commands",
                newName: "run_id");

            migrationBuilder.RenameColumn(
                name: "MessageId",
                table: "signal_decision_commands",
                newName: "message_id");

            migrationBuilder.RenameColumn(
                name: "IssuedAtUtc",
                table: "signal_decision_commands",
                newName: "issued_at_utc");

            migrationBuilder.RenameColumn(
                name: "IntersectionId",
                table: "signal_decision_commands",
                newName: "intersection_id");

            migrationBuilder.RenameColumn(
                name: "GreenDurationSeconds",
                table: "signal_decision_commands",
                newName: "green_duration_seconds");

            migrationBuilder.RenameIndex(
                name: "IX_signal_decision_commands_RunId",
                table: "signal_decision_commands",
                newName: "IX_signal_decision_commands_run_id");

            migrationBuilder.RenameIndex(
                name: "IX_signal_decision_commands_MessageId",
                table: "signal_decision_commands",
                newName: "IX_signal_decision_commands_message_id");

            migrationBuilder.RenameIndex(
                name: "IX_signal_decision_commands_IntersectionId_IssuedAtUtc",
                table: "signal_decision_commands",
                newName: "IX_signal_decision_commands_intersection_id_issued_at_utc");

            migrationBuilder.RenameColumn(
                name: "Scenario",
                table: "experiment_runs",
                newName: "scenario");

            migrationBuilder.RenameColumn(
                name: "Policy",
                table: "experiment_runs",
                newName: "policy");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "experiment_runs",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "StartedAtUtc",
                table: "experiment_runs",
                newName: "started_at_utc");

            migrationBuilder.RenameColumn(
                name: "FinishedAtUtc",
                table: "experiment_runs",
                newName: "finished_at_utc");

            migrationBuilder.RenameIndex(
                name: "IX_experiment_runs_StartedAtUtc",
                table: "experiment_runs",
                newName: "IX_experiment_runs_started_at_utc");

            migrationBuilder.AddForeignKey(
                name: "FK_signal_state_items_signal_state_snapshots_snapshot_id",
                table: "signal_state_items",
                column: "snapshot_id",
                principalTable: "signal_state_snapshots",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_signal_state_items_signal_state_snapshots_snapshot_id",
                table: "signal_state_items");

            migrationBuilder.RenameColumn(
                name: "departures",
                table: "traffic_measurements",
                newName: "Departures");

            migrationBuilder.RenameColumn(
                name: "arrivals",
                table: "traffic_measurements",
                newName: "Arrivals");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "traffic_measurements",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "signal_id",
                table: "traffic_measurements",
                newName: "SignalId");

            migrationBuilder.RenameColumn(
                name: "run_id",
                table: "traffic_measurements",
                newName: "RunId");

            migrationBuilder.RenameColumn(
                name: "queue_length",
                table: "traffic_measurements",
                newName: "QueueLength");

            migrationBuilder.RenameColumn(
                name: "message_id",
                table: "traffic_measurements",
                newName: "MessageId");

            migrationBuilder.RenameColumn(
                name: "measured_at_utc",
                table: "traffic_measurements",
                newName: "MeasuredAtUtc");

            migrationBuilder.RenameColumn(
                name: "intersection_id",
                table: "traffic_measurements",
                newName: "IntersectionId");

            migrationBuilder.RenameIndex(
                name: "IX_traffic_measurements_run_id",
                table: "traffic_measurements",
                newName: "IX_traffic_measurements_RunId");

            migrationBuilder.RenameIndex(
                name: "IX_traffic_measurements_message_id",
                table: "traffic_measurements",
                newName: "IX_traffic_measurements_MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_traffic_measurements_intersection_id_signal_id_measured_at_~",
                table: "traffic_measurements",
                newName: "IX_traffic_measurements_IntersectionId_SignalId_MeasuredAtUtc");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "signal_state_snapshots",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "run_id",
                table: "signal_state_snapshots",
                newName: "RunId");

            migrationBuilder.RenameColumn(
                name: "message_id",
                table: "signal_state_snapshots",
                newName: "MessageId");

            migrationBuilder.RenameColumn(
                name: "intersection_id",
                table: "signal_state_snapshots",
                newName: "IntersectionId");

            migrationBuilder.RenameColumn(
                name: "captured_at_utc",
                table: "signal_state_snapshots",
                newName: "CapturedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_signal_state_snapshots_run_id",
                table: "signal_state_snapshots",
                newName: "IX_signal_state_snapshots_RunId");

            migrationBuilder.RenameIndex(
                name: "IX_signal_state_snapshots_message_id",
                table: "signal_state_snapshots",
                newName: "IX_signal_state_snapshots_MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_signal_state_snapshots_intersection_id_captured_at_utc",
                table: "signal_state_snapshots",
                newName: "IX_signal_state_snapshots_IntersectionId_CapturedAtUtc");

            migrationBuilder.RenameColumn(
                name: "state",
                table: "signal_state_items",
                newName: "State");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "signal_state_items",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "snapshot_id",
                table: "signal_state_items",
                newName: "SnapshotId");

            migrationBuilder.RenameColumn(
                name: "signal_id",
                table: "signal_state_items",
                newName: "SignalId");

            migrationBuilder.RenameColumn(
                name: "queue_length",
                table: "signal_state_items",
                newName: "QueueLength");

            migrationBuilder.RenameColumn(
                name: "elapsed_seconds",
                table: "signal_state_items",
                newName: "ElapsedSeconds");

            migrationBuilder.RenameIndex(
                name: "IX_signal_state_items_snapshot_id",
                table: "signal_state_items",
                newName: "IX_signal_state_items_SnapshotId");

            migrationBuilder.RenameColumn(
                name: "policy",
                table: "signal_decision_commands",
                newName: "Policy");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "signal_decision_commands",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "yellow_duration_seconds",
                table: "signal_decision_commands",
                newName: "YellowDurationSeconds");

            migrationBuilder.RenameColumn(
                name: "selected_signal_id",
                table: "signal_decision_commands",
                newName: "SelectedSignalId");

            migrationBuilder.RenameColumn(
                name: "run_id",
                table: "signal_decision_commands",
                newName: "RunId");

            migrationBuilder.RenameColumn(
                name: "message_id",
                table: "signal_decision_commands",
                newName: "MessageId");

            migrationBuilder.RenameColumn(
                name: "issued_at_utc",
                table: "signal_decision_commands",
                newName: "IssuedAtUtc");

            migrationBuilder.RenameColumn(
                name: "intersection_id",
                table: "signal_decision_commands",
                newName: "IntersectionId");

            migrationBuilder.RenameColumn(
                name: "green_duration_seconds",
                table: "signal_decision_commands",
                newName: "GreenDurationSeconds");

            migrationBuilder.RenameIndex(
                name: "IX_signal_decision_commands_run_id",
                table: "signal_decision_commands",
                newName: "IX_signal_decision_commands_RunId");

            migrationBuilder.RenameIndex(
                name: "IX_signal_decision_commands_message_id",
                table: "signal_decision_commands",
                newName: "IX_signal_decision_commands_MessageId");

            migrationBuilder.RenameIndex(
                name: "IX_signal_decision_commands_intersection_id_issued_at_utc",
                table: "signal_decision_commands",
                newName: "IX_signal_decision_commands_IntersectionId_IssuedAtUtc");

            migrationBuilder.RenameColumn(
                name: "scenario",
                table: "experiment_runs",
                newName: "Scenario");

            migrationBuilder.RenameColumn(
                name: "policy",
                table: "experiment_runs",
                newName: "Policy");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "experiment_runs",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "started_at_utc",
                table: "experiment_runs",
                newName: "StartedAtUtc");

            migrationBuilder.RenameColumn(
                name: "finished_at_utc",
                table: "experiment_runs",
                newName: "FinishedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_experiment_runs_started_at_utc",
                table: "experiment_runs",
                newName: "IX_experiment_runs_StartedAtUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_signal_state_items_signal_state_snapshots_SnapshotId",
                table: "signal_state_items",
                column: "SnapshotId",
                principalTable: "signal_state_snapshots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
