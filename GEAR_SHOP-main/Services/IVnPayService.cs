using TL4_SHOP.Models.ViewModels;

namespace TL4_SHOP.Services
{
    public interface IVnPayService
    {
        // Tạo URL thanh toán
        string CreatePaymentUrl(int orderId, decimal amount, HttpContext context, string orderInfo);

        // Xử lý kết quả trả về từ VNPay
        VnPayResponseModel PaymentExecute(IQueryCollection collections);
    }
}
