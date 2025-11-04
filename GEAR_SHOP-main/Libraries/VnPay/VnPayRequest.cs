using Microsoft.AspNetCore.WebUtilities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System;
using System.Net;

namespace TL4_SHOP.Data.VnPay
{
    public class VnPayRequest
    {
        private readonly SortedList<string, string> _requestData = new SortedList<string, string>(new VnPayCompare());
        private readonly string _vnpUrl;
        private readonly string _vnpHashSecret;

        public VnPayRequest(string tmnCode, string hashSecret, string baseUrl, string returnUrl)
        {
            _vnpUrl = baseUrl;
            _vnpHashSecret = hashSecret;

            // Các tham số cố định
            AddRequestData("vnp_TmnCode", tmnCode);
            AddRequestData("vnp_ReturnUrl", returnUrl);
        }

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                _requestData.Add(key, value);
            }
        }

        public string GetVnPayUrl()
        {
            var data = new StringBuilder();

            // 1. Chuỗi dữ liệu để tính Hash (HashData) - Không URL Encode
            var hashData = string.Join("&", _requestData.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => x.Key + "=" + x.Value));

            // 2. Tính chữ ký (Hash)
            var vnpSecureHash = VnPayHelper.HmacSHA512(_vnpHashSecret, hashData);

            // 3. Xây dựng chuỗi Query String (Tham số ĐÃ URL Encode 2 LẦN)
            foreach (var kv in _requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    // URL Encode lần 1
                    var encodedValue = WebUtility.UrlEncode(kv.Value);
                    // URL Encode lần 2 (ĐƯỢC YÊU CẦU BỞI VNPAY API)
                    var doubleEncodedValue = WebUtility.UrlEncode(encodedValue);

                    data.Append(kv.Key + "=" + doubleEncodedValue + "&");
                }
            }

            // 4. Thêm chữ ký vào URL
            var paymentUrl = _vnpUrl + "?" + data.ToString() + "vnp_SecureHash=" + vnpSecureHash;

            return paymentUrl;
        }
    }

    public class VnPayCompare : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            if (x == y) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return string.Compare(x, y, StringComparison.Ordinal);
        }
    }
}
