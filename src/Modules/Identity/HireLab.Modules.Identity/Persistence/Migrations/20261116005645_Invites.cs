using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireLab.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Invites : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "invites",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: true),
                    new_team_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invites", x => x.id);
                    table.CheckConstraint("ck_invites_team_matches_role", "(role = 'Admin' AND team_id IS NULL AND new_team_name IS NULL) OR (role = 'Manager' AND (team_id IS NULL) <> (new_team_name IS NULL)) OR (role = 'Interviewer' AND team_id IS NOT NULL AND new_team_name IS NULL)");
                    table.ForeignKey(
                        name: "fk_invites_teams_team_id",
                        column: x => x.team_id,
                        principalSchema: "identity",
                        principalTable: "teams",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_invites_users_invited_by",
                        column: x => x.invited_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_invites_invited_by",
                schema: "identity",
                table: "invites",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "ix_invites_one_open_per_email",
                schema: "identity",
                table: "invites",
                column: "normalized_email",
                unique: true,
                filter: "used_at IS NULL AND revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_invites_team_id",
                schema: "identity",
                table: "invites",
                column: "team_id");

            migrationBuilder.CreateIndex(
                name: "ix_invites_token_hash",
                schema: "identity",
                table: "invites",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invites",
                schema: "identity");
        }
    }
}
