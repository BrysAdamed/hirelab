using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireLab.Modules.Sessions.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InterviewSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "sessions");

            migrationBuilder.CreateTable(
                name: "interview_sessions",
                schema: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    queue_revision = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_interview_sessions", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "session_tasks",
                schema: "sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    task_type_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    content_schema_version = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    summary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    candidate_content = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    staff_content = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    reference_answer = table.Column<string>(type: "text", nullable: false),
                    interviewer_notes = table.Column<string>(type: "text", nullable: true),
                    rubric = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_session_tasks", x => x.id);
                    table.ForeignKey(
                        name: "fk_session_tasks_interview_sessions_session_id",
                        column: x => x.session_id,
                        principalSchema: "sessions",
                        principalTable: "interview_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_interview_sessions_candidate_id_number",
                schema: "sessions",
                table: "interview_sessions",
                columns: new[] { "candidate_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_interview_sessions_team_id_state",
                schema: "sessions",
                table: "interview_sessions",
                columns: new[] { "team_id", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_session_tasks_session_id_position",
                schema: "sessions",
                table: "session_tasks",
                columns: new[] { "session_id", "position" });

            migrationBuilder.CreateIndex(
                name: "ix_session_tasks_task_id",
                schema: "sessions",
                table: "session_tasks",
                column: "task_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "session_tasks",
                schema: "sessions");

            migrationBuilder.DropTable(
                name: "interview_sessions",
                schema: "sessions");
        }
    }
}
