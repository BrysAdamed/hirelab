using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HireLab.Modules.Positions.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ClosePosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "closed_at",
                schema: "positions",
                table: "job_positions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "closed_by",
                schema: "positions",
                table: "job_positions",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "closed_at",
                schema: "positions",
                table: "job_positions");

            migrationBuilder.DropColumn(
                name: "closed_by",
                schema: "positions",
                table: "job_positions");
        }
    }
}
