using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireLab.Modules.TaskBank.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TaskStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "taskbank");

            migrationBuilder.CreateTable(
                name: "tasks",
                schema: "taskbank",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    task_type_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    active_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
                    archived_by = table.Column<Guid>(type: "uuid", nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tasks", x => x.id);
                    table.CheckConstraint("ck_tasks_owner", "(is_system AND team_id IS NULL AND slug IS NOT NULL AND visibility = 'Global') OR (NOT is_system AND team_id IS NOT NULL AND slug IS NULL)");
                });

            migrationBuilder.CreateTable(
                name: "task_versions",
                schema: "taskbank",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    state = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    level = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    estimated_minutes = table.Column<int>(type: "integer", nullable: false),
                    tags = table.Column<string[]>(type: "text[]", nullable: false),
                    reference_answer = table.Column<string>(type: "text", nullable: false),
                    interviewer_notes = table.Column<string>(type: "text", nullable: true),
                    rubric = table.Column<string>(type: "jsonb", nullable: false),
                    candidate_content = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    staff_content = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    content_schema_version = table.Column<int>(type: "integer", nullable: false),
                    content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_by = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_task_versions", x => x.id);
                    table.CheckConstraint("ck_task_versions_published", "(state = 'Published') = (published_at IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_task_versions_tasks_task_id",
                        column: x => x.task_id,
                        principalSchema: "taskbank",
                        principalTable: "tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_task_versions_tags",
                schema: "taskbank",
                table: "task_versions",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_task_versions_task_id_draft",
                schema: "taskbank",
                table: "task_versions",
                column: "task_id",
                unique: true,
                filter: "state = 'Draft'");

            migrationBuilder.CreateIndex(
                name: "ix_task_versions_task_id_number",
                schema: "taskbank",
                table: "task_versions",
                columns: new[] { "task_id", "number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_slug",
                schema: "taskbank",
                table: "tasks",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tasks_team_id",
                schema: "taskbank",
                table: "tasks",
                column: "team_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "task_versions",
                schema: "taskbank");

            migrationBuilder.DropTable(
                name: "tasks",
                schema: "taskbank");
        }
    }
}
