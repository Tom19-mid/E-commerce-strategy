using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TL4_SHOP.Data;

namespace TL4_SHOP.Controllers
{
    [ApiController]
    [Route("api/paypal/webhook")]
    public class PayPalWebhookController : ControllerBase
    {
        private readonly _4tlShopContext _context;

        public PayPalWebhookController(_4tlShopContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Webhook([FromBody] dynamic body)
        {
            if (body.event_type != "PAYMENT.CAPTURE.COMPLETED")
                return Ok();

            string transactionId = body.resource.id; // ✅ ID PayPal UI
            string invoiceId = body.resource.invoice_id; // DH_194

            int orderId = int.Parse(invoiceId.Replace("DH_", ""));

            var order = await _context.DonHangs.FindAsync(orderId);
            if (order == null) return Ok();

            order.TransactionId = transactionId;
            order.PhuongThucThanhToan = "PayPal";
            order.TrangThaiDonHangText = "Đã thanh toán";

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
