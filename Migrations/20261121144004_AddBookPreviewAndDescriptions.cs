using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOK.Migrations
{
    /// <inheritdoc />
    public partial class AddBookPreviewAndDescriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MoTa",
                table: "Sachs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SoTrang",
                table: "Sachs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TrangDocThus",
                columns: table => new
                {
                    MaTrangDocThu = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSach = table.Column<int>(type: "int", nullable: false),
                    SoTrang = table.Column<int>(type: "int", nullable: false),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrangDocThus", x => x.MaTrangDocThu);
                    table.ForeignKey(
                        name: "FK_TrangDocThus_Sachs_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sachs",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TrangDocThus_MaSach_SoTrang",
                table: "TrangDocThus",
                columns: new[] { "MaSach", "SoTrang" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TrangDocThus");

            migrationBuilder.DropColumn(
                name: "MoTa",
                table: "Sachs");

            migrationBuilder.DropColumn(
                name: "SoTrang",
                table: "Sachs");
        }
    }
}
