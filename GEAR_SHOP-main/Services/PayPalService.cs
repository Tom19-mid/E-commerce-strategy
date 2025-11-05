using Microsoft.Extensions.Configuration;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TL4_SHOP.Services
{
    public class PayPalService : IPayPalService
    {
        private readonly PayPalHttpClient _client;

        public PayPalService(IConfiguration config)
        {
            _client = PayPalClient.Client(config);
        }

        // Gọi API PayPal để tạo đơn hàng
        public async Task<PayPalHttp.HttpResponse> CreateOrderAsync(decimal amount, string currency, int orderId, string returnUrl, string cancelUrl)
        {
            var request = new OrdersCreateRequest();
            request.Prefer("return=representation");
            request.RequestBody(BuildRequestBody(amount, currency, orderId, returnUrl, cancelUrl));

            Console.WriteLine("🚀 [PayPal] Đang tạo đơn hàng...");
            var response = await _client.Execute(request);
            Console.WriteLine($"✅ [PayPal] Tạo đơn hàng thành công, Status: {response.StatusCode}");

            return response;
        }

        // Gọi API PayPal để "bắt" (capture) thanh toán sau khi user đồng ý
        public async Task<PayPalHttp.HttpResponse> CaptureOrderAsync(string token)
        {
            var request = new OrdersCaptureRequest(token);
            request.RequestBody(new OrderActionRequest());

            Console.WriteLine($"🚀 [PayPal] Đang capture đơn hàng với Token: {token}...");
            var response = await _client.Execute(request);
            Console.WriteLine($"✅ [PayPal] Capture thành công, Status: {response.StatusCode}");

            return response;
        }

        // Xây dựng nội dung gửi lên PayPal
        private OrderRequest BuildRequestBody(decimal amount, string currency, int orderId, string returnUrl, string cancelUrl)
        {
            var request = new OrderRequest()
            {
                CheckoutPaymentIntent = "CAPTURE",
                ApplicationContext = new ApplicationContext()
                {
                    ReturnUrl = returnUrl,
                    CancelUrl = cancelUrl,
                    BrandName = "4TL Shop",
                    ShippingPreference = "NO_SHIPPING" // Giả sử không cần địa chỉ từ PayPal
                },
                PurchaseUnits = new List<PurchaseUnitRequest>()
                {
                    new PurchaseUnitRequest()
                    {
                        // CustomId hoặc InvoiceId để lưu mã đơn hàng của hệ thống
                        CustomId = $"DH_{orderId}",
                        Description = $"Thanh toan cho don hang DH_{orderId}",
                        AmountWithBreakdown = new AmountWithBreakdown()
                        {
                            CurrencyCode = currency,
                            Value = amount.ToString("F2") // PayPal yêu cầu 2 số thập phân
                        }
                    }
                }
            };
            return request;
        }
    }
}