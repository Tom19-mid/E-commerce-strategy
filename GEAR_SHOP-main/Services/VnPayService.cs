using Microsoft.Extensions.Configuration;
using TL4_SHOP.Models.ViewModels;
using TL4_SHOP.Data.VnPay;
using Microsoft.AspNetCore.Http;
using System;
using System.Net; // Thêm thư viện này
using System.Linq; // Thêm thư viện này

namespace TL4_SHOP.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _baseUrl;
        private readonly string _returnUrl;

        public VnPayService(IConfiguration config)
        {
            _config = config;
            // Đọc cấu hình từ appsettings.json
            _tmnCode = _config["Vnpay:TmnCode"];
            _hashSecret = _config["Vnpay:HashSecret"];
            _baseUrl = _config["Vnpay:BaseUrl"];
            _returnUrl = _config["Vnpay:ReturnUrl"];
        }

        public string CreatePaymentUrl(int orderId, decimal amount, HttpContext context, string orderInfo)
        {
            // Sử dụng các class Helper đã tạo trong Data/VnPay
            var tick = DateTime.Now.Ticks.ToString();

            // Khởi tạo VnPayRequest - tmnCode và ReturnUrl đã được thêm ở đây!
            var pay = new VnPayRequest(_tmnCode, _hashSecret, _baseUrl, _returnUrl);

            // 1. CHUYỂN ĐỔI SỐ TIỀN sang long (Integer)
            // Đảm bảo số tiền không bị mất độ chính xác khi nhân 100 (đơn vị VNĐ nhỏ nhất)
            long amountLong = (long)(amount * 100);

            // 2. LẤY IP (Fix lỗi IPv6 hoặc null)
            string ipAddress = GetIpAddress(context);


            pay.AddRequestData("vnp_Version", "2.1.0");
            pay.AddRequestData("vnp_Command", "pay");

            pay.AddRequestData("vnp_Amount", amountLong.ToString()); // <<< FIX: Số tiền đã ép kiểu Long
            pay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
            pay.AddRequestData("vnp_CurrCode", "VND");
            pay.AddRequestData("vnp_IpAddr", ipAddress); // <<< FIX: Sử dụng hàm GetIpAddress
            pay.AddRequestData("vnp_Locale", "vn");
            pay.AddRequestData("vnp_OrderInfo", orderInfo);
            pay.AddRequestData("vnp_OrderType", "other");
            pay.AddRequestData("vnp_TxnRef", orderId.ToString() + "_" + tick);
            pay.AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

            string paymentUrl = pay.GetVnPayUrl();
            return paymentUrl;
        }

        private string GetIpAddress(HttpContext context)
        {
            // Cố gắng lấy IP Remote (IP thực của client)
            var remoteIpAddress = context.Connection.RemoteIpAddress;
            if (remoteIpAddress == null)
            {
                // Nếu null, dùng IP Local hoặc một IP mặc định an toàn cho test
                return "127.0.0.1";
            }

            if (remoteIpAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                // Nếu là IPv6 (như ::1), tìm IPv4 hoặc trả về IP Loopback IPv4
                var ipv4Address = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList.FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                return ipv4Address != null ? ipv4Address.ToString() : "127.0.0.1";
            }

            return remoteIpAddress.ToString();
        }


        public VnPayResponseModel PaymentExecute(IQueryCollection collections)
        {
            // Sử dụng các class Helper đã tạo trong Data/VnPay
            var pay = new VnPayResponse(_hashSecret, collections);

            if (!pay.IsRequestValid())
            {
                return new VnPayResponseModel { Success = false, Message = "Chữ ký không hợp lệ" };
            }

            var vnpResponseCode = pay.GetResponseData("vnp_ResponseCode");
            var vnpTransactionStatus = pay.GetResponseData("vnp_TransactionStatus");
            var vnpTxnRef = pay.GetResponseData("vnp_TxnRef");
            var vnpOrderInfo = pay.GetResponseData("vnp_OrderInfo");
            var vnpAmount = pay.GetResponseData("vnp_Amount");
            var vnpTransactionNo = pay.GetResponseData("vnp_TransactionNo");

            // Xử lý logic nghiệp vụ
            int orderId = 0;
            if (vnpTxnRef != null && vnpTxnRef.Contains("_"))
            {
                int.TryParse(vnpTxnRef.Split('_')[0], out orderId);
            }
            decimal amount = 0;
            decimal.TryParse(vnpAmount, out amount);

            if (vnpResponseCode == "00" && vnpTransactionStatus == "00")
            {
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
