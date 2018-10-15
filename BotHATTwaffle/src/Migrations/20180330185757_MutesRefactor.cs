using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BotHATTwaffle.src.Migrations
{
    public partial class MutesRefactor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            @"
                create table mutes_new (
                    id         integer primary key,
                    user_id    integer not null,
                    user_name  text    not null,
                    reason     text,
                    duration   integer,
                    muter_name text    not null,
                    timestamp  integer not null,
                    expired    integer not null check (expired in (0, 1)) default 0);
            ");

            migrationBuilder.Sql(
            @"
                insert into mutes_new (
                    user_id,
                    user_name,
                    reason,
                    duration,
                    muter_name,
                    timestamp,
                    expired)
                select
                    snowflake,
                    username,
                    mute_reason,
                    mute_duration,
                    muted_by,
                    date,
                    1
                from mutes
                where
                    username is not null and
                    muted_by is not null
                order by date;
            ");

            migrationBuilder.DropTable(
                name: "mutes");

            migrationBuilder.Sql(
            @"
                insert or replace into mutes_new (
                    user_id,
                    user_name,
                    reason,
                    duration,
                    muter_name,
                    timestamp)
                select
                    snowflake,
                    username,
                    mute_reason,
                    mute_duration,
                    muted_by,
                    muted_time
                from activemutes
                where
                    username is not null and
                    muted_by is not null;
            ");

            migrationBuilder.DropTable(
                name: "ActiveMutes");

            migrationBuilder.RenameTable(
                name: "mutes_new",
                newName: "mutes");

            migrationBuilder.CreateIndex(
                name: "IX_mutes_user_id_expired",
                table: "mutes",
                columns: new[] { "user_id", "expired" },
                unique: true,
                filter: "expired == 0");

            migrationBuilder.CreateIndex(
                name: "IX_mutes_user_id_timestamp",
                table: "mutes",
                columns: new[] { "user_id", "timestamp" },
                unique: true);

            migrationBuilder.Sql(
            @"
                update commandusage
                set command = 'MuteHistory'
                where command == 'MuteStatus'
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "mutes",
                newName: "mutes_new");

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

            migrationBuilder.Sql(
            @"
                insert into mutes (
                    snowflake,
                    username,
                    mute_reason,
                    mute_duration,
                    muted_by,
                    date)
                select
                    user_id,
                    user_name,
                    reason,
                    duration,
                    muter_name,
                    timestamp
                from (
                    select
                        user_id,
                        user_name,
                        reason,
                        duration,
                        muter_name,
                        timestamp,
                        expired
                    from mutes_new
                    where expired == 1)
                order by timestamp;
            ");

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

            migrationBuilder.Sql(
            @"
                insert into activemutes (
                    snowflake,
                    username,
                    mute_reason,
                    mute_duration,
                    muted_by,
                    muted_time)
                select
                    user_id,
                    user_name,
                    reason,
                    duration,
                    muter_name,
                    timestamp
                from (
                    select
                        user_id,
                        user_name,
                        reason,
                        duration,
                        muter_name,
                        timestamp,
                        expired
                    from mutes_new
                    where expired == 0)
                order by timestamp;
            ");

            migrationBuilder.DropIndex(
                name: "IX_mutes_user_id_expired",
                table: "mutes");

            migrationBuilder.DropIndex(
                name: "IX_mutes_user_id_timestamp",
                table: "mutes");

            migrationBuilder.DropTable(
                name: "mutes_new");

            migrationBuilder.Sql(
            @"
                update commandusage
                set command = 'MuteStatus'
                where command == 'MuteHistory'
            ");
        }
    }
}
