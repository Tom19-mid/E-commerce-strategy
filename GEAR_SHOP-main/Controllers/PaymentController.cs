using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization; 
using TL4_SHOP.Models;
using TL4_SHOP.Data;
using TL4_SHOP.Models.ViewModels;
using TL4_SHOP.Services;
using System;
using System.Linq;
using TL4_SHOP.Services; 
using System.Threading.Tasks; 
using PayPalCheckoutSdk.Orders;
using System.IO;

namespace TL4_SHOP.Controllers
{
    public class PaymentController : Controller
    {
        private readonly _4tlShopContext _context;
        private readonly IVnPayService _vnPayService;
        private readonly IPayPalService _payPalService;

        public PaymentController(_4tlShopContext context, IVnPayService vnPayService, IPayPalService payPalService)
        {
            _context = context;
            _vnPayService = vnPayService;
            _payPalService = payPalService;
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
                //TotalAmount = order.TongTien + order.PhiVanChuyen
                TotalAmount = order.TongTien // Không cộng thêm tiền phí vận chuyển nữa
            };

            return View(model);
        }

        // =======================================================
        // Xử lý thanh toán - Chuyển hướng sang cổng
        // =======================================================
        [HttpPost]
        [ValidateAntiForgeryToken]  // ← Thêm attribute này vì đã có @Html.AntiForgeryToken()
        public async Task<IActionResult> ProcessPayment(PaymentMethodViewModel model)
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

            // ===========================================
            // 💰 XỬ LÝ PAYPAL
            // ===========================================
            if (model.SelectedMethod == "PayPal")
            {
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine("💳 Processing PayPal payment...");
                Console.WriteLine("═══════════════════════════════════════");

                decimal exchangeRate = 25000;
                //decimal totalAmountVND = order.TongTien + order.PhiVanChuyen;
                decimal totalAmountVND = order.TongTien; // Không cộng thêm tiền phí vận chuyển nữa
                decimal totalUsd = Math.Round(totalAmountVND / exchangeRate, 2);
                string currency = "USD";

                Console.WriteLine($"💰 Total VND: {totalAmountVND}đ (Rate: {exchangeRate})");
                Console.WriteLine($"💰 Total USD: ${totalUsd}");

                var returnUrl = Url.Action("CapturePayment", "Payment", new { orderId = order.DonHangId }, Request.Scheme);
                var cancelUrl = Url.Action("CancelPayment", "Payment", new { orderId = order.DonHangId }, Request.Scheme);

                Console.WriteLine($"➡️ Return URL: {returnUrl}");
                Console.WriteLine($"⬅️ Cancel URL: {cancelUrl}");

                try
                {
                    // Chỗ này bạn dùng 'await' nên method ProcessPayment phải là 'async Task'
                    var response = await _payPalService.CreateOrderAsync(totalUsd, currency, order.DonHangId, returnUrl, cancelUrl);

                    var result = response.Result<Order>();
                    string approveLink = result.Links.FirstOrDefault(link => link.Rel == "approve")?.Href;

                    if (!string.IsNullOrEmpty(approveLink))
                    {
                        Console.WriteLine($"🔄 Redirecting to PayPal: {approveLink}");
                        return Redirect(approveLink);
                    }
                    else
                    {
                        Console.WriteLine("❌ Lỗi: Không tìm thấy link 'approve' từ PayPal.");
                        TempData["Error"] = "Lỗi khi tạo link thanh toán PayPal.";
                        return View("SelectMethod", model);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ EXCEPTION khi gọi PayPal: {ex.Message}");
                    TempData["Error"] = "Lỗi khi kết nối với PayPal: " + ex.Message;
                    return View("SelectMethod", model);
                }
            }

            // Xử lý VNPay
            if (model.SelectedMethod == "VNPay")
            {
                Console.WriteLine("═══════════════════════════════════════");
                Console.WriteLine("💳 Processing VNPay payment...");
                Console.WriteLine("═══════════════════════════════════════");

                //decimal tongThanhToan = order.TongTien + order.PhiVanChuyen;
                decimal tongThanhToan = order.TongTien; // Không cộng thêm tiền phí vận chuyển nữa
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
        public async Task<IActionResult> CapturePayment(int orderId, string token)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════╗");
            Console.WriteLine("║  ✅ PAYPAL CAPTURE ACTION ĐƯỢC GỌI!               ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════╝");
            Console.WriteLine($"📦 OrderId: {orderId}");
            Console.WriteLine($"🔑 Token: {token}");

            try
            {
                var response = await _payPalService.CaptureOrderAsync(token);
                var result = response.Result<Order>();
                PaymentResultViewModel resultModel;

                if (result.Status == "COMPLETED")
                {
                    Console.WriteLine("✅ THANH TOÁN PAYPAL THÀNH CÔNG!");
                    string payPalCaptureId = result.PurchaseUnits[0].Payments.Captures[0].Id;
                    string payPalOrderId = token;
                    Console.WriteLine($"💳 Capture ID: {payPalCaptureId}");
                    Console.WriteLine($"🧾 Order ID: {payPalOrderId}");

                    //UpdateOrderStatus(orderId, "Đã thanh toán", payPalCaptureId, "PayPal");
                    var order = _context.DonHangs.Find(orderId);
                    if (order != null)
                    {
                        order.TransactionId = payPalCaptureId;     // dùng Capture ID làm giao dịch
                        order.PhuongThucThanhToan = "PayPal";
                        order.TrangThaiDonHangText = "Đã thanh toán";

                        _context.SaveChanges();
                    }

                    // ◀️ SỬA LỖI 1: Xử lý CreateTime có thể bị null
                    DateTime paymentTime;
                    if (!string.IsNullOrEmpty(result.CreateTime))
                    {
                        paymentTime = DateTime.Parse(result.CreateTime);
                    }
                    else
                    {
                        paymentTime = DateTime.Now; // Dùng giờ hiện tại làm dự phòng
                    }

                    resultModel = new PaymentResultViewModel
                    {
                        Success = true,
                        Message = "Thanh toán PayPal thành công!",
                        OrderId = orderId,
                        //Amount = (order?.TongTien ?? 0) + (order?.PhiVanChuyen ?? 0),
                        Amount = (order?.TongTien ?? 0), // Không cộng thêm tiền phí vận chuyển nữa
                        PaymentMethod = "PayPal",
                        PaymentTime = paymentTime, // ◀️ Dùng biến đã xử lý
                        TransactionId = payPalCaptureId
                    };
                }
                else
                {
                    Console.WriteLine($"⚠️ PAYPAL STATUS: {result.Status}");
                    // Lấy đơn hàng để hiển thị số tiền (kể cả khi status != COMPLETED)
                    var order = _context.DonHangs.Find(orderId);
                    resultModel = new PaymentResultViewModel
                    {
                        Success = false,
                        Message = $"Thanh toán PayPal không thành công. (Status: {result.Status})",
                        OrderId = orderId,
                        PaymentMethod = "PayPal",
                       // Amount = (order?.TongTien ?? 0) + (order?.PhiVanChuyen ?? 0) // ◀️ Thêm Amount
                        Amount = (order?.TongTien ?? 0) // Không cộng thêm tiền phí vận chuyển nữa
                    };
                }

                return View("Result", resultModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ EXCEPTION khi Capture PayPal: {ex.Message}");

                // ◀️ SỬA LỖI 2: Lấy thông tin đơn hàng để hiển thị số tiền khi có exception
                var order = _context.DonHangs.Find(orderId);

                var failResult = new PaymentResultViewModel
                {
                    Success = false,
                    Message = "Lỗi nghiêm trọng khi xác nhận thanh toán PayPal: " + ex.Message,
                    OrderId = orderId,
                    PaymentMethod = "PayPal",
                    //Amount = (order?.TongTien ?? 0) + (order?.PhiVanChuyen ?? 0) // ◀️ Thêm dòng này
                    Amount = (order?.TongTien ?? 0) // Không cộng thêm tiền phí vận chuyển nữa
                };
                return View("Result", failResult);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult CancelPayment(int orderId)
        {
            Console.WriteLine("╔═══════════════════════════════════════════════════╗");
            Console.WriteLine("║  ❌ PAYPAL CANCEL ACTION ĐƯỢC GỌI!                ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════╝");
            Console.WriteLine($"📦 OrderId: {orderId}");

            var order = _context.DonHangs.Find(orderId);

            var result = new PaymentResultViewModel
            {
                Success = false,
                Message = "Bạn đã hủy giao dịch thanh toán qua PayPal.",
                OrderId = orderId,
                //Amount = (order?.TongTien ?? 0) + (order?.PhiVanChuyen ?? 0),
                Amount = (order?.TongTien ?? 0), // Không cộng thêm tiền phí vận chuyển nữa
                PaymentMethod = "PayPal",
                PaymentTime = DateTime.Now
            };

            // Không cần cập nhật DB vì đơn hàng vẫn ở trạng thái "Chờ xác nhận"
            // Người dùng có thể thử lại từ trang chi tiết đơn hàng (nếu bạn có chức năng đó)
            // hoặc từ trang chọn phương thức thanh toán.

            return View("Result", result); // Dùng lại view Result.cshtml
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
                    UpdateOrderStatus(response.OrderId, "Đã thanh toán", response.TransactionId, "VNPay");
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
                //Amount = order.TongTien + order.PhiVanChuyen,
                Amount = order.TongTien, // Không cộng thêm tiền phí vận chuyển nữa
                PaymentMethod = paymentMethod.ToString(),
                PaymentTime = DateTime.Now,
                TransactionId = transactionId
            };

            // Cập nhật trạng thái đơn hàng
            UpdateOrderStatus(result.OrderId, "Đã thanh toán", result.TransactionId, paymentMethod.ToString());

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
                //Amount = (order?.TongTien ?? 0) + (order?.PhiVanChuyen ?? 0),
                Amount = (order?.TongTien ?? 0), // Không cộng thêm tiền phí vận chuyển nữa
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

        private void UpdateOrderStatus(int orderId, string statusName, string transactionId, string paymentMethodName)
        {
            try
            {
                var order = _context.DonHangs.FirstOrDefault(d => d.DonHangId == orderId);

                if (order != null)
                {
                    // 1. Cập nhật trạng thái
                    var trangThai = _context.TrangThaiDonHangs
                        .FirstOrDefault(t => t.TenTrangThai == statusName);

                    if (trangThai != null)
                    {
                        order.TrangThaiId = trangThai.TrangThaiId;
                        order.TrangThaiDonHangText = trangThai.TenTrangThai;
                    }
                    // 2. Lưu TransactionId và Phương thức thanh toán vào database
                    if (!string.IsNullOrEmpty(transactionId) && string.IsNullOrEmpty(order.TransactionId))
                    {
                        order.TransactionId = transactionId;
                    }

                    // 3. Lưu Phương thức thanh toán vào database
                    // Dùng biến 'paymentMethodName' được truyền vào, KHÔNG dùng chữ 'PaymentMethod'
                    if (!string.IsNullOrEmpty(paymentMethodName))
                    {
                        order.PhuongThucThanhToan = paymentMethodName;
                    }

                    _context.SaveChanges();
                    Console.WriteLine($"✅ Order {orderId} updated successfully.");
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