using System.Security.Cryptography;
using System.Text;
using System;
using System.Linq;

namespace TL4_SHOP.Data.VnPay
{
    public static class VnPayHelper
    {
        // Thuật toán SHA512 để tạo chữ ký Hash
        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            // Xóa mọi ký tự trắng (whitespace) khỏi Secret Key để đảm bảo chính xác
            string cleanedKey = key.Replace(" ", "").Replace("\r", "").Replace("\n", "");

            using (var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(cleanedKey)))
            {
                byte[] hashBytes = hmac.ComputeHash(inputBytes);
                foreach (var b in hashBytes)
                {
                    hash.Append(b.ToString("x2")); // "x2" đảm bảo là chữ thường
                }
            }
            return hash.ToString(); // Trả về chuỗi hash chữ thường
        }
    }
}
