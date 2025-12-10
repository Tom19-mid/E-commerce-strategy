using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace TL4_SHOP.Data
{
    public class SlugHistory
    {
        public int Id { get; set; }
        public int SanPhamId { get; set; }
        public string OldSlug { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(SanPhamId))]
        public SanPham? SanPham { get; set; }
    }
}
