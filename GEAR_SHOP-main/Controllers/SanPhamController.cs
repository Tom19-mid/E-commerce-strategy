using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TL4_SHOP.Data;

namespace TL4_SHOP.Controllers
{
    public class SanPhamController : BaseController
    {
        private readonly _4tlShopContext _context;

        public SanPhamController(_4tlShopContext context) : base(context)
        {
            _context = context;
        }

        // File: Controllers/SanPhamController.cs
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var sanPham = await _context.SanPhams
                .Include(sp => sp.DanhMuc)
                .Include(sp => sp.NhaCungCap)
                .FirstOrDefaultAsync(sp => sp.SanPhamId == id);

            if (sanPham == null)
            {
                return NotFound();
            }

            // (Tùy chọn) populate meta nếu muốn cho id-based link
            PopulateMetaForProduct(sanPham);

            return View("~/Views/SanPhams/Details.cshtml", sanPham);
        }

          // MỚI: action lấy theo slug -> route: /san-pham/{slug}
        [HttpGet]
        public async Task<IActionResult> DetailsBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug))
                return NotFound();

            var sanPham = await _context.SanPhams
                .Include(sp => sp.DanhMuc)
                .Include(sp => sp.NhaCungCap)
                .FirstOrDefaultAsync(sp => sp.Slug == slug); // đảm bảo bạn có cột Slug trong DB

            if (sanPham == null)
            {
                return NotFound();
            }

            // Thiết lập meta/og cho layout/view
            PopulateMetaForProduct(sanPham);

            // Render cùng view Details.cshtml (giữ nguyên)
            return View("~/Views/SanPhams/Details.cshtml", sanPham);
        }

        // Helper: set các ViewData để _Layout.cshtml dùng cho meta + og
        private void PopulateMetaForProduct(dynamic sanPham)
        {
            // Thay tên các property nếu entity bạn khác (TenSanPham, MoTaNgan, HinhAnh,...)
            // Mình dùng dynamic để tránh phải biết chính xác kiểu entity; bạn có thể đổi sang kiểu cụ thể.
            string title = sanPham.TenSanPham ?? sanPham.TenSanPham ?? sanPham.Title ?? "Sản phẩm";
            string shortDesc = sanPham.MoTa ?? sanPham.MoTaNgan ?? sanPham.ShortDescription ?? "";
            string slug = sanPham.Slug ?? "";
            string imageFile = sanPham.HinhAnh ?? sanPham.ImageFileName ?? sanPham.ImageUrl ?? "";

            // canonical (absolute)
            var request = HttpContext.Request;
            string canonical = $"{request.Scheme}://{request.Host}/san-pham/{slug}";

            // absolute image url
            string ogImage;
            if (string.IsNullOrWhiteSpace(imageFile))
            {
                ogImage = $"{request.Scheme}://{request.Host}/images/default-share.jpg";
            }
            else
            {
                // nếu imageFile đã là absolute thì giữ nguyên, else build từ wwwroot/images/...
                if (imageFile.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    ogImage = imageFile;
                else
                    ogImage = $"{request.Scheme}://{request.Host}{(imageFile.StartsWith("/") ? "" : "/")}{imageFile}";
            }

            ViewData["Title"] = $"{title} - YourShop";
            ViewData["Description"] = shortDesc;
            ViewData["Canonical"] = canonical;
            ViewData["OgType"] = "product";
            ViewData["OgTitle"] = title;
            ViewData["OgDescription"] = shortDesc;
            ViewData["OgImage"] = ogImage;
            ViewData["OgUrl"] = canonical;

            // Optional: JSON-LD (nếu muốn) - bạn có thể build schema tương tự
            var jsonLd = new
            {
                @context = "https://schema.org",
                @type = "Product",
                name = title,
                image = new[] { ogImage },
                description = shortDesc,
                sku = sanPham.Sku ?? "",
                offers = new
                {
                    @type = "Offer",
                    priceCurrency = "VND",
                    price = sanPham.Gia ?? 0,
                    availability = "https://schema.org/InStock"
                }
            };
            ViewData["JsonLd"] = System.Text.Json.JsonSerializer.Serialize(jsonLd);
        }

        public static string ToUrlSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            // chuyển về thường, loại bỏ dấu, thay khoảng trắng bằng '-'
            var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var ch in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(ch);
            }
            var cleaned = Regex.Replace(sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant(), @"[^a-z0-9\s-]", "");
            cleaned = Regex.Replace(cleaned, @"\s+", "-").Trim('-');
            cleaned = Regex.Replace(cleaned, @"-+", "-");
            return cleaned;
        }

    }
}
