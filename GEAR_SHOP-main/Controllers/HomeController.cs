using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using TL4_SHOP.Data;
using TL4_SHOP.Extensions;
using TL4_SHOP.Models;
using TL4_SHOP.Models.ViewModels;
using TL4_SHOP.Extensions;
using DataNCC = TL4_SHOP.Data.NhaCungCap;
using ModelNCC = TL4_SHOP.Models.NhaCungCap;


namespace TL4_SHOP.Controllers
{
    public class HomeController : BaseController
    {
        public HomeController(_4tlShopContext context) : base(context)
        {
        }

        public async Task<IActionResult> Index()
        {
            // Nếu cần giữ message
            ViewBag.Message = TempData["Message"];

            var vm = new ShopViewModel();

            // 1. Lấy danh sách sản phẩm để hiển thị trên trang chủ 
            int[] ids = {38, 40, 47, 50, 53, 56};
            vm.SanPhams = await _context.SanPhams
                .Where(sp => ids.Contains(sp.SanPhamId))
                .ToListAsync();

            // 2. Lấy sản phẩm nổi bật vào model (thay vì ViewBag nếu muốn thống nhất)
            vm.PriceRanges = new List<PriceRangeViewModel>(); // (giữ nếu cần)
            vm.NhaCungCaps = await _context.NhaCungCaps.AsNoTracking().ToListAsync();
            vm.DanhMucs = await _context.DanhMucSanPhams.AsNoTracking().ToListAsync();

            // Nếu muốn có FeaturedProducts, thêm property vào ShopViewModel:
            // vm.FeaturedProducts = await _context.SanPhams.Where(s => s.LaNoiBat==true).Take(8).ToListAsync();
            // hoặc tiếp tục sử dụng ViewBag.FeaturedProducts như hiện tại:
            ViewBag.FeaturedProducts = await _context.SanPhams
                .Where(sp => sp.LaNoiBat == true)
                .OrderByDescending(sp => sp.SanPhamId)
                .Take(8)
                .ToListAsync();

            // Lấy 4 tin mới nhất (có thể gán vào vm.LatestNews nếu bạn đã thêm property)
            ViewBag.LatestNews = await _context.TechNews
                .AsNoTracking()
                .OrderByDescending(n => n.PublishedAt)
                .Take(4)
                .ToListAsync();

            return View(vm); // quan trọng: phải trả View(viewModel)
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // using Microsoft.EntityFrameworkCore; // đảm bảo có using này

        public async Task<IActionResult> Shop(string searchTerm, int? danhMucId, int? nhaCungCapId,
            decimal? minPrice, decimal? maxPrice, string sortBy, int page = 1)
        {
            var viewModel = new ShopViewModel
            {
                SearchTerm = searchTerm,
                DanhMucId = danhMucId,
                NhaCungCapId = nhaCungCapId,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortBy = sortBy,
                CurrentPage = page
            };

            // Build product query (chỉ 1 lần, dùng cho count & paging)
            var query = _context.SanPhams
                .Include(s => s.DanhMuc)
                .Include(s => s.NhaCungCap)
                .AsNoTracking()
                .AsQueryable(); // dùng var/AsQueryable để tránh reference type ambiguous

            if (!string.IsNullOrEmpty(searchTerm))
                query = query.Where(s => s.TenSanPham.Contains(searchTerm));

            if (danhMucId.HasValue)
                query = query.Where(s => s.DanhMucId == danhMucId.Value);

            if (nhaCungCapId.HasValue)
                query = query.Where(s => s.NhaCungCapId == nhaCungCapId.Value);

            if (minPrice.HasValue)
                query = query.Where(s => s.Gia >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(s => s.Gia <= maxPrice.Value);

            // Sắp xếp
            var orderedQuery = query; // giữ chung kiểu IQueryable từ DbSet
            switch (sortBy)
            {
                case "price_asc":
                    orderedQuery = query.OrderBy(p => p.Gia);
                    break;
                case "price_desc":
                    orderedQuery = query.OrderByDescending(p => p.Gia);
                    break;
                case "newest":
                    orderedQuery = query.OrderByDescending(p => p.SanPhamId);
                    break;
                case "discount":
                    orderedQuery = query.OrderByDescending(p => (p.GiaSauGiam != null && p.GiaSauGiam < p.Gia) ? 1 : 0)
                                        .ThenByDescending(p => p.Gia - (p.GiaSauGiam ?? p.Gia));
                    break;
                default:
                    orderedQuery = query.OrderByDescending(p => p.SanPhamId);
                    break;
            }

            // Tổng số item (sau áp filter)
            viewModel.TotalItems = await orderedQuery.CountAsync();

            // Phân trang
            var pageSize = viewModel.PageSize;
            viewModel.SanPhams = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // using Microsoft.EntityFrameworkCore; // ensure present

            // --- Lấy số lượng sản phẩm theo NhaCungCapId (1 query GROUP BY) ---
            var counts = await _context.SanPhams
                .GroupBy(p => p.NhaCungCapId)
                .Select(g => new { NhaCungCapId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.NhaCungCapId, x => x.Count);

            // --- Lấy danh sách brands (entities) ---
            var brands = await _context.NhaCungCaps
                .AsNoTracking()
                .ToListAsync();

            // --- Map sang ViewModel (client-side, không có ProductCount trong DB) ---
            var brandVms = brands.Select(b => new DataNCC
            {
                NhaCungCapId = b.NhaCungCapId,
                TenNhaCungCap = b.TenNhaCungCap,
                ProductCount = counts.ContainsKey(b.NhaCungCapId) ? counts[b.NhaCungCapId] : 0
            }).ToList();

            viewModel.NhaCungCaps = brandVms;

            // Tính total pages
            viewModel.TotalPages = (int)Math.Ceiling((double)viewModel.TotalItems / pageSize);

            return View(viewModel);
        }


        public async Task<IActionResult> SearchSuggestions(string term)
        {
            if (string.IsNullOrEmpty(term) || term.Length < 2)
                return Json(new List<object>());

            var suggestions = await _context.SanPhams
                .Where(s => s.TenSanPham.Contains(term))
                .Select(s => new
                {
                    s.SanPhamId,
                    TenSanPham = s.TenSanPham,
                    Gia = s.Gia,
                    HinhAnh = s.HinhAnh,
                    // [THÊM] URL ảnh tuyệt đối cho client
                    imageUrl = s.HinhAnh.StartsWith("http")
                        ? s.HinhAnh
                        : (s.HinhAnh.StartsWith("/") ? s.HinhAnh
                            : "/" + s.HinhAnh.Replace("~/", ""))
                })
                .ToListAsync();

            return Json(suggestions);
        }

        public IActionResult ShopDetail() => View();
        public IActionResult ShoppingCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>("GioHang") ?? new List<CartItem>();
            return View("~/Views/Cart/ShoppingCart.cshtml", cart); // dùng đường dẫn tuyệt đối
        }
        public IActionResult Checkout() => View();
        public IActionResult Contact() => View();

        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term)
        {
            if (string.IsNullOrEmpty(term))
                return Json(new List<object>());

            var products = await _context.SanPhams
                .Where(s => s.TenSanPham.Contains(term))
                .Take(10)
                .Select(s => new
                {
                    id = s.SanPhamId,
                    name = s.TenSanPham,
                    price = s.Gia,
                    image = s.HinhAnh
                })
                .ToListAsync();

            return Json(products);
        }
    }
}
