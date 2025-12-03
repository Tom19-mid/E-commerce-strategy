using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TL4_SHOP.Data.VnPay
{
    public class VnPayResponse
    {
        // Lưu trữ các parameters ĐÃ decode (ASP.NET Core tự động decode)
        private readonly SortedDictionary<string, string> _responseData = new();
        private readonly string _hashSecret;

        // Constructor khớp với VnPayService: hashSecret TRƯỚC, collections SAU
        public VnPayResponse(string hashSecret, IQueryCollection collections)
        {
            _hashSecret = hashSecret;

            Console.WriteLine("\n╔════════════════════════════════════════════╗");
            Console.WriteLine("║  VnPayResponse Constructor                ║");
            Console.WriteLine("╚════════════════════════════════════════════╝");

            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    // ASP.NET Core đã tự động decode, ta chỉ cần lưu lại
                    string decodedValue = value.ToString();
                    _responseData[key] = decodedValue;

                    Console.WriteLine($"[{key}] = [{decodedValue}]");
                }
            }

            Console.WriteLine($"Total params added: {_responseData.Count}");
            Console.WriteLine("════════════════════════════════════════════\n");
        }

        // Phương thức để lấy giá trị đã decode
        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var value) ? value : string.Empty;
        }

        // QUAN TRỌNG: URL encode lại giá trị theo cách VNPay yêu cầu
        // VNPay sử dụng '+' cho space, không phải '%20'
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
                    // VNPay sử dụng '+' cho space (form URL encoding)
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
        // Unreserved characters: A-Z, a-z, 0-9, -, _, ., ~
        private bool IsUnreserved(char c)
        {
            return (c >= 'A' && c <= 'Z') ||
                   (c >= 'a' && c <= 'z') ||
                   (c >= '0' && c <= '9') ||
                   c == '-' || c == '_' || c == '.' || c == '~';
        }

        // IsRequestValid không nhận parameter (khớp với VnPayService)
        public bool IsRequestValid()
        {
            Console.WriteLine("═══ IsRequestValid START ═══");

            // Lấy chữ ký từ VNPay gửi về
            var receivedHash = GetResponseData("vnp_SecureHash");
            Console.WriteLine($"Received Hash: [{receivedHash}]");

            if (string.IsNullOrEmpty(receivedHash))
            {
                Console.WriteLine("❌ No vnp_SecureHash found!");
                Console.WriteLine("═══ IsRequestValid END ═══\n");
                return false;
            }

            // Tạo danh sách params để hash (loại bỏ vnp_SecureHash và vnp_SecureHashType)
            var paramsToHash = _responseData
                .Where(p => !p.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                         && !p.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Key)
                .ToList();

            // QUAN TRỌNG: Tạo chuỗi hash data với các giá trị ĐÃ ENCODE LẠI
            // VNPay yêu cầu encode lại các giá trị trước khi hash
            var hashData = string.Join("&",
                paramsToHash.Select(p => $"{p.Key}={UrlEncodeForVnPay(p.Value)}")
            );

            Console.WriteLine($"HashData:");
            Console.WriteLine($"  {hashData}");
            Console.WriteLine($"HashSecret: {_hashSecret}");

            // Tính toán hash
            var calculatedHash = VnPayHelper.HmacSHA512(_hashSecret, hashData);

            Console.WriteLine($"\nCalculated Hash:");
            Console.WriteLine($"  {calculatedHash}");
            Console.WriteLine($"Received Hash:");
            Console.WriteLine($"  {receivedHash}");

            bool isValid = calculatedHash.Equals(receivedHash, StringComparison.OrdinalIgnoreCase);
            Console.WriteLine($"\nMatch: {isValid}");

            if (isValid)
            {
                Console.WriteLine("✅ Signature Valid!");
            }
            else
            {
                Console.WriteLine("❌ Invalid signature!");
            }

            Console.WriteLine("═══ IsRequestValid END ═══\n");

            return isValid;
        }

        // Properties để truy cập các giá trị thông dụng
        public string TxnRef => GetResponseData("vnp_TxnRef");
        public string Amount => GetResponseData("vnp_Amount");
        public string OrderInfo => GetResponseData("vnp_OrderInfo");
        public string ResponseCode => GetResponseData("vnp_ResponseCode");
        public string TransactionStatus => GetResponseData("vnp_TransactionStatus");
        public string PayDate => GetResponseData("vnp_PayDate");
        public string BankCode => GetResponseData("vnp_BankCode");
        public string BankTranNo => GetResponseData("vnp_BankTranNo");
        public string CardType => GetResponseData("vnp_CardType");
        public string TransactionNo => GetResponseData("vnp_TransactionNo");
    }
}