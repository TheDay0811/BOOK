using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BOOK.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NhaXuatBans",
                columns: table => new
                {
                    MaNXB = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenNXB = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NhaXuatBans", x => x.MaNXB);
                });

            migrationBuilder.CreateTable(
                name: "TaiKhoans",
                columns: table => new
                {
                    MaTK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoans", x => x.MaTK);
                });

            migrationBuilder.CreateTable(
                name: "TheLoais",
                columns: table => new
                {
                    MaTheLoai = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenTheLoai = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheLoais", x => x.MaTheLoai);
                });

            migrationBuilder.CreateTable(
                name: "TheThuViens",
                columns: table => new
                {
                    MaThe = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoThe = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HoTen = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayHetHan = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TinhTrangSuDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaTK = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TheThuViens", x => x.MaThe);
                    table.ForeignKey(
                        name: "FK_TheThuViens_TaiKhoans_MaTK",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sachs",
                columns: table => new
                {
                    MaSach = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSachCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenSach = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TacGia = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NamXuatBan = table.Column<int>(type: "int", nullable: false),
                    MaTheLoai = table.Column<int>(type: "int", nullable: false),
                    MaNXB = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sachs", x => x.MaSach);
                    table.ForeignKey(
                        name: "FK_Sachs_NhaXuatBans_MaNXB",
                        column: x => x.MaNXB,
                        principalTable: "NhaXuatBans",
                        principalColumn: "MaNXB",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sachs_TheLoais_MaTheLoai",
                        column: x => x.MaTheLoai,
                        principalTable: "TheLoais",
                        principalColumn: "MaTheLoai",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PhieuMuons",
                columns: table => new
                {
                    MaPhieuMuon = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayMuon = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanTra = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaThe = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PhieuMuons", x => x.MaPhieuMuon);
                    table.ForeignKey(
                        name: "FK_PhieuMuons_TheThuViens_MaThe",
                        column: x => x.MaThe,
                        principalTable: "TheThuViens",
                        principalColumn: "MaThe",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DanhGias",
                columns: table => new
                {
                    MaDanhGia = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaSach = table.Column<int>(type: "int", nullable: false),
                    MaTK = table.Column<int>(type: "int", nullable: false),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGias", x => x.MaDanhGia);
                    table.ForeignKey(
                        name: "FK_DanhGias_Sachs_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sachs",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhGias_TaiKhoans_MaTK",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoans",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DauSachs",
                columns: table => new
                {
                    MaDauSach = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SoKiemSoat = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TinhTrang = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaSach = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DauSachs", x => x.MaDauSach);
                    table.ForeignKey(
                        name: "FK_DauSachs_Sachs_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sachs",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauMuons",
                columns: table => new
                {
                    MaYeuCau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaThe = table.Column<int>(type: "int", nullable: false),
                    MaSach = table.Column<int>(type: "int", nullable: false),
                    NgayYeuCau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HanTraDeXuat = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauMuons", x => x.MaYeuCau);
                    table.ForeignKey(
                        name: "FK_YeuCauMuons_Sachs_MaSach",
                        column: x => x.MaSach,
                        principalTable: "Sachs",
                        principalColumn: "MaSach",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_YeuCauMuons_TheThuViens_MaThe",
                        column: x => x.MaThe,
                        principalTable: "TheThuViens",
                        principalColumn: "MaThe",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "YeuCauGiaHans",
                columns: table => new
                {
                    MaYeuCau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaPhieuMuon = table.Column<int>(type: "int", nullable: false),
                    LyDo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NgayYeuCau = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauGiaHans", x => x.MaYeuCau);
                    table.ForeignKey(
                        name: "FK_YeuCauGiaHans_PhieuMuons_MaPhieuMuon",
                        column: x => x.MaPhieuMuon,
                        principalTable: "PhieuMuons",
                        principalColumn: "MaPhieuMuon",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ChiTietPhieuMuons",
                columns: table => new
                {
                    MaChiTiet = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NgayTraThucTe = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TinhTrangTra = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TienBoiThuong = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DaBoiThuong = table.Column<bool>(type: "bit", nullable: false),
                    MaPhieuMuon = table.Column<int>(type: "int", nullable: false),
                    MaDauSach = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChiTietPhieuMuons", x => x.MaChiTiet);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuMuons_DauSachs_MaDauSach",
                        column: x => x.MaDauSach,
                        principalTable: "DauSachs",
                        principalColumn: "MaDauSach",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ChiTietPhieuMuons_PhieuMuons_MaPhieuMuon",
                        column: x => x.MaPhieuMuon,
                        principalTable: "PhieuMuons",
                        principalColumn: "MaPhieuMuon",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuMuons_MaDauSach",
                table: "ChiTietPhieuMuons",
                column: "MaDauSach");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietPhieuMuons_MaPhieuMuon",
                table: "ChiTietPhieuMuons",
                column: "MaPhieuMuon");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGias_MaSach",
                table: "DanhGias",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGias_MaTK",
                table: "DanhGias",
                column: "MaTK");

            migrationBuilder.CreateIndex(
                name: "IX_DauSachs_MaSach",
                table: "DauSachs",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_PhieuMuons_MaThe",
                table: "PhieuMuons",
                column: "MaThe");

            migrationBuilder.CreateIndex(
                name: "IX_Sachs_MaNXB",
                table: "Sachs",
                column: "MaNXB");

            migrationBuilder.CreateIndex(
                name: "IX_Sachs_MaTheLoai",
                table: "Sachs",
                column: "MaTheLoai");

            migrationBuilder.CreateIndex(
                name: "IX_TheThuViens_MaTK",
                table: "TheThuViens",
                column: "MaTK",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauGiaHans_MaPhieuMuon",
                table: "YeuCauGiaHans",
                column: "MaPhieuMuon");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauMuons_MaSach",
                table: "YeuCauMuons",
                column: "MaSach");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauMuons_MaThe",
                table: "YeuCauMuons",
                column: "MaThe");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChiTietPhieuMuons");

            migrationBuilder.DropTable(
                name: "DanhGias");

            migrationBuilder.DropTable(
                name: "YeuCauGiaHans");

            migrationBuilder.DropTable(
                name: "YeuCauMuons");

            migrationBuilder.DropTable(
                name: "DauSachs");

            migrationBuilder.DropTable(
                name: "PhieuMuons");

            migrationBuilder.DropTable(
                name: "Sachs");

            migrationBuilder.DropTable(
                name: "TheThuViens");

            migrationBuilder.DropTable(
                name: "NhaXuatBans");

            migrationBuilder.DropTable(
                name: "TheLoais");

            migrationBuilder.DropTable(
                name: "TaiKhoans");
        }
    }
}
