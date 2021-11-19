using AngleSharp;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;

namespace SaveFifteen.M10Scraper.Pages
{
    internal class DepartmentPage : Page
    {
        public DepartmentPage(string url, IBrowsingContext context) : base(url, context)
        {
        }

        public async Task<IEnumerable<DepartmentPage>> GetAllSubDepartmentPages()
        {
            var urls = await GetDepartmentUrls();

            return urls.Select(url => new DepartmentPage(url, Context));
        }

        public async Task<IEnumerable<ProductListPage>> GetAllProductListPages()
        {
            var pageCount = await GetPageCount();

            Console.WriteLine($"{pageCount} pages found on {(await GetDocument()).Url}");

            var result = Enumerable.Range(0, pageCount).Select(page => 
            {
                var productListPageUrl = Url.ToString() + $"?q=%3Arelevance&page={page}";
                var productListPage = new ProductListPage(productListPageUrl, Context);
                
                Console.WriteLine($"Found page {productListPageUrl}");

                return productListPage;
            });

            return result;
        }


        private async Task<IEnumerable<string>> GetDepartmentUrls()
        {
            var linkSelector = "#department-count-component li a";
            var links = (await GetDocument()).QuerySelectorAll(linkSelector);

            var result = new List<string>();
            foreach (var link in links)
            {
                var href = link.Attributes["href"].Value;
                if (Regex.Match(href, "^/shop").Success)
                {
                    result.Add(BASE_URL + link.Attributes["href"].Value);
                }
            }

            return result;
        }

        private async Task<string> GetTitle()
        {
            return Regex.Replace((await GetDocument()).Title, "\\s*\\|.+", "");
        }

        private async Task<int> GetPageCount()
        {
            // this looks like "1 - 20 of 510" or "18 results found"
            var document = await GetDocument();
            if (document == null) return 0;

            var resultCountElement = document.QuerySelector(".result-count");
            if (resultCountElement == null) return 0;

            var resultCountText = resultCountElement.TextContent;
            if (resultCountText == null) return 0;

            var pageIndicator = (await GetDocument()).QuerySelector(".result-count").TextContent;

            if (Regex.Match(pageIndicator, @"\d{1,3} results").Success)
            {
                return 1;
            }

            var matches = Regex.Match(pageIndicator, @"\d{1,3} - (\d{1,3}) of (\d{1,5})");
            var pageSize = Int32.Parse(matches.Groups[1].Value);
            var pageCount = Int32.Parse(matches.Groups[2].Value);

            var result = Math.Ceiling((decimal)pageCount / (decimal)pageSize);

            return ((int)result);
        }
    }
}