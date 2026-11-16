using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireLab.Modules.Sessions.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CandidateLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "candidate_link_created_at",
                schema: "sessions",
                table: "interview_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "candidate_link_created_by",
                schema: "sessions",
                table: "interview_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "candidate_link_id",
                schema: "sessions",
                table: "interview_sessions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "candidate_link_token_hash",
                schema: "sessions",
                table: "interview_sessions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_interview_sessions_candidate_link_token_hash",
                schema: "sessions",
                table: "interview_sessions",
                column: "candidate_link_token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_interview_sessions_candidate_link_token_hash",
                schema: "sessions",
                table: "interview_sessions");

            migrationBuilder.DropColumn(
                name: "candidate_link_created_at",
                schema: "sessions",
                table: "interview_sessions");

            migrationBuilder.DropColumn(
                name: "candidate_link_created_by",
                schema: "sessions",
                table: "interview_sessions");

            migrationBuilder.DropColumn(
                name: "candidate_link_id",
                schema: "sessions",
                table: "interview_sessions");

            migrationBuilder.DropColumn(
                name: "candidate_link_token_hash",
                schema: "sessions",
                table: "interview_sessions");
        }
    }
}
