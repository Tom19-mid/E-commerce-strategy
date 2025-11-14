using System.Security.Cryptography;
using System.Text;

namespace TL4_SHOP.Data.VnPay
{
    public static class VnPayHelper
    {
        // Thuật toán HMAC-SHA512 để tạo chữ ký Hash
        public static string HmacSHA512(string key, string inputData)
        {
            // Xóa mọi ký tự trắng (whitespace) khỏi Secret Key để đảm bảo chính xác
            string cleanedKey = key.Replace(" ", "").Replace("\r", "").Replace("\n", "").Replace("\t", "");
            byte[] keyBytes = Encoding.UTF8.GetBytes(cleanedKey);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(inputBytes);
                // Convert sang chuỗi hex chữ thường
                var hash = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    hash.Append(b.ToString("x2"));
                }
                return hash.ToString();
            }
        }
    }
}