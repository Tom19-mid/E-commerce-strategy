namespace TL4_SHOP.Models
{

    public class PaymentMethodViewModel
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string SelectedMethod { get; set; }
    }

    public class PaymentResultViewModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; }
        public DateTime PaymentTime { get; set; }
        public string TransactionId { get; set; }
    }
}