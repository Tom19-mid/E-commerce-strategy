namespace TL4_SHOP.Models.ViewModels
{
    public class ChiPhiSanPhamVM
    {
        public int SanPhamId { get; set; }
        public string TenSanPham { get; set; } = string.Empty;

        public int SoLuongBan { get; set; }
        public decimal GiaNhapTrungBinh { get; set; }

        public decimal TongChiPhi => SoLuongBan * GiaNhapTrungBinh;
    }
}
