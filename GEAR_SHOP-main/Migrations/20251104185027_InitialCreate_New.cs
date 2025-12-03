using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TL4_SHOP.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate_New : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatMessages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SenderId = table.Column<int>(type: "int", nullable: false),
                    ReceiverId = table.Column<int>(type: "int", nullable: false),
                    SenderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DanhMucSanPham",
                columns: table => new
                {
                    DanhMucID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenDanhMuc = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DanhMucChaId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DanhMucS__1C53BA7B71EFB0D1", x => x.DanhMucID);
                    table.ForeignKey(
                        name: "FK_DanhMuc_ChaCon",
                        column: x => x.DanhMucChaId,
                        principalTable: "DanhMucSanPham",
                        principalColumn: "DanhMucID");
                });

            migrationBuilder.CreateTable(
                name: "DoanhThuTheoNam",
                columns: table => new
                {
                    Nam = table.Column<int>(type: "int", nullable: false),
                    TongSoDonHang = table.Column<int>(type: "int", nullable: true),
                    TongSoLuong = table.Column<int>(type: "int", nullable: true),
                    TongDoanhThu = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    LoiNhuan = table.Column<decimal>(type: "decimal(18,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DoanhThu__C7D111C2C4C815AF", x => x.Nam);
                });

            migrationBuilder.CreateTable(
                name: "DoanhThuTheoNgay",
                columns: table => new
                {
                    Ngay = table.Column<DateOnly>(type: "date", nullable: false),
                    TongSoDonHang = table.Column<int>(type: "int", nullable: true),
                    TongSoLuong = table.Column<int>(type: "int", nullable: true),
                    TongDoanhThu = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    LoiNhuan = table.Column<decimal>(type: "decimal(18,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DoanhThu__6BCCE7B20FFD82FF", x => x.Ngay);
                });

            migrationBuilder.CreateTable(
                name: "DoanhThuTheoThang",
                columns: table => new
                {
                    Nam = table.Column<int>(type: "int", nullable: false),
                    Thang = table.Column<int>(type: "int", nullable: false),
                    TongSoDonHang = table.Column<int>(type: "int", nullable: true),
                    TongSoLuong = table.Column<int>(type: "int", nullable: true),
                    TongDoanhThu = table.Column<decimal>(type: "decimal(18,3)", nullable: true),
                    LoiNhuan = table.Column<decimal>(type: "decimal(18,3)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DoanhThu__750C5E96CD37571B", x => new { x.Nam, x.Thang });
                });

            migrationBuilder.CreateTable(
                name: "KhachHang",
                columns: table => new
                {
                    KhachHangID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__KhachHan__880F211BBB2BF01B", x => x.KhachHangID);
                });

            migrationBuilder.CreateTable(
                name: "NhaCungCap",
                columns: table => new
                {
                    NhaCungCapID = table.Column<int>(type: "int", nullable: false),
                    TenNhaCungCap = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DiaChi = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__NhaCungC__8B8917272738218A", x => x.NhaCungCapID);
                });

            migrationBuilder.CreateTable(
                name: "NhanVien",
                columns: table => new
                {
                    NhanVienID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    VaiTro = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__NhanVien__E27FD7EA80CBD491", x => x.NhanVienID);
                });

            migrationBuilder.CreateTable(
                name: "TechNews",
                columns: table => new
                {
                    TechNewsId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: true),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CoverImage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Author = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TechNews", x => x.TechNewsId);
                });

            migrationBuilder.CreateTable(
                name: "TrangThaiDonHang",
                columns: table => new
                {
                    TrangThaiID = table.Column<int>(type: "int", nullable: false),
                    TenTrangThai = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TrangTha__D5BF1E85ABD8D594", x => x.TrangThaiID);
                });

            migrationBuilder.CreateTable(
                name: "DiaChi",
                columns: table => new
                {
                    DiaChiID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhachHangID = table.Column<int>(type: "int", nullable: false),
                    TenNguoiNhan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoNha = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PhuongXa = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QuanHuyen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThanhPho = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    QuocGia = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ZipCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DiaChi__94E668E696DC2723", x => x.DiaChiID);
                    table.ForeignKey(
                        name: "FK__DiaChi__KhachHan__6A30C649",
                        column: x => x.KhachHangID,
                        principalTable: "KhachHang",
                        principalColumn: "KhachHangID");
                });

            migrationBuilder.CreateTable(
                name: "SanPham",
                columns: table => new
                {
                    SanPhamID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenSanPham = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    MoTa = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Gia = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    SoLuongTon = table.Column<int>(type: "int", nullable: false),
                    HinhAnh = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DanhMucID = table.Column<int>(type: "int", nullable: true),
                    NhaCungCapID = table.Column<int>(type: "int", nullable: false),
                    LaNoiBat = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    ChiTiet = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GiaSauGiam = table.Column<decimal>(type: "decimal(18,0)", nullable: true),
                    ThongSoKyThuat = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SanPham__05180FF4AC4878EE", x => x.SanPhamID);
                    table.ForeignKey(
                        name: "FK__SanPham__DanhMuc__71D1E811",
                        column: x => x.DanhMucID,
                        principalTable: "DanhMucSanPham",
                        principalColumn: "DanhMucID");
                    table.ForeignKey(
                        name: "FK__SanPham__NhaCung__72C60C4A",
                        column: x => x.NhaCungCapID,
                        principalTable: "NhaCungCap",
                        principalColumn: "NhaCungCapID");
                });

            migrationBuilder.CreateTable(
                name: "NhapHang",
                columns: table => new
                {
                    PhieuNhapID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NhaCungCapID = table.Column<int>(type: "int", nullable: true),
                    NgayNhap = table.Column<DateTime>(type: "datetime", nullable: false),
                    NhanVienID = table.Column<int>(type: "int", nullable: true),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__NhapHang__DE3A3882C632D946", x => x.PhieuNhapID);
                    table.ForeignKey(
                        name: "FK__NhapHang__NhaCun__6EF57B66",
                        column: x => x.NhaCungCapID,
                        principalTable: "NhaCungCap",
                        principalColumn: "NhaCungCapID");
                    table.ForeignKey(
                        name: "FK__NhapHang__NhanVi__6FE99F9F",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "NhanVienID");
                });

            migrationBuilder.CreateTable(
                name: "TaoTaiKhoan",
                columns: table => new
                {
                    TaiKhoanID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HoTen = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MatKhau = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LoaiTaiKhoan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    NhanVienID = table.Column<int>(type: "int", nullable: true),
                    KhachHangID = table.Column<int>(type: "int", nullable: true),
                    VaiTro = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TaoTaiKh__9A124B658310C38B", x => x.TaiKhoanID);
                    table.ForeignKey(
                        name: "FK__TaoTaiKho__Khach__73BA3083",
                        column: x => x.KhachHangID,
                        principalTable: "KhachHang",
                        principalColumn: "KhachHangID");
                    table.ForeignKey(
                        name: "FK__TaoTaiKho__NhanV__74AE54BC",
                        column: x => x.NhanVienID,
                        principalTable: "NhanVien",
                        principalColumn: "NhanVienID");
                });

            migrationBuilder.CreateTable(
                name: "DonHang",
                columns: table => new
                {
                    DonHangID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KhachHangID = table.Column<int>(type: "int", nullable: true),
                    NgayDatHang = table.Column<DateTime>(type: "datetime", nullable: false),
                    PhiVanChuyen = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    DiaChiID = table.Column<int>(type: "int", nullable: true),
                    TrangThaiID = table.Column<int>(type: "int", nullable: false),
                    DiaChiGiaoHang = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GhiChu = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhuongThucThanhToan = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SoDienThoai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TenKhachHang = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TrangThaiDonHangText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmailNguoiDat = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DonHang__D159F4DE93253B15", x => x.DonHangID);
                    table.ForeignKey(
                        name: "FK__DonHang__DiaChiI__6B24EA82",
                        column: x => x.DiaChiID,
                        principalTable: "DiaChi",
                        principalColumn: "DiaChiID");
                    table.ForeignKey(
                        name: "FK__DonHang__KhachHa__6C190EBB",
                        column: x => x.KhachHangID,
                        principalTable: "KhachHang",
                        principalColumn: "KhachHangID");
                    table.ForeignKey(
                        name: "FK__DonHang__TrangTh__6D0D32F4",
                        column: x => x.TrangThaiID,
                        principalTable: "TrangThaiDonHang",
                        principalColumn: "TrangThaiID");
                });

            migrationBuilder.CreateTable(
                name: "KhoHang",
                columns: table => new
                {
                    SanPhamID = table.Column<int>(type: "int", nullable: false),
                    SoLuongTon = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__KhoHang__05180FF4F3BF291A", x => x.SanPhamID);
                    table.ForeignKey(
                        name: "FK__KhoHang__SanPham__6E01572D",
                        column: x => x.SanPhamID,
                        principalTable: "SanPham",
                        principalColumn: "SanPhamID");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietNhapHang",
                columns: table => new
                {
                    ChiTietNhapHangID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhieuNhapID = table.Column<int>(type: "int", nullable: true),
                    SanPhamID = table.Column<int>(type: "int", nullable: true),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    DonGiaNhap = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    TongTien = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ChiTietN__D9FE8A90D5D8DB80", x => x.ChiTietNhapHangID);
                    table.ForeignKey(
                        name: "FK__ChiTietNh__Phieu__6754599E",
                        column: x => x.PhieuNhapID,
                        principalTable: "NhapHang",
                        principalColumn: "PhieuNhapID");
                    table.ForeignKey(
                        name: "FK__ChiTietNh__SanPh__68487DD7",
                        column: x => x.SanPhamID,
                        principalTable: "SanPham",
                        principalColumn: "SanPhamID");
                });

            migrationBuilder.CreateTable(
                name: "PasswordResetTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Password__3214EC07288ACA41", x => x.Id);
                    table.ForeignKey(
                        name: "FK__PasswordR__TaiKh__70DDC3D8",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaoTaiKhoan",
                        principalColumn: "TaiKhoanID");
                });

            migrationBuilder.CreateTable(
                name: "Wishlists",
                columns: table => new
                {
                    WishlistId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: true),
                    KhachHangId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Wishlist__233189EBE961249E", x => x.WishlistId);
                    table.ForeignKey(
                        name: "FK_Wishlists_KhachHang_KhachHangId",
                        column: x => x.KhachHangId,
                        principalTable: "KhachHang",
                        principalColumn: "KhachHangID");
                    table.ForeignKey(
                        name: "FK__Wishlists__TaiKhoan__778AC167",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaoTaiKhoan",
                        principalColumn: "TaiKhoanID");
                });

            migrationBuilder.CreateTable(
                name: "ChiTietDonHang",
                columns: table => new
                {
                    ChiTietID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DonHangID = table.Column<int>(type: "int", nullable: false),
                    SanPhamID = table.Column<int>(type: "int", nullable: false),
                    DonGia = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                    SoLuong = table.Column<int>(type: "int", nullable: false),
                    ThanhTien = table.Column<decimal>(type: "decimal(18,3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ChiTietD__B117E9EAF1299D72", x => x.ChiTietID);
                    table.ForeignKey(
                        name: "FK__ChiTietDo__DonHa__656C112C",
                        column: x => x.DonHangID,
                        principalTable: "DonHang",
                        principalColumn: "DonHangID");
                    table.ForeignKey(
                        name: "FK__ChiTietDo__SanPh__66603565",
                        column: x => x.SanPhamID,
                        principalTable: "SanPham",
                        principalColumn: "SanPhamID");
                });

            migrationBuilder.CreateTable(
                name: "WishlistItems",
                columns: table => new
                {
                    WishlistItemId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WishlistId = table.Column<int>(type: "int", nullable: false),
                    SanPhamId = table.Column<int>(type: "int", nullable: false),
                    TaiKhoanId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Wishlist__171E21A136D407A9", x => x.WishlistItemId);
                    table.ForeignKey(
                        name: "FK_WishlistItem_TaoTaiKhoan",
                        column: x => x.TaiKhoanId,
                        principalTable: "TaoTaiKhoan",
                        principalColumn: "TaiKhoanID");
                    table.ForeignKey(
                        name: "FK__WishlistI__SanPh__75A278F5",
                        column: x => x.SanPhamId,
                        principalTable: "SanPham",
                        principalColumn: "SanPhamID");
                    table.ForeignKey(
                        name: "FK__WishlistI__Wishl__76969D2E",
                        column: x => x.WishlistId,
                        principalTable: "Wishlists",
                        principalColumn: "WishlistId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonHang_DonHangID",
                table: "ChiTietDonHang",
                column: "DonHangID");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietDonHang_SanPhamID",
                table: "ChiTietDonHang",
                column: "SanPhamID");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietNhapHang_PhieuNhapID",
                table: "ChiTietNhapHang",
                column: "PhieuNhapID");

            migrationBuilder.CreateIndex(
                name: "IX_ChiTietNhapHang_SanPhamID",
                table: "ChiTietNhapHang",
                column: "SanPhamID");

            migrationBuilder.CreateIndex(
                name: "IX_DanhMucSanPham_DanhMucChaId",
                table: "DanhMucSanPham",
                column: "DanhMucChaId");

            migrationBuilder.CreateIndex(
                name: "IX_DiaChi_KhachHangID",
                table: "DiaChi",
                column: "KhachHangID");

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_DiaChiID",
                table: "DonHang",
                column: "DiaChiID");

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_KhachHangID",
                table: "DonHang",
                column: "KhachHangID");

            migrationBuilder.CreateIndex(
                name: "IX_DonHang_TrangThaiID",
                table: "DonHang",
                column: "TrangThaiID");

            migrationBuilder.CreateIndex(
                name: "IX_NhapHang_NhaCungCapID",
                table: "NhapHang",
                column: "NhaCungCapID");

            migrationBuilder.CreateIndex(
                name: "IX_NhapHang_NhanVienID",
                table: "NhapHang",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetTokens_TaiKhoanId",
                table: "PasswordResetTokens",
                column: "TaiKhoanId");

            migrationBuilder.CreateIndex(
                name: "IX_SanPham_DanhMucID",
                table: "SanPham",
                column: "DanhMucID");

            migrationBuilder.CreateIndex(
                name: "IX_SanPham_NhaCungCapID",
                table: "SanPham",
                column: "NhaCungCapID");

            migrationBuilder.CreateIndex(
                name: "IX_TaoTaiKhoan_KhachHangID",
                table: "TaoTaiKhoan",
                column: "KhachHangID");

            migrationBuilder.CreateIndex(
                name: "IX_TaoTaiKhoan_NhanVienID",
                table: "TaoTaiKhoan",
                column: "NhanVienID");

            migrationBuilder.CreateIndex(
                name: "UQ__TaoTaiKh__5C7E359EF074C186",
                table: "TaoTaiKhoan",
                column: "Phone",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UQ__TaoTaiKh__A9D1053489221C10",
                table: "TaoTaiKhoan",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TechNews_IsFeatured_PublishedAt",
                table: "TechNews",
                columns: new[] { "IsFeatured", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_TechNews_PublishedAt",
                table: "TechNews",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TechNews_Slug",
                table: "TechNews",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_SanPhamId",
                table: "WishlistItems",
                column: "SanPhamId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_TaiKhoanId",
                table: "WishlistItems",
                column: "TaiKhoanId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_WishlistId",
                table: "WishlistItems",
                column: "WishlistId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_KhachHangId",
                table: "Wishlists",
                column: "KhachHangId");

            migrationBuilder.CreateIndex(
                name: "IX_Wishlists_TaiKhoanId",
                table: "Wishlists",
                column: "TaiKhoanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatMessages");

            migrationBuilder.DropTable(
                name: "ChiTietDonHang");

            migrationBuilder.DropTable(
                name: "ChiTietNhapHang");

            migrationBuilder.DropTable(
                name: "DoanhThuTheoNam");

            migrationBuilder.DropTable(
                name: "DoanhThuTheoNgay");

            migrationBuilder.DropTable(
                name: "DoanhThuTheoThang");

            migrationBuilder.DropTable(
                name: "KhoHang");

            migrationBuilder.DropTable(
                name: "PasswordResetTokens");

            migrationBuilder.DropTable(
                name: "TechNews");

            migrationBuilder.DropTable(
                name: "WishlistItems");

            migrationBuilder.DropTable(
                name: "DonHang");

            migrationBuilder.DropTable(
                name: "NhapHang");

            migrationBuilder.DropTable(
                name: "SanPham");

            migrationBuilder.DropTable(
                name: "Wishlists");

            migrationBuilder.DropTable(
                name: "DiaChi");

            migrationBuilder.DropTable(
                name: "TrangThaiDonHang");

            migrationBuilder.DropTable(
                name: "DanhMucSanPham");

            migrationBuilder.DropTable(
                name: "NhaCungCap");

            migrationBuilder.DropTable(
                name: "TaoTaiKhoan");

            migrationBuilder.DropTable(
                name: "KhachHang");

            migrationBuilder.DropTable(
                name: "NhanVien");
        }
    }
}
