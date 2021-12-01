using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;

namespace SaveFifteen.M10Scraper.Pages
{
    internal class ProductListPage : Page
    {
        public ProductListPage(string url, IBrowsingContext context) : base(url, context)
        {
        }

        public async Task<IEnumerable<ProductPage>> GetAllProducts()
        {
            var pageUrls = (await GetDocument()).QuerySelectorAll(".product-link");
            Console.WriteLine($"Found {pageUrls.Count()} page URLs on this product list page ({(await GetDocument()).Url})");
            
            var productTasks = new List<Task<ProductPage>>();

            return pageUrls.Select(url => new ProductPage(BASE_URL + url.Attributes["href"].Value, Context));
        }
    }
}