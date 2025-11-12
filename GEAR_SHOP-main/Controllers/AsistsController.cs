using Microsoft.AspNetCore.Mvc;
using TL4_SHOP.Data;
using TL4_SHOP.Models.ViewModels;

namespace TL4_SHOP.Controllers
{
    public class AsistsController : Controller
    {
        private readonly _4tlShopContext _context;

        public AsistsController(_4tlShopContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult FAQs()
        {
            return View();
        }

        public IActionResult GiupDo()
        {
            return View();
        }

        public IActionResult HoTro()
        {
            ViewBag.HideChat = true; // Báo cho view biến cần ẩn thanh chat nổi
            return View();
        }
    }
}
