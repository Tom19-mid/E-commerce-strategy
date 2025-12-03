using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TL4_SHOP.Data.VnPay
{
    public class VnPayRequest
    {
        private readonly SortedDictionary<string, string> _requestData = new();
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _baseUrl;
        private readonly string _returnUrl;

        // Constructor khớp với VnPayService: tmnCode, hashSecret, baseUrl, returnUrl
        public VnPayRequest(string tmnCode, string hashSecret, string baseUrl, string returnUrl)
        {
            _tmnCode = tmnCode;
            _hashSecret = hashSecret;
            _baseUrl = baseUrl;
            _returnUrl = returnUrl;

            // Thêm sẵn các params cơ bản
            AddRequestData("vnp_TmnCode", tmnCode);
            AddRequestData("vnp_ReturnUrl", returnUrl);
        }

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData[key] = value;
            }
        }

        public string GetRequestData(string key)
        {
            return _requestData.TryGetValue(key, out var value) ? value : string.Empty;
        }

        // URL encode theo chuẩn form URL encoding (+ cho space)
        private string UrlEncodeForVnPay(string value)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            var result = new StringBuilder();
            foreach (char c in value)
            {
                if (IsUnreserved(c))
                {
                    result.Append(c);
                }
                else if (c == ' ')
                {
                    // VNPay sử dụng '+' cho space
                    result.Append('+');
                }
                else
                {
                    // Các ký tự khác encode thành %XX
                    result.Append('%');
                    result.Append(((int)c).ToString("X2"));
                }
            }
            return result.ToString();
        }

        // Kiểm tra ký tự có phải unreserved character không
        private bool IsUnreserved(char c)
        {
            return (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z') ||
                   (c >= '0' && c <= '9') ||
                   c == '-' || c == '_' || c == '.' || c == '~';
        }

        // GetVnPayUrl không nhận parameter (khớp với VnPayService)
        public string GetVnPayUrl()
        {
            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║  Creating VNPay Payment URL                ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");

            // Sắp xếp parameters theo alphabet
            var sortedParams = _requestData.OrderBy(p => p.Key).ToList();

            Console.WriteLine("\n📋 Request Parameters:");
            foreach (var param in sortedParams)
            {
                Console.WriteLine($"  {param.Key} = {param.Value}");
            }

            // QUAN TRỌNG: Tạo query string với các giá trị ĐÃ ENCODE
            var queryString = string.Join("&",
                sortedParams.Select(p => $"{p.Key}={UrlEncodeForVnPay(p.Value)}")
            );

            Console.WriteLine($"\n🔗 Query String (encoded):");
            Console.WriteLine($"  {queryString}");

            // Tính toán secure hash
            var secureHash = VnPayHelper.HmacSHA512(_hashSecret, queryString);
            Console.WriteLine($"\n🔐 Secure Hash:");
            Console.WriteLine($"  {secureHash}");

            // Thêm secure hash vào URL
            var paymentUrl = $"{_baseUrl}?{queryString}&vnp_SecureHash={secureHash}";

            Console.WriteLine($"\n✅ Final Payment URL:");
            Console.WriteLine($"  {paymentUrl}");
            Console.WriteLine("\n════════════════════════════════════════════\n");

            return paymentUrl;
        }
    }
}