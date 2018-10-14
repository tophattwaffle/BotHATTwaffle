using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BotHATTwaffle.src.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActiveMutes",
                columns: table => new
                {
                    snowflake = table.Column<long>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    mute_duration = table.Column<int>(nullable: false),
                    mute_reason = table.Column<string>(nullable: true),
                    muted_by = table.Column<string>(nullable: true),
                    muted_time = table.Column<long>(nullable: false),
                    username = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActiveMutes", x => x.snowflake);
                });

            migrationBuilder.CreateTable(
                name: "CommandUsage",
                columns: table => new
                {
                    seq_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    command = table.Column<string>(nullable: true),
                    date = table.Column<long>(nullable: false),
                    fullmessage = table.Column<string>(nullable: true),
                    snowflake = table.Column<long>(nullable: false),
                    username = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommandUsage", x => x.seq_id);
                });

            migrationBuilder.CreateTable(
                name: "KeyVaules",
                columns: table => new
                {
                    key = table.Column<string>(nullable: false),
                    value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyVaules", x => x.key);
                });

            migrationBuilder.CreateTable(
                name: "Mutes",
                columns: table => new
                {
                    seq_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<long>(nullable: false),
                    mute_duration = table.Column<int>(nullable: false),
                    mute_reason = table.Column<string>(nullable: true),
                    muted_by = table.Column<string>(nullable: true),
                    snowflake = table.Column<long>(nullable: false),
                    username = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Mutes", x => x.seq_id);
                });

            migrationBuilder.CreateTable(
                name: "SearchDataResults",
                columns: table => new
                {
                    name = table.Column<string>(nullable: false),
                    url = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchDataResults", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    name = table.Column<string>(nullable: false),
                    address = table.Column<string>(nullable: true),
                    description = table.Column<string>(nullable: true),
                    ftp_password = table.Column<string>(nullable: true),
                    ftp_path = table.Column<string>(nullable: true),
                    ftp_type = table.Column<string>(nullable: true),
                    ftp_username = table.Column<string>(nullable: true),
                    rcon_password = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.name);
                });

            migrationBuilder.CreateTable(
                name: "Shitposts",
                columns: table => new
                {
                    seq_id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    date = table.Column<long>(nullable: false),
                    fullmessage = table.Column<string>(nullable: true),
                    shitpost = table.Column<string>(nullable: true),
                    snowflake = table.Column<long>(nullable: false),
                    username = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shitposts", x => x.seq_id);
                });

            migrationBuilder.CreateTable(
                name: "SearchDataTags",
                columns: table => new
                {
                    name = table.Column<string>(nullable: false),
                    tag = table.Column<string>(nullable: false),
                    series = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchDataTags", x => new { x.name, x.tag, x.series });
                    table.ForeignKey(
                        name: "FK_SearchDataTags_SearchDataResults_name",
                        column: x => x.name,
                        principalTable: "SearchDataResults",
                        principalColumn: "name",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActiveMutes");

            migrationBuilder.DropTable(
                name: "CommandUsage");

            migrationBuilder.DropTable(
                name: "KeyVaules");

            migrationBuilder.DropTable(
                name: "Mutes");

            migrationBuilder.DropTable(
                name: "SearchDataTags");

            migrationBuilder.DropTable(
                name: "Servers");

            migrationBuilder.DropTable(
                name: "Shitposts");

            migrationBuilder.DropTable(
                name: "SearchDataResults");
        }
    }
}
