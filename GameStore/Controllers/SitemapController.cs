using Microsoft.AspNetCore.Mvc;
using GameStore.Models;
using System.Text;
using GameStore.Helpers; // Gọi thư viện SlugHelper để lấy link đẹp

namespace GameStore.Controllers
{
    public class SitemapController : Controller
    {
        private readonly GameStoreDBContext _context;

        public SitemapController(GameStoreDBContext context)
        {
            _context = context;
        }

        // Đường dẫn truy cập sẽ là: localhost:xxxx/sitemap.xml
        [Route("sitemap.xml")]
        public IActionResult Index()
        {
            var products = _context.Products.ToList();
            var sb = new StringBuilder();

            sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

            sb.AppendLine("<url>");
            sb.AppendLine($"<loc>{GetBaseUrl()}</loc>");
            sb.AppendLine("<lastmod>" + DateTime.Now.ToString("yyyy-MM-dd") + "</lastmod>");
            sb.AppendLine("<changefreq>daily</changefreq>");
            sb.AppendLine("<priority>1.0</priority>");
            sb.AppendLine("</url>");

            foreach (var p in products)
            {
                var slug = SlugHelper.GenerateSlug(p.Name);
                var url = $"{GetBaseUrl()}/san-pham/{slug}-{p.Id}";

                sb.AppendLine("<url>");
                sb.AppendLine($"<loc>{url}</loc>"); 
                sb.AppendLine("<lastmod>" + DateTime.Now.ToString("yyyy-MM-dd") + "</lastmod>");
                sb.AppendLine("<changefreq>weekly</changefreq>");
                sb.AppendLine("<priority>0.8</priority>");
                sb.AppendLine("</url>");
            }


            sb.AppendLine("</urlset>");

            return Content(sb.ToString(), "application/xml", Encoding.UTF8);
        }
        private string GetBaseUrl()
        {
            return $"{Request.Scheme}://{Request.Host}";
        }
    }
}