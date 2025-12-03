namespace TL4_SHOP.Models.ViewModels
{
    public class VnPayResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionId { get; set; }
        public string OrderInfo { get; set; }
    }
}
