using PayPalCheckoutSdk.Orders;
using System.Threading.Tasks;

namespace TL4_SHOP.Services
{
    public interface IPayPalService
    {
        Task<PayPalHttp.HttpResponse> CreateOrderAsync(decimal amount, string currency, int orderId, string returnUrl, string cancelUrl);
        Task<PayPalHttp.HttpResponse> CaptureOrderAsync(string token);
    }
}