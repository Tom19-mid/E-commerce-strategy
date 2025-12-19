using Microsoft.Extensions.Configuration;
using TL4_SHOP.Models.ViewModels;
using TL4_SHOP.Data.VnPay;
using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Linq;

namespace TL4_SHOP.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _baseUrl;

        public VnPayService(IConfiguration config)
        {
            _config = config;

            // Đọc cấu hình từ appsettings.json
            _tmnCode = _config["Vnpay:TmnCode"];
            _hashSecret = _config["Vnpay:HashSecret"];
            _baseUrl = _config["Vnpay:BaseUrl"];

            // ❌ KHÔNG ĐỌC ReturnUrl từ config nữa
            // _returnUrl = _config["Vnpay:ReturnUrl"];
        }

        public string CreatePaymentUrl(int orderId, decimal amount, HttpContext context, string orderInfo)
        {
            var tick = DateTime.Now.Ticks.ToString();

            // ✅ TẠO RETURNURL ĐỘNG DỰA TRÊN REQUEST HIỆN TẠI
            string returnUrl = GetDynamicReturnUrl(context);

            Console.WriteLine("=== VNPAY PAYMENT URL CREATION ===");
            Console.WriteLine($"🔗 Dynamic ReturnUrl: {returnUrl}");
            Console.WriteLine($"🆔 OrderId: {orderId}");
            Console.WriteLine($"💰 Amount: {amount}");

            // Khởi tạo VnPayRequest với ReturnUrl động
            var pay = new VnPayRequest(_tmnCode, _hashSecret, _baseUrl, returnUrl);

            // 1. CHUYỂN ĐỔI SỐ TIỀN sang long
            long amountLong = (long)(amount * 100);

            // 2. LẤY IP
            string ipAddress = GetIpAddress(context);

            pay.AddRequestData("vnp_Version", "2.1.0");
            pay.AddRequestData("vnp_Command", "pay");
            pay.AddRequestData("vnp_Amount", amountLong.ToString());
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_IpAddr", ipAddress);
            pay.AddRequestData("vnp_Locale", "vn");
            string cleanOrderInfo = RemoveSign4VietnameseString(orderInfo);
            pay.AddRequestData("vnp_OrderInfo", cleanOrderInfo);
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_TxnRef", orderId.ToString() + "_" + tick);
            pay.AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            string paymentUrl = pay.GetVnPayUrl();

            Console.WriteLine($"🌐 Full VNPay URL: {paymentUrl}");
            Console.WriteLine("===================================");

            return paymentUrl;
        }
        // --- THÊM HÀM TIỆN ÍCH NÀY VÀO CUỐI CLASS ---
        private static string RemoveSign4VietnameseString(string str)
        {
            if (string.IsNullOrEmpty(str)) return str;

            string[] VietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            for (int i = 1; i < VietnameseSigns.Length; i++)
            {
                for (int j = 0; j < VietnameseSigns[i].Length; j++)
                    str = str.Replace(VietnameseSigns[i][j], VietnameseSigns[0][i - 1]);
            }
            return str;
        }

        /// <summary>
        /// Tạo ReturnUrl động dựa trên request hiện tại
        /// Hoạt động cho cả localhost và Azure
        /// </summary>
        private string GetDynamicReturnUrl(HttpContext context)
        {
            var request = context.Request;

            // Lấy scheme (http hoặc https)
            string scheme = request.Scheme;

            // Lấy host (bao gồm port nếu có)
            // Ví dụ: localhost:7095 hoặc tl4shop-demo.azurewebsites.net
            string host = request.Host.Value;

            // Tạo ReturnUrl đầy đủ
            string returnUrl = $"{scheme}://{host}/Payment/Result";

            return returnUrl;
        }

        private string GetIpAddress(HttpContext context)
        {
            // Cố gắng lấy IP Remote (IP thực của client)
            var remoteIpAddress = context.Connection.RemoteIpAddress;
            if (remoteIpAddress == null)
            {
                return "127.0.0.1";
            }

            if (remoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // Nếu là IPv6, tìm IPv4 hoặc trả về IP Loopback IPv4
                var ipv4Address = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                return ipv4Address != null ? ipv4Address.ToString() : "127.0.0.1";
            }

            return remoteIpAddress.ToString();
        }

        public VnPayResponseModel PaymentExecute(IQueryCollection collections)
        {
            Console.WriteLine("=== VnPayService.PaymentExecute START ===");

            // In ra tất cả params nhận được
            Console.WriteLine("Received parameters:");
            foreach (var key in collections.Keys)
            {
                Console.WriteLine($"  {key} = {collections[key]}");
            }

            var pay = new VnPayResponse(_hashSecret, collections);

            // Kiểm tra chữ ký
            bool isValid = pay.IsRequestValid();
            Console.WriteLine($"Signature Valid: {isValid}");

            if (!isValid)
            {
                Console.WriteLine("❌ Invalid signature!");
                return new VnPayResponseModel { Success = false, Message = "Chữ ký không hợp lệ" };
            }

            var vnpResponseCode = pay.GetResponseData("vnp_ResponseCode");
            var vnpTransactionStatus = pay.GetResponseData("vnp_TransactionStatus");
            var vnpTxnRef = pay.GetResponseData("vnp_TxnRef");
            var vnpOrderInfo = pay.GetResponseData("vnp_OrderInfo");
            var vnpAmount = pay.GetResponseData("vnp_Amount");
            var vnpTransactionNo = pay.GetResponseData("vnp_TransactionNo");

            Console.WriteLine($"ResponseCode: {vnpResponseCode}");
            Console.WriteLine($"TransactionStatus: {vnpTransactionStatus}");
            Console.WriteLine($"TxnRef: {vnpTxnRef}");

            // Xử lý logic nghiệp vụ
            int orderId = 0;
            if (vnpTxnRef != null && vnpTxnRef.Contains("_"))
            {
                int.TryParse(vnpTxnRef.Split('_')[0], out orderId);
            }

            Console.WriteLine($"Extracted OrderId: {orderId}");

            decimal amount = 0;
            decimal.TryParse(vnpAmount, out amount);

            if (vnpResponseCode == "00" && vnpTransactionStatus == "00")
            {
                Console.WriteLine("✅ Payment SUCCESS");
                return new VnPayResponseModel
                {
                    Success = true,
                    Message = "Thanh toán thành công!",
                    OrderId = orderId,
                    Amount = amount / 100,
                    TransactionId = vnpTransactionNo,
                    OrderInfo = vnpOrderInfo
                };
            }
            else
            {
                Console.WriteLine($"❌ Payment FAILED - Code: {vnpResponseCode}, Status: {vnpTransactionStatus}");
                return new VnPayResponseModel
                {
                    Success = false,
                    Message = $"Thanh toán thất bại: Mã lỗi {vnpResponseCode} - Trạng thái: {vnpTransactionStatus}",
                    OrderId = orderId,
                    Amount = amount / 100,
                    TransactionId = vnpTransactionNo,
                    OrderInfo = vnpOrderInfo
                };
            }
        }
    }
}