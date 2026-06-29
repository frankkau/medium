using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Authentication.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantBrandingAndUIFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactEmail",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContactPhone",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "FaviconUrl",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogoUrl",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MissionStatement",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Motto",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhysicalAddress",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Tenants",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VisionStatement",
                table: "Tenants",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactEmail",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ContactPhone",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "FaviconUrl",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LogoUrl",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MissionStatement",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Motto",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PhysicalAddress",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "VisionStatement",
                table: "Tenants");
        }
    }
}
