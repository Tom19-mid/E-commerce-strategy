using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using TL4_SHOP.Data;

namespace TL4_SHOP.Helpers_ProductHelpers
{
    public class ProductHelpers
    {
        // bạn đã có ToUrlSlug - dùng lại
        public static string ToUrlSlug(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            string normalized = value.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            var cleaned = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
            cleaned = Regex.Replace(cleaned, @"[^a-z0-9\s-]", "");
            cleaned = Regex.Replace(cleaned, @"\s+", "-").Trim('-');
            cleaned = Regex.Replace(cleaned, @"-+", "-");
            return cleaned;
        }

        public static string GenerateSkuById(int id)
        {
            return "SP" + id.ToString().PadLeft(5, '0');
        }

        public static async Task<string> EnsureUniqueSlugAsync(_4tlShopContext context, string baseSlug, int currentProductId)
        {
            if (string.IsNullOrWhiteSpace(baseSlug)) return baseSlug ?? "";

            var slug = baseSlug;
            int suffix = 1;

            while (await context.SanPhams.AnyAsync(s => s.Slug == slug && s.SanPhamId != currentProductId))
            {
                slug = $"{baseSlug}-{suffix}";
                suffix++;
            }

            return slug;
        }
    }
}


