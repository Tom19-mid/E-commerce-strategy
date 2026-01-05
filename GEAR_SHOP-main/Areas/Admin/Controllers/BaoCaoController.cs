using GEAR_SHOP.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TL4_SHOP.Data;
using TL4_SHOP.Models.ViewModels;

namespace TL4_SHOP.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOrOrderManager")]
    public class BaoCaoController : Controller
    {
        private readonly _4tlShopContext _context;
        public BaoCaoController(_4tlShopContext context) => _context = context;

        public async Task<IActionResult> Index(string mode = "day", DateTime? from = null, DateTime? to = null, int? year = null)
        {
            var vm = new BaoCaoDoanhThuVM { Mode = mode, From = from, To = to, Year = year };

            // Đơn hàng đã hoàn tất (TrangThaiId = 4)
            var orders = _context.DonHangs.AsNoTracking()
                .Where(d => d.TrangThaiId == 4)
                .Select(d => new
                {
                    d.DonHangId,
                    Day = d.NgayDatHang.Date,
                    Month = d.NgayDatHang.Month,
                    Year = d.NgayDatHang.Year,
                    DoanhThu = d.TongTien
                });

            // ------------------- THEO NGÀY -------------------
            if (mode == "day")
            {
                var f = from?.Date ?? DateTime.Today.AddDays(-29);
                var t = (to?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);

                var series = await orders
                    .Where(x => x.Day >= f && x.Day <= t)
                    .GroupBy(x => x.Day)
                    .Select(g => new
                    {
                        Key = g.Key,
                        Don = g.Select(x => x.DonHangId).Distinct().Count(),
                        DoanhThu = g.Sum(x => x.DoanhThu)
                    })
                    .OrderBy(x => x.Key)
                    .ToListAsync();

                // Tổng số lượng bán
                var soLuongBan = await (from ct in _context.ChiTietDonHangs.AsNoTracking()
                                        join d in _context.DonHangs.AsNoTracking() on ct.DonHangId equals d.DonHangId
                                        where d.TrangThaiId == 4 && d.NgayDatHang >= f && d.NgayDatHang <= t
                                        select (int?)ct.SoLuong).SumAsync() ?? 0;

                // Tính chi phí vốn theo giá nhập bình quân
                var chiTietBans = await (from ct in _context.ChiTietDonHangs.AsNoTracking()
                                         join d in _context.DonHangs.AsNoTracking() on ct.DonHangId equals d.DonHangId
                                         where d.TrangThaiId == 4 && d.NgayDatHang >= f && d.NgayDatHang <= t
                                         select new { ct.SanPhamId, ct.SoLuong }).ToListAsync();

                decimal tongChiPhi = 0;
                foreach (var item in chiTietBans)
                {
                    var queryNhap = _context.ChiTietNhapHangs.Where(n => n.SanPhamId == item.SanPhamId);
                    var tongSL = await queryNhap.SumAsync(n => (int?)n.SoLuong) ?? 0;
                    if (tongSL > 0)
                    {
                        var tongTienNhap = await queryNhap.SumAsync(n => (decimal?)n.SoLuong * n.DonGiaNhap) ?? 0;
                        var giaNhapTb = tongTienNhap / tongSL;
                        tongChiPhi += giaNhapTb * item.SoLuong;
                    }
                }

                var sanPhamBan = await (
                    from ct in _context.ChiTietDonHangs.AsNoTracking()
                    join d in _context.DonHangs.AsNoTracking()
                        on ct.DonHangId equals d.DonHangId
                    join sp in _context.SanPhams.AsNoTracking()
                        on ct.SanPhamId equals sp.SanPhamId
                    where d.TrangThaiId == 4
                            && d.NgayDatHang >= f
                            && d.NgayDatHang <= t
                    group ct by new { ct.SanPhamId, sp.TenSanPham } into g
                    select new
                    {
                        g.Key.SanPhamId,
                        g.Key.TenSanPham,
                        SoLuongBan = g.Sum(x => x.SoLuong)
                    }
                ).ToListAsync();

                var chiPhiTheoSanPham = new List<ChiPhiSanPhamVM>();

                foreach (var sp in sanPhamBan)
                {
                    var nhap = _context.ChiTietNhapHangs
                        .Where(n => n.SanPhamId == sp.SanPhamId);

                    var tongSoLuongNhap = await nhap.SumAsync(n => (int?)n.SoLuong) ?? 0;
                    if (tongSoLuongNhap == 0) continue;

                    var tongTienNhap = await nhap
                        .SumAsync(n => (decimal?)n.SoLuong * n.DonGiaNhap) ?? 0;

                    var giaNhapTb = tongTienNhap / tongSoLuongNhap;

                    chiPhiTheoSanPham.Add(new ChiPhiSanPhamVM
                    {
                        SanPhamId = sp.SanPhamId,
                        TenSanPham = sp.TenSanPham,
                        SoLuongBan = sp.SoLuongBan,
                        GiaNhapTrungBinh = giaNhapTb
                    });
                }


                vm.Labels = series.Select(r => r.Key.ToString("dd/MM")).ToList();
                vm.Values = series.Select(r => r.DoanhThu).ToList();
                vm.TongSoDonHang = series.Sum(r => r.Don);
                vm.TongSoLuong = soLuongBan;
                vm.TongDoanhThu = series.Sum(r => r.DoanhThu);
                vm.ChiPhiTheoSanPham = chiPhiTheoSanPham;
                vm.TongLoiNhuan = vm.TongDoanhThu
                                  - chiPhiTheoSanPham.Sum(x => x.TongChiPhi);
                return View(vm);
            }

            // ------------------- THEO THÁNG -------------------
            if (mode == "month")
            {
                int y = year ?? DateTime.Today.Year;

                var series = await orders
                    .Where(x => x.Year == y)
                    .GroupBy(x => x.Month)
                    .Select(g => new
                    {
                        Key = g.Key,
                        Don = g.Select(x => x.DonHangId).Distinct().Count(),
                        DoanhThu = g.Sum(x => x.DoanhThu)
                    })
                    .OrderBy(x => x.Key)
                    .ToListAsync();

                var soLuongBan = await (from ct in _context.ChiTietDonHangs.AsNoTracking()
                                        join d in _context.DonHangs.AsNoTracking() on ct.DonHangId equals d.DonHangId
                                        where d.TrangThaiId == 4 && d.NgayDatHang.Year == y
                                        select (int?)ct.SoLuong).SumAsync() ?? 0;

                // Tính chi phí vốn
                var chiTietBans = await (from ct in _context.ChiTietDonHangs.AsNoTracking()
                                         join d in _context.DonHangs.AsNoTracking() on ct.DonHangId equals d.DonHangId
                                         where d.TrangThaiId == 4 && d.NgayDatHang.Year == y
                                         select new { ct.SanPhamId, ct.SoLuong }).ToListAsync();

                decimal tongChiPhi = 0;
                foreach (var item in chiTietBans)
                {
                    var queryNhap = _context.ChiTietNhapHangs.Where(n => n.SanPhamId == item.SanPhamId);
                    var tongSL = await queryNhap.SumAsync(n => (int?)n.SoLuong) ?? 0;
                    if (tongSL > 0)
                    {
                        var tongTienNhap = await queryNhap.SumAsync(n => (decimal?)n.SoLuong * n.DonGiaNhap) ?? 0;
                        var giaNhapTb = tongTienNhap / tongSL;
                        tongChiPhi += giaNhapTb * item.SoLuong;
                    }
                }

                vm.Labels = series.Select(r => $"Thg {r.Key:00}").ToList();
                vm.Values = series.Select(r => r.DoanhThu).ToList();
                vm.TongSoDonHang = series.Sum(r => r.Don);
                vm.TongSoLuong = soLuongBan;

                vm.TongDoanhThu = series.Sum(r => r.DoanhThu);
                vm.TongLoiNhuan = vm.TongDoanhThu - tongChiPhi;
                return View(vm);
            }

            // ------------------- THEO NĂM -------------------
            {
                var series = await orders
                    .GroupBy(x => x.Year)
                    .Select(g => new
                    {
                        Key = g.Key,
                        Don = g.Select(x => x.DonHangId).Distinct().Count(),
                        DoanhThu = g.Sum(x => x.DoanhThu)
                    })
                    .OrderBy(x => x.Key)
                    .ToListAsync();

                var soLuongBan = await (from ct in _context.ChiTietDonHangs.AsNoTracking()
                                        join d in _context.DonHangs.AsNoTracking() on ct.DonHangId equals d.DonHangId
                                        where d.TrangThaiId == 4
                                        select (int?)ct.SoLuong).SumAsync() ?? 0;

                // Tính chi phí vốn
                var chiTietBans = await (from ct in _context.ChiTietDonHangs.AsNoTracking()
                                         join d in _context.DonHangs.AsNoTracking() on ct.DonHangId equals d.DonHangId
                                         where d.TrangThaiId == 4
                                         select new { ct.SanPhamId, ct.SoLuong }).ToListAsync();

                decimal tongChiPhi = 0;
                foreach (var item in chiTietBans)
                {
                    var queryNhap = _context.ChiTietNhapHangs.Where(n => n.SanPhamId == item.SanPhamId);
                    var tongSL = await queryNhap.SumAsync(n => (int?)n.SoLuong) ?? 0;
                    if (tongSL > 0)
                    {
                        var tongTienNhap = await queryNhap.SumAsync(n => (decimal?)n.SoLuong * n.DonGiaNhap) ?? 0;
                        var giaNhapTb = tongTienNhap / tongSL;
                        tongChiPhi += giaNhapTb * item.SoLuong;
                    }
                }

                vm.Labels = series.Select(r => r.Key.ToString()).ToList();
                vm.Values = series.Select(r => r.DoanhThu).ToList();
                vm.TongSoDonHang = series.Sum(r => r.Don);
                vm.TongSoLuong = soLuongBan;
                vm.TongDoanhThu = series.Sum(r => r.DoanhThu);
                vm.TongLoiNhuan = vm.TongDoanhThu - tongChiPhi;
                return View(vm);
            }
        }

        [HttpGet]
        public async Task<IActionResult> DonHang_Details(
            string mode = "day",
            DateTime? from = null,
            DateTime? to = null,
            int? year = null)
        {
            var query = _context.DonHangs
                .AsNoTracking()
                .Where(d => d.TrangThaiId == 4);

            if (mode == "day")
            {
                var f = from?.Date ?? DateTime.Today.AddDays(-29);
                var t = (to?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);
                query = query.Where(d => d.NgayDatHang >= f && d.NgayDatHang <= t);
            }
            else if (mode == "month" && year.HasValue)
            {
                query = query.Where(d => d.NgayDatHang.Year == year);
            }

            var data = await query
                .Select(d => new DonHangItemVM
                {
                    DonHangId = d.DonHangId,
                    NgayDatHang = d.NgayDatHang,
                    TongTien = d.TongTien 
                })
                .OrderByDescending(d => d.NgayDatHang)
                .ToListAsync();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> SanPhamDaBan_Details(
            string mode = "day",
            DateTime? from = null,
            DateTime? to = null,
            int? year = null)
        {
            var donHangs = _context.DonHangs
                .AsNoTracking()
                .Where(d => d.TrangThaiId == 4);

            if (mode == "day")
            {
                var f = from?.Date ?? DateTime.Today.AddDays(-29);
                var t = (to?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);
                donHangs = donHangs.Where(d => d.NgayDatHang >= f && d.NgayDatHang <= t);
            }
            else if (mode == "month" && year.HasValue)
            {
                donHangs = donHangs.Where(d => d.NgayDatHang.Year == year);
            }

            var data = await (
                from ct in _context.ChiTietDonHangs.AsNoTracking()
                join d in donHangs on ct.DonHangId equals d.DonHangId
                join sp in _context.SanPhams.AsNoTracking()
                    on ct.SanPhamId equals sp.SanPhamId
                group ct by new { ct.SanPhamId, sp.TenSanPham } into g
                select new SanPhamBanVM
                {
                    SanPhamId = g.Key.SanPhamId,
                    TenSanPham = g.Key.TenSanPham,
                    TongSoLuong = g.Sum(x => x.SoLuong)
                }
            ).ToListAsync();

            return View(data);
        }

        [HttpGet]
        public async Task<IActionResult> ChiPhiTheoSanPham(
            string mode = "day",
            DateTime? from = null,
            DateTime? to = null,
            int? year = null)
        {
            // 1️⃣ Lọc đơn hàng hoàn tất
            var donHangs = _context.DonHangs
                .AsNoTracking()
                .Where(d => d.TrangThaiId == 4);

            if (mode == "day")
            {
                var f = from?.Date ?? DateTime.Today.AddDays(-29);
                var t = (to?.Date ?? DateTime.Today).AddDays(1).AddTicks(-1);
                donHangs = donHangs.Where(d => d.NgayDatHang >= f && d.NgayDatHang <= t);
            }
            else if (mode == "month" && year.HasValue)
            {
                donHangs = donHangs.Where(d => d.NgayDatHang.Year == year);
            }

            // 2️⃣ Số lượng bán theo sản phẩm
            var sanPhamBan = await (
                from ct in _context.ChiTietDonHangs.AsNoTracking()
                join d in donHangs on ct.DonHangId equals d.DonHangId
                join sp in _context.SanPhams.AsNoTracking()
                    on ct.SanPhamId equals sp.SanPhamId
                group ct by new { ct.SanPhamId, sp.TenSanPham } into g
                select new
                {
                    g.Key.SanPhamId,
                    g.Key.TenSanPham,
                    SoLuongBan = g.Sum(x => x.SoLuong)
                }
            ).ToListAsync();

            // 3️⃣ Tính chi phí nhập cho từng sản phẩm
            var result = new List<ChiPhiSanPhamVM>();

            foreach (var sp in sanPhamBan)
            {
                var nhap = _context.ChiTietNhapHangs
                    .Where(n => n.SanPhamId == sp.SanPhamId);

                var tongSLNhap = await nhap.SumAsync(n => (int?)n.SoLuong) ?? 0;
                if (tongSLNhap == 0) continue;

                var tongTienNhap = await nhap
                    .SumAsync(n => (decimal?)n.SoLuong * n.DonGiaNhap) ?? 0;

                var giaNhapTB = tongTienNhap / tongSLNhap;

                result.Add(new ChiPhiSanPhamVM
                {
                    SanPhamId = sp.SanPhamId,
                    TenSanPham = sp.TenSanPham,
                    SoLuongBan = sp.SoLuongBan,
                    GiaNhapTrungBinh = giaNhapTB
                });
            }

            return View(result);
        }


    }
}
