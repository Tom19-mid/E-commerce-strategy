using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;  // ← THÊM USING NÀY
using TL4_SHOP.Models;
using TL4_SHOP.Data;
using TL4_SHOP.Models.ViewModels;
using TL4_SHOP.Services;
using System;
using System.Linq;

namespace TL4_SHOP.Controllers
{
    public class PaymentController : Controller
    {
        private readonly _4tlShopContext _context;
        private readonly IVnPayService _vnPayService;

        public PaymentController(_4tlShopContext context, IVnPayService vnPayService)
        {
            _context = context;
            _vnPayService = vnPayService;
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
                TotalAmount = order.TongTien + order.PhiVanChuyen
            };

            return View(model);
        }

        // =======================================================
        // Xử lý thanh toán - Chuyển hướng sang cổng
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]  // ← Thêm attribute này vì đã có @Html.AntiForgeryToken()
        public IActionResult ProcessPayment(PaymentMethodViewModel model)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════╗");
            Console.WriteLine("║  🎯 PROCESSPAYMENT ACTION CALLED!               ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════╝");
            Console.WriteLine($"📦 OrderId: {model.OrderId}");
            Console.WriteLine($"💳 Selected Method: {model.SelectedMethod}");
            Console.WriteLine($"💰 Total Amount: {model.TotalAmount}");
            Console.WriteLine($"✅ ModelState Valid: {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                Console.WriteLine("❌ ModelState INVALID!");
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine($"   Error: {error.ErrorMessage}");
                }
                return View("SelectMethod", model);
            }

            var order = _context.DonHangs.Find(model.OrderId);
            if (order == null)
            {
                Console.WriteLine($"❌ Order {model.OrderId} NOT FOUND!");
                TempData["Error"] = "Không tìm thấy đơn hàng!";
                return RedirectToAction("Index", "Home");
            }

            Console.WriteLine($"✅ Order found: ID={order.DonHangId}, Total={order.TongTien}");

            // Xử lý VNPay
            if (model.SelectedMethod == "VNPay")
            {
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine("💳 Processing VNPay payment...");
                Console.WriteLine("═══════════════════════════════════════");

                decimal tongThanhToan = order.TongTien + order.PhiVanChuyen;
                Console.WriteLine($"💰 Total payment amount: {tongThanhToan}");

                var vnpayUrl = _vnPayService.CreatePaymentUrl(
                    order.DonHangId,
                    tongThanhToan,
                    HttpContext,
                    $"Thanh toan don hang DH{order.DonHangId} cho khach hang {order.TenKhachHang}"
                );

                Console.WriteLine($"🌐 VNPay URL created: {vnpayUrl.Substring(0, Math.Min(100, vnpayUrl.Length))}...");
                Console.WriteLine($"🔄 Redirecting to VNPay...");
                Console.WriteLine("═══════════════════════════════════════");

                return Redirect(vnpayUrl);
            }

            // Xử lý các phương thức khác
            Console.WriteLine($"💰 Processing {model.SelectedMethod} payment...");
            TempData["PaymentMethod"] = model.SelectedMethod;
            TempData["OrderId"] = model.OrderId;
            TempData["Amount"] = model.TotalAmount.ToString("0.##");

            return RedirectToAction("Processing", new { method = model.SelectedMethod, orderId = model.OrderId });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Result()
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════╗");
            Console.WriteLine("║  ✅ PAYMENT/RESULT ACTION ĐƯỢC GỌI!              ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════╝");

            try
            {
                var collections = Request.Query;

                // ✅ LOG TẤT CẢ PARAMS
                Console.WriteLine("=== ALL QUERY PARAMS ===");
                foreach (var key in collections.Keys)
                {
                    Console.WriteLine($"  {key} = {collections[key]}");
                }
                Console.WriteLine($"Total params: {collections.Count}");
                Console.WriteLine("========================");

                if (!collections.Any())
                {
                    Console.WriteLine("❌ ERROR: No query parameters!");
                    return Content("ERROR: No query parameters received from VNPay", "text/plain");
                }

                // ✅ XỬ LÝ VNPAY RESPONSE
                Console.WriteLine("⏳ Calling _vnPayService.PaymentExecute...");
                var response = _vnPayService.PaymentExecute(collections);
                Console.WriteLine($"✅ Response Success: {response.Success}");
                Console.WriteLine($"📝 Message: {response.Message}");
                Console.WriteLine($"🆔 OrderId: {response.OrderId}");

                // ✅ TÌM ĐỢN HÀNG
                var order = _context.DonHangs.Find(response.OrderId);
                if (order == null)
                {
                    Console.WriteLine($"❌ Order {response.OrderId} NOT FOUND!");
                    return Content($"ERROR: Order {response.OrderId} not found in database", "text/plain");
                }

                Console.WriteLine($"✅ Order found: {order.DonHangId}");

                // ✅ TẠO VIEW MODEL
                var result = new PaymentResultViewModel
                {
                    Success = response.Success,
                    Message = response.Message,
                    OrderId = response.OrderId,
                    Amount = response.Amount,
                    PaymentMethod = "VNPay",
                    PaymentTime = DateTime.Now,
                    TransactionId = response.TransactionId ?? "N/A"
                };

                // ✅ CẬP NHẬT TRẠNG THÁI
                if (response.Success)
                {
                    Console.WriteLine($"⏳ Updating order {response.OrderId} status...");
                    UpdateOrderStatus(response.OrderId, "Đã thanh toán", response.TransactionId);
                    Console.WriteLine($"✅ Order updated!");
                }

                Console.WriteLine("🎯 Attempting to render Result view...");

                // ✅ KIỂM TRA VIEW TỒN TẠI
                var viewPath = Path.Combine(Directory.GetCurrentDirectory(), "Views", "Payment", "Result.cshtml");
                Console.WriteLine($"📁 View path: {viewPath}");
                Console.WriteLine($"📁 View exists: {System.IO.File.Exists(viewPath)}");

                return View("Result", result);
            }
            catch (Exception ex)
            {
                Console.WriteLine("╔═══════════════════════════════════════════════════╗");
                Console.WriteLine("║  ❌❌❌ EXCEPTION IN PAYMENT/RESULT              ║");
                Console.WriteLine("╚═══════════════════════════════════════════════════╝");
                Console.WriteLine($"Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }

                // ❌ RETURN TEXT ĐỂ DEBUG
                return Content($@"
                ╔════════════════════════════════════════╗
                ║  ❌ ERROR IN PAYMENT/RESULT           ║
                ╚════════════════════════════════════════╝

                Exception: {ex.GetType().Name}
                Message: {ex.Message}

                Stack Trace:
                {ex.StackTrace}

                Inner Exception:
                {ex.InnerException?.Message ?? "None"}

                Inner Stack Trace:
                {ex.InnerException?.StackTrace ?? "None"}
                        ", "text/plain");
                            }
                        }

        // =======================================================
        // Trang loading giả lập quá trình xử lý thanh toán
        // =======================================================
        [HttpGet]
        public IActionResult Processing(string method, int orderId)
        {
            if (string.IsNullOrEmpty(method))
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Method = method;
            ViewBag.OrderId = orderId;
            return View();
        }

        // =======================================================
        // Trang kết quả thanh toán thành công (cho COD/khác)
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

            string transactionId = GenerateTransactionId();

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

            // Cập nhật trạng thái đơn hàng
            UpdateOrderStatus(result.OrderId, "Đã thanh toán", result.TransactionId);

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

        // =======================================================
        // Helper Methods
        // =======================================================
        private string GenerateTransactionId()
        {
            return $"TXN{DateTime.Now:yyyyMMddHHmmss}{new Random().Next(1000, 9999)}";
        }

        private void UpdateOrderStatus(int orderId, string statusName, string transactionId)
        {
            try
            {
                var order = _context.DonHangs.FirstOrDefault(d => d.DonHangId == orderId);

                if (order != null)
                {
                    var trangThai = _context.TrangThaiDonHangs
                        .FirstOrDefault(t => t.TenTrangThai == statusName);

                    if (trangThai != null)
                    {
                        order.TrangThaiId = trangThai.TrangThaiId;
                        order.TrangThaiDonHangText = trangThai.TenTrangThai;
                    }

                    _context.SaveChanges();
                    Console.WriteLine($"✅ Order {orderId} status updated");
                }
                else
                {
                    Console.WriteLine($"❌ Order {orderId} not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating order: {ex.Message}");
            }
        }
    }
}