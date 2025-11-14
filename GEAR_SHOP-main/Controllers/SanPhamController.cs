using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TL4_SHOP.Data;
using System.Text.Json;

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
            if (!string.IsNullOrWhiteSpace(sanPham.Slug))
            {
                var canonical = Url.Action("DetailsBySlug", "SanPham", new { slug = sanPham.Slug }, Request.Scheme);
                return RedirectPermanent(canonical);
            }

            // (Tùy chọn) populate meta nếu muốn cho id-based link
            PopulateMetaForProduct(sanPham);

            return View("~/Views/SanPhams/Details.cshtml", sanPham);
        }

        // route: /san-pham/{slug}
        [HttpGet]
        public async Task<IActionResult> DetailsBySlug(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return NotFound();

            // 1) tìm sản phẩm theo slug hiện tại
            var product = await _context.SanPhams
                .Include(p => p.DanhMuc)
                .Include(p => p.NhaCungCap)
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (product != null)
            {
                // render same view as Details(id)
                PopulateMetaForProduct(product);
                return View("~/Views/SanPhams/Details.cshtml", product);
            }

            // 2) nếu không tìm thấy -> kiểm tra lịch sử slug và redirect tới slug mới
            var hist = await _context.SlugHistories
                .Include(h => h.SanPham)
                .FirstOrDefaultAsync(h => h.OldSlug == slug);

            if (hist != null && hist.SanPham != null)
            {
                var newUrl = Url.Action("DetailsBySlug", "SanPham", new { slug = hist.SanPham.Slug}, protocol: Request.Scheme);
                // RedirectPermanent dùng URL tuyệt đối
                return RedirectPermanent(newUrl);
            }

            return NotFound();
        }


        // PRIVATE helper: đặt meta cho layout (og, twitter, canonical, json-ld)
        private void PopulateMetaForProduct(TL4_SHOP.Data.SanPham product)
        {
            // Host absolute
            var host = $"{Request.Scheme}://{Request.Host}";

            // 1) Ảnh: dùng ảnh product nếu có, fallback về default
            // => đảm bảo ảnh lưu ở wwwroot/images/sanpham/{HinhAnh}
            string imageRelative = string.IsNullOrEmpty(product.HinhAnh)
                ? "/images/default-share.jpg"
                : $"/images/sanpham/{product.HinhAnh}";
            string imageAbsolute = host + Url.Content(imageRelative); // absolute image url

            // 2) canonical / og:url -> dùng Url.Action với protocol để có absolute url
            string productUrl = Url.Action(
                action: "DetailsBySlug",
                controller: "SanPham",                       // chỉnh nếu controller tên khác
                values: new { slug = product.Slug },
                protocol: Request.Scheme
            );

            // 3) title & description (ngắn gọn)
            string title = string.IsNullOrWhiteSpace(product.TenSanPham) ? "YourShop" : product.TenSanPham;
            string description = product.MoTa ?? "";
            description = HtmlToPlainText(description); // loại bỏ tag nếu cần
            if (description.Length > 200) description = description.Substring(0, 197) + "...";

            // 4) gán vào ViewData keys mà _Layout.cshtml đang đọc
            ViewData["Title"] = title;
            ViewData["Description"] = description;
            ViewData["Canonical"] = productUrl;

            ViewData["OgTitle"] = title;
            ViewData["OgDescription"] = description;
            ViewData["OgImage"] = imageAbsolute;
            ViewData["OgUrl"] = productUrl;
            ViewData["OgType"] = "product";

            // 5) JSON-LD (schema.org) tùy chọn
            var jsonLd = new
            {
                @context = "https://schema.org/",
                @type = "Product",
                name = title,
                image = new[] { imageAbsolute },
                description = description,
                url = productUrl,
                brand = product.NhaCungCap?.TenNhaCungCap ?? "",
                category = product.DanhMuc?.TenDanhMuc ?? ""
                // bạn có thể mở rộng với price, currency, sku nếu có
            };
            ViewData["JsonLd"] = JsonSerializer.Serialize(jsonLd);
        }

        // helper đơn giản: loại bỏ HTML tags (nếu MoTa chứa HTML). Nếu content của bạn đã plain text, có thể bỏ hàm này.
        private string HtmlToPlainText(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            // rất đơn giản: remove tags; nếu cần robust hơn dùng HtmlAgilityPack
            var withoutTags = System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", " ");
            // decode HTML entities
            return System.Net.WebUtility.HtmlDecode(withoutTags).Trim();
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
