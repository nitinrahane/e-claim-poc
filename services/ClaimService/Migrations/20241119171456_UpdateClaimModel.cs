using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClaimService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateClaimModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimAmount",
                table: "Claims");

            migrationBuilder.RenameColumn(
                name: "PolicyNumber",
                table: "Claims",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "DateOfClaim",
                table: "Claims",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "ClaimStatus",
                table: "Claims",
                newName: "AssessorId");

            migrationBuilder.AddColumn<decimal>(
                name: "Amount",
                table: "Claims",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ClaimantId",
                table: "Claims",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Claims",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Claims",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "DocumentIds",
                table: "Claims",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Claims",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Claims",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Claims",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Amount",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "ClaimantId",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "DocumentIds",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Claims");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Claims");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Claims",
                newName: "DateOfClaim");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Claims",
                newName: "PolicyNumber");

            migrationBuilder.RenameColumn(
                name: "AssessorId",
                table: "Claims",
                newName: "ClaimStatus");

            migrationBuilder.AddColumn<decimal>(
                name: "ClaimAmount",
                table: "Claims",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
