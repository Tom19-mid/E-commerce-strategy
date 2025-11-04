using Microsoft.AspNetCore.Mvc;
using TL4_SHOP.Models;
using TL4_SHOP.Data;

namespace TL4_SHOP.Controllers
{
    public class PaymentController : Controller
    {
        private readonly _4tlShopContext _context;

        public PaymentController(_4tlShopContext context)
        {
            _context = context;
        }

        /// Hiển thị trang chọn phương thức thanh toán

        [HttpGet]
        public IActionResult SelectMethod(int orderId)
        {
            var order = _context.DonHangs.Find(orderId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index", "Home");
            }

            var model = new PaymentMethodViewModel
            {
                OrderId = orderId,
                TotalAmount = order.TongTien
            };

            return View(model);
        }


        /// Xử lý thanh toán - chuyển đến trang Processing

        [HttpPost]
        public IActionResult ProcessPayment(PaymentMethodViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("SelectMethod", model);
            }

            // Lưu thông tin vào TempData để sử dụng ở các trang tiếp theo
            TempData["PaymentMethod"] = model.SelectedMethod;
            TempData["OrderId"] = model.OrderId;
            TempData["Amount"] = model.TotalAmount;

            // Redirect đến trang xử lý thanh toán (loading animation)
            return RedirectToAction("Processing", new { method = model.SelectedMethod });
        }


        /// Trang loading giả lập quá trình xử lý thanh toán

        [HttpGet]
        public IActionResult Processing(string method)
        {
            if (string.IsNullOrEmpty(method))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Method = method;
            return View();
        }

        /// Trang kết quả thanh toán thành công

        [HttpGet]
        public IActionResult Success()
        {
            var orderId = TempData["OrderId"];
            var amount = TempData["Amount"];
            var paymentMethod = TempData["PaymentMethod"];

            if (orderId == null || amount == null || paymentMethod == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var result = new PaymentResultViewModel
            {
                Success = true,
                Message = "Thanh toán thành công!",
                OrderId = (int)orderId,
                Amount = (decimal)amount,
                PaymentMethod = paymentMethod.ToString(),
                PaymentTime = DateTime.Now,
                TransactionId = GenerateTransactionId()
            };

            // Cập nhật trạng thái đơn hàng trong database
            UpdateOrderStatus(result.OrderId, "Đã thanh toán", result.TransactionId);

            return View("Result", result);
        }

       
        /// Trang kết quả thanh toán thất bại
        
        [HttpGet]
        public IActionResult Failed()
        {
            var orderId = TempData["OrderId"];
            var amount = TempData["Amount"];
            var paymentMethod = TempData["PaymentMethod"];

            var result = new PaymentResultViewModel
            {
                Success = false,
                Message = "Thanh toán thất bại hoặc đã bị hủy!",
                OrderId = orderId != null ? (int)orderId : 0,
                Amount = amount != null ? (decimal)amount : 0,
                PaymentMethod = paymentMethod?.ToString() ?? "",
                PaymentTime = DateTime.Now
            };

            return View("Result", result);
        }

        
        private string GenerateTransactionId()
        {
            return $"TXN{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }


        private void UpdateOrderStatus(int orderId, string statusName, string transactionId)
        {
            try
            {
                var order = _context.DonHangs.Find(orderId);
                if (order != null)
                {
                    // ✅ Tìm TrangThaiId dựa trên TenTrangThai
                    var trangThai = _context.TrangThaiDonHangs
                        .FirstOrDefault(t => t.TenTrangThai == statusName);

                    if (trangThai != null)
                    {
                        order.TrangThaiId = trangThai.TrangThaiId;
                    }

                    // Nếu bảng DonHang có field MaGiaoDich thì bỏ comment dòng dưới:
                    // order.MaGiaoDich = transactionId;

                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                // Log error nếu cần
                Console.WriteLine($"Error updating order status: {ex.Message}");
            }
        }
    }
}
