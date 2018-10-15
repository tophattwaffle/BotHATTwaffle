using Microsoft.EntityFrameworkCore.Migrations;

namespace BotHATTwaffle.src.Migrations
{
    public partial class TypoFixes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
@"update shitposts
set shitpost = 'Pakrat'
where shitpost = 'PakRat';");

            migrationBuilder.Sql(
@"update commandusage
set command = 'PenguinFact'
where command = 'PenguineFact';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
@"update shitposts
set shitpost = 'PakRat'
where shitpost = 'Pakrat';");

            migrationBuilder.Sql(
@"update commandusage
set command = 'PenguineFact'
where command = 'PenguinFact';");
        }
    }
}
