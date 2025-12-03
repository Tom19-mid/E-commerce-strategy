using Microsoft.AspNetCore.Mvc;
using TL4_SHOP.Data;
using TL4_SHOP.Models;
using TL4_SHOP.Extensions;

namespace TL4_SHOP.Controllers
{
    public class CartController : BaseController
    {
        private readonly _4tlShopContext _context;
        private const string CART_KEY = "GioHang";

        public CartController(_4tlShopContext context) : base(context)
        {
            _context = context;
        }

        // Lấy giỏ hàng từ Session
        private List<CartItem> GetCart()
        {
            var cart = HttpContext.Session.GetObjectFromJson<List<CartItem>>(CART_KEY);
            if (cart == null)
            {
                cart = new List<CartItem>();
                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
            }
            return cart;
        }

        // Lưu giỏ hàng vào Session
        private void SaveCart(List<CartItem> cart)
        {
            HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = GetCart();
            return View("ShoppingCart", cart);
        }

        // Thêm sản phẩm vào giỏ (redirect về Index)
        public IActionResult AddToCart(int id)
        {
            var product = _context.SanPhams.FirstOrDefault(p => p.SanPhamId == id);
            if (product == null) return NotFound();

            // ❗ KIỂM TRA TỒN KHO
            if (product.SoLuongTon <= 0)
            {
                TempData["ErrorMessage"] = "Sản phẩm đã hết hàng!";
                return RedirectToAction("Index");  // hoặc Redirect về trang sản phẩm
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.SanPhamId == id);

            decimal effectivePrice = (product.GiaSauGiam.HasValue && product.GiaSauGiam.Value > 0
                                      && product.GiaSauGiam.Value < product.Gia)
                                      ? product.GiaSauGiam.Value : product.Gia;

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    SanPhamId = product.SanPhamId,
                    TenSanPham = product.TenSanPham,
                    GiaGoc = product.Gia,
                    GiaHienTai = effectivePrice,
                    SoLuong = 1,
                    HinhAnh = product.HinhAnh ?? ""
                });
            }
            else
            {
                if (item.SoLuong + 1 > product.SoLuongTon)
                {
                    TempData["ErrorMessage"] = "Không thể thêm vì vượt quá số lượng tồn!";
                    return RedirectToAction("Index");
                }

                item.SoLuong++;
            }

            SaveCart(cart);
            return RedirectToAction("Index");
        }


        // Thêm sản phẩm vào giỏ bằng Ajax
        [HttpPost]
        [HttpPost]
        public JsonResult AddToCartAjax(int productId, int quantity = 1)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, requireLogin = true, message = "Bạn cần đăng nhập để thêm vào giỏ." });
            }

            var product = _context.SanPhams.FirstOrDefault(p => p.SanPhamId == productId);
            if (product == null)
                return Json(new { success = false, message = "❌ Sản phẩm không tồn tại." });

            // ❗ KIỂM TRA TỒN KHO
            if (product.SoLuongTon <= 0)
            {
                return Json(new { success = false, message = "Sản phẩm đã hết hàng.", stock = 0 });
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.SanPhamId == productId);

            decimal effectivePrice = (product.GiaSauGiam.HasValue && product.GiaSauGiam.Value > 0
                                      && product.GiaSauGiam.Value < product.Gia)
                                      ? product.GiaSauGiam.Value : product.Gia;

            if (item == null)
            {
                cart.Add(new CartItem
                {
                    SanPhamId = product.SanPhamId,
                    TenSanPham = product.TenSanPham,
                    GiaGoc = product.Gia,
                    GiaHienTai = effectivePrice,
                    SoLuong = quantity,
                    HinhAnh = product.HinhAnh ?? ""
                });
            }
            else
            {
                if (item.SoLuong + quantity > product.SoLuongTon)
                {
                    return Json(new { success = false, message = $"Chỉ còn {product.SoLuongTon} sản phẩm trong kho." });
                }

                item.SoLuong += quantity;
            }

            SaveCart(cart);

            return Json(new
            {
                success = true,
                message = $"Đã thêm \"{product.TenSanPham}\" vào giỏ.",
                cartCount = cart.Sum(i => i.SoLuong),
                cartTotal = cart.Sum(i => i.ThanhTien).ToString("N0")
            });
        }



        // Remove, UpdateQuantity, CartCount, MiniCart ... cập nhật để dùng GiaHienTai
        public IActionResult Remove(int id)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.SanPhamId == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
            }
            return RedirectToAction("Index");
        }

        // Cập nhật số lượng
        [HttpPost]
        public IActionResult UpdateQuantity(int id, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.SanPhamId == id);
            if (item != null)
            {
                var product = _context.SanPhams.FirstOrDefault(p => p.SanPhamId == id);
                if (product != null)
                {
                    int stock = (int)product.SoLuongTon;
                    if (quantity <= 0) quantity = 1;
                    if (quantity > stock)
                    {
                        TempData["ErrorMessage"] = $"Chỉ còn {stock} sản phẩm trong kho. Số lượng đã được điều chỉnh.";
                        item.SoLuong = stock;
                    }
                    else
                    {
                        item.SoLuong = quantity;
                    }
                    SaveCart(cart);
                }
            }
            return RedirectToAction("Index");
        }


        // Trả về số lượng sản phẩm trong giỏ
        [HttpGet]
        public IActionResult CartCount()
        {
            var cart = GetCart();
            int count = cart.Sum(x => x.SoLuong);
            return Json(new { count });
        }

        // Mini Cart (hiển thị nhỏ ở header)
        public IActionResult MiniCart()
        {
            var cart = GetCart();
            return PartialView("_MiniCart", cart);
        }
    }
}
