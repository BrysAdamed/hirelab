using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireLab.Modules.Identity.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ResetLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reset_links",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    issued_by = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_reset_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_reset_links_users_issued_by",
                        column: x => x.issued_by,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_reset_links_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_reset_links_issued_by",
                schema: "identity",
                table: "reset_links",
                column: "issued_by");

            migrationBuilder.CreateIndex(
                name: "ix_reset_links_one_open_per_user",
                schema: "identity",
                table: "reset_links",
                column: "user_id",
                unique: true,
                filter: "used_at IS NULL AND revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_reset_links_token_hash",
                schema: "identity",
                table: "reset_links",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "reset_links",
                schema: "identity");
        }
    }
}
