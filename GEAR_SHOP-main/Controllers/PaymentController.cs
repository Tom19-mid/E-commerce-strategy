using Microsoft.AspNetCore.Mvc;
using TL4_SHOP.Models;
using TL4_SHOP.Data;
using TL4_SHOP.Models.ViewModels; // Cần thiết cho PaymentMethodViewModel
using TL4_SHOP.Services; // Cần thiết cho IVnPayService
using System;
using System.Linq;

namespace TL4_SHOP.Controllers
{
    public class PaymentController : Controller
    {
        private readonly _4tlShopContext _context;
        private readonly IVnPayService _vnPayService; // <<< 1. Thêm Injection Interface

        public PaymentController(_4tlShopContext context, IVnPayService vnPayService) // <<< 1. Thêm Injection
        {
            _context = context;
            _vnPayService = vnPayService; // <<< Gán service
        }

        // =======================================================
        // Hiển thị trang chọn phương thức thanh toán
        // =======================================================
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
                TotalAmount = order.TongTien + order.PhiVanChuyen // Tổng tiền thanh toán (bao gồm phí vận chuyển)
            };

            return View(model);
        }

        // =======================================================
        // Xử lý thanh toán - Chuyển hướng sang cổng hoặc trang Processing
        // =======================================================
        [HttpPost]
        public IActionResult ProcessPayment(PaymentMethodViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("SelectMethod", model);
            }

            var order = _context.DonHangs.Find(model.OrderId);
            if (order == null)
            {
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index", "Home");
            }

            // Xử lý VNPay - Chuyển hướng trực tiếp
            if (model.SelectedMethod == "VNPay")
            {
                // Tổng tiền cần thanh toán
                decimal tongThanhToan = order.TongTien + order.PhiVanChuyen;

                var vnpayUrl = _vnPayService.CreatePaymentUrl(
                    order.DonHangId,
                    tongThanhToan,
                    HttpContext,
                    $"Thanh toan don hang DH{order.DonHangId} cho khach hang {order.TenKhachHang}"
                );

                // Chuyển hướng thẳng sang VNPay Gateway
                return Redirect(vnpayUrl);
            }

            // Xử lý các phương thức khác (COD, PayPal...) - Chuyển đến trang Loading Processing
            TempData["PaymentMethod"] = model.SelectedMethod;
            TempData["OrderId"] = model.OrderId;
            TempData["Amount"] = model.TotalAmount.ToString("0.##");

            return RedirectToAction("Processing", new { method = model.SelectedMethod, orderId = model.OrderId });
        }


        // =======================================================
        // Xử lý Callback từ VNPay (ReturnUrl)
        // =======================================================
        [HttpGet]
        public IActionResult PaymentCallback()
        {
            // Lấy tất cả tham số Query String từ VNPay
            var collections = HttpContext.Request.Query;

            // Xử lý kết quả VNPay
            var response = _vnPayService.PaymentExecute(collections);

            // Lấy orderId từ response. Nếu không lấy được, đặt là 0
            int orderId = response.OrderId;

            if (response.Success)
            {
                // Thành công: Cập nhật trạng thái đơn hàng trong database
                // Status Name = "Đã thanh toán"
                UpdateOrderStatus(response.OrderId, "Đã thanh toán", response.TransactionId);

                TempData["PaymentMethod"] = "VNPay";
                return RedirectToAction("Success", new { orderId = orderId });
            }
            else
            {
                // Thất bại: Giữ nguyên trạng thái đơn hàng là "Chờ xác nhận"
                TempData["PaymentMethod"] = "VNPay";
                TempData["ErrorMessage"] = response.Message;
                // Nếu không tìm được OrderId, chuyển về trang chủ
                if (orderId == 0) return RedirectToAction("Index", "Home");

                return RedirectToAction("Failed", new { orderId = orderId });
            }
        }


        // =======================================================
        // Trang loading giả lập quá trình xử lý thanh toán (chỉ cho COD/PayPal...)
        // =======================================================
        [HttpGet]
        public IActionResult Processing(string method, int orderId)
        {
            // ... (Giữ nguyên logic hiện tại)
            if (string.IsNullOrEmpty(method))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Method = method;
            ViewBag.OrderId = orderId;
            return View();
        }

        // =======================================================
        // Trang kết quả thanh toán thành công
        // =======================================================
        [HttpGet]
        public IActionResult Success(int orderId)
        {
            var paymentMethod = TempData["PaymentMethod"];
            var order = _context.DonHangs.Find(orderId);

            if (paymentMethod == null || order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Tạo TransactionId giả nếu phương thức không phải VNPay
            string transactionId = paymentMethod.ToString() == "VNPay" ?
                                   TempData["TransactionId"]?.ToString() ?? GenerateTransactionId() :
                                   GenerateTransactionId();


            var result = new PaymentResultViewModel
            {
                Success = true,
                Message = "Thanh toán thành công!",
                OrderId = orderId,
                Amount = order.TongTien + order.PhiVanChuyen,
                PaymentMethod = paymentMethod.ToString(),
                PaymentTime = DateTime.Now,
                TransactionId = transactionId
            };

            // Nếu phương thức là COD/Giả lập, Cập nhật trạng thái đơn hàng trong database.
            if (paymentMethod.ToString() != "VNPay")
            {
                UpdateOrderStatus(result.OrderId, "Đã thanh toán", result.TransactionId);
            }

            return View("Result", result);
        }

        // =======================================================
        // Trang kết quả thanh toán thất bại
        // =======================================================
        [HttpGet]
        public IActionResult Failed(int orderId)
        {
            var paymentMethod = TempData["PaymentMethod"];
            var order = _context.DonHangs.Find(orderId);

            var result = new PaymentResultViewModel
            {
                Success = false,
                Message = TempData["ErrorMessage"]?.ToString() ?? "Thanh toán thất bại hoặc đã bị hủy!",
                OrderId = orderId,
                Amount = (order?.TongTien ?? 0) + (order?.PhiVanChuyen ?? 0),
                PaymentMethod = paymentMethod?.ToString() ?? "",
                PaymentTime = DateTime.Now
            };

            return View("Result", result);
        }


        private string GenerateTransactionId()
        {
            return $"TXN{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }


        // Cập nhật hàm này để có thể lưu TransactionId của VNPay
        private void UpdateOrderStatus(int orderId, string statusName, string transactionId)
        {
            try
            {
                var order = _context.DonHangs
                    .FirstOrDefault(d => d.DonHangId == orderId);

                if (order != null)
                {
                    var trangThai = _context.TrangThaiDonHangs
                        .FirstOrDefault(t => t.TenTrangThai == statusName);

                    if (trangThai != null)
                    {
                        order.TrangThaiId = trangThai.TrangThaiId;
                        order.TrangThaiDonHangText = trangThai.TenTrangThai; // Cập nhật text status
                    }

                    // Nếu bảng DonHang có field MaGiaoDich (chưa thấy trong model của bạn)
                    // order.MaGiaoDich = transactionId; 

                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating order status: {ex.Message}");
            }
        }
    }
}
