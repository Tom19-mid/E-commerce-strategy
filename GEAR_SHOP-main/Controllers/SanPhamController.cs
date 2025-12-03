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
       
        private void PopulateMetaForProduct(TL4_SHOP.Data.SanPham product)
        {
            // Host absolute (scheme + host)
            var host = $"{Request.Scheme}://{Request.Host}";

            // 1) Ảnh: chuẩn hoá nhiều trường hợp input từ DB
            string imageAbsolute;
            if (string.IsNullOrWhiteSpace(product.HinhAnh))
            {
                // không có ảnh -> dùng default
                imageAbsolute = host + Url.Content("/images/default-share.jpg");
            }
            else if (product.HinhAnh.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                     product.HinhAnh.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                // đã là URL tuyệt đối
                imageAbsolute = product.HinhAnh;
            }
            else
            {
                // các trường hợp còn lại: product.HinhAnh có thể là:
                // "images/products/xxx.jpg", "/images/products/xxx.jpg", hoặc "file.jpg"
                string relative;
                if (product.HinhAnh.StartsWith("/"))
                {
                    relative = product.HinhAnh; // đã có leading slash
                }
                else if (product.HinhAnh.Contains("/"))
                {
                    // đã là đường dẫn nhưng thiếu leading slash -> thêm '/'
                    relative = "/" + product.HinhAnh;
                }
                else
                {
                    // chỉ tên file -> dùng folder mặc định (bạn có thể đổi sang images/products nếu phù hợp)
                    relative = $"/images/sanpham/{product.HinhAnh}";
                }

                imageAbsolute = host + Url.Content(relative);
            }

            // 2) canonical / og:url -> dùng Url.Action với protocol để có absolute url
            // Nếu slug rỗng thì fallback về Details bằng id để tránh null canonical
            string productUrl;
            if (!string.IsNullOrWhiteSpace(product.Slug))
            {
                productUrl = Url.Action(
                    action: "DetailsBySlug",
                    controller: "SanPham",
                    values: new { slug = product.Slug },
                    protocol: Request.Scheme
                );
            }
            else
            {
                productUrl = Url.Action(
                    action: "Details",
                    controller: "SanPham",
                    values: new { id = product.SanPhamId },
                    protocol: Request.Scheme
                );
            }

            // 3) title & description (ngắn gọn)
            string title = string.IsNullOrWhiteSpace(product.TenSanPham) ? "YourShop" : product.TenSanPham;
            string description = product.MoTa ?? "";
            description = HtmlToPlainText(description);
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

            // 5) JSON-LD (schema.org) — BẢN CHUẨN
            var offers = new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["url"] = productUrl,
                ["priceCurrency"] = "VND",
                ["price"] = product.Gia,
                ["availability"] = product.SoLuongTon > 0
                    ? "https://schema.org/InStock"
                    : "https://schema.org/OutOfStock"
            };

            var jsonLd = new Dictionary<string, object?>
            {
                ["@context"] = "https://schema.org/",
                ["@type"] = "Product",
                ["name"] = title,
                ["image"] = new[] { imageAbsolute },
                ["description"] = description,
                ["sku"] = product.SanPhamId.ToString(),
                ["brand"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Brand",
                    ["name"] = product.NhaCungCap?.TenNhaCungCap ?? ""
                },
                ["category"] = product.DanhMuc?.TenDanhMuc ?? "",
                ["url"] = productUrl,
                ["offers"] = offers
            };

            ViewData["JsonLd"] = JsonSerializer.Serialize(jsonLd);


            // Optional debug info (bỏ/comment khi không cần)
            ViewData["Debug_ImageAbsolute"] = imageAbsolute;
            ViewData["Debug_HinhAnh"] = product.HinhAnh;
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
