using Microsoft.AspNetCore.Mvc;
using TL4_SHOP.Data;

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
            return View();
        }
    }
}
