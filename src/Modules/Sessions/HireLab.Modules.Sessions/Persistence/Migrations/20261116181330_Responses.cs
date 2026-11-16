using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireLab.Modules.Sessions.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Responses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "responses",
                schema: "sessions",
                columns: table => new
                {
                    session_task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<JsonElement>(type: "jsonb", nullable: false),
                    response_schema_version = table.Column<int>(type: "integer", nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_responses", x => x.session_task_id);
                    table.ForeignKey(
                        name: "fk_responses_session_task_session_task_id",
                        column: x => x.session_task_id,
                        principalSchema: "sessions",
                        principalTable: "session_tasks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_responses_session_id",
                schema: "sessions",
                table: "responses",
                column: "session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "responses",
                schema: "sessions");
        }
    }
}
