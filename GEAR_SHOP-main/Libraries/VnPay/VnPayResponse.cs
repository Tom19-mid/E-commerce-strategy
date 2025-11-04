namespace TL4_SHOP.Data.VnPay
{
    public class VnPayResponse
    {
        private readonly SortedList<string, string> _responseData = new SortedList<string, string>(new VnPayCompare());
        private readonly string _hashSecret;

        public VnPayResponse(string hashSecret, IQueryCollection collections)
        {
            _hashSecret = hashSecret;

            foreach (string key in collections.Keys)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    _responseData.Add(key, collections[key]);
                }
            }
        }

        public string GetResponseData(string key)
        {
            return _responseData.TryGetValue(key, out var value) ? value : string.Empty;
        }

        public bool IsRequestValid()
        {
            // 1. Lấy SecureHash từ URL
            var secureHash = GetResponseData("vnp_SecureHash");
            if (string.IsNullOrEmpty(secureHash)) return false;

            // 2. Xóa SecureHash khỏi danh sách tham số để tính lại hash
            _responseData.Remove("vnp_SecureHash");

            // 3. Chuỗi dữ liệu để tính Hash
            var hashData = string.Join("&", _responseData.Where(x => !string.IsNullOrEmpty(x.Value)).Select(x => x.Key + "=" + x.Value));

            // 4. Tính toán lại SecureHash
            var calculatedHash = VnPayHelper.HmacSHA512(_hashSecret, hashData);

            // 5. So sánh
            return calculatedHash.Equals(secureHash, StringComparison.OrdinalIgnoreCase);
        }
    }
}