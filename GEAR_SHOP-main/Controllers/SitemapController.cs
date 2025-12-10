using Microsoft.AspNetCore.Mvc;
using SimpleMvcSitemap;
using System.Collections.Generic;

namespace TL4_SHOP.Controllers
{
    [Route("")] // optional
    public class SitemapController : Controller
    {
        [HttpGet("sitemap.xml")]
        public IActionResult Index()
        {
            var nodes = new List<SitemapNode>
            {
                new SitemapNode(Url.Action("Index", "Home", null, Request.Scheme))
            };

            var sitemap = new SitemapModel(nodes);
            return new SitemapProvider().CreateSitemap(sitemap);
        }
    }
}
