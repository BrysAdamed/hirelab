using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireLab.Modules.Positions.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Candidates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "last_candidate_number",
                schema: "positions",
                table: "job_positions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "candidates",
                schema: "positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    job_position_id = table.Column<Guid>(type: "uuid", nullable: false),
                    number = table.Column<int>(type: "integer", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    links = table.Column<string[]>(type: "text[]", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    anonymised_by = table.Column<Guid>(type: "uuid", nullable: true),
                    anonymised_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_candidates", x => x.id);
                    table.ForeignKey(
                        name: "fk_candidates_job_positions_job_position_id",
                        column: x => x.job_position_id,
                        principalSchema: "positions",
                        principalTable: "job_positions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_candidates_job_position_id_number",
                schema: "positions",
                table: "candidates",
                columns: new[] { "job_position_id", "number" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "candidates",
                schema: "positions");

            migrationBuilder.DropColumn(
                name: "last_candidate_number",
                schema: "positions",
                table: "job_positions");
        }
    }
}
