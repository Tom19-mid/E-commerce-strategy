using TL4_SHOP.Models.ViewModels;

namespace GEAR_SHOP.Models.ViewModels
{
    public class BaoCaoDoanhThuVM
    {
        public string Mode { get; set; } = "day";  // day | month | year
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int? Year { get; set; }

        public List<string> Labels { get; set; } = new();
        public List<decimal> Values { get; set; } = new();

        public int TongSoDonHang { get; set; }
        public int TongSoLuong { get; set; }
        public List<DonHangItemVM> DonHangs { get; set; } = new();
        public List<SanPhamBanVM> SanPhamDaBan { get; set; } = new();
        public decimal TongDoanhThu { get; set; }
        public decimal TongLoiNhuan { get; set; }
        public List<ChiPhiSanPhamVM> ChiPhiTheoSanPham { get; set; } = new();

    }
}
