using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;
using SaveFifteen.M10Scraper.Pages;

namespace SaveFifteen.M10Scraper
{
    public class Mitre10Scraper
    {
        private const string START_URL = "https://www.mitre10.co.nz/shop";
        private const string BASE_URL = "https://www.mitre10.co.nz";


        public async Task Scrape()
        {
            var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

            var frontPage = new ShopPage(START_URL, context);
            var topLevelDepartments = (await frontPage.GetAllDepartments()).Skip(2).Take(2);
            var departmentTasks = topLevelDepartments.Select(dpt => dpt.GetAllSubDepartmentPages());
            var departmentLists = await Task.WhenAll(departmentTasks);
            var allDepartments = departmentLists.SelectMany(departments => departments);

            var productListPageTasks = allDepartments.Select(dpt => dpt.GetAllProductListPages()).ToList();
            var pageList = await Task.WhenAll(productListPageTasks);
            var productListPages = pageList.SelectMany(pageList => pageList);

            var allProductPageTasks = productListPages.Select(listPage => listPage.GetAllProducts());
            var allProductPages = (await Task.WhenAll(allProductPageTasks)).SelectMany(prod => prod);

            Console.WriteLine($"{allProductPages.Count()} product pages found");

            //return allProductPages;
        }

        private static int requestCount = 0;
        private Task<IDocument> Get(string url, IBrowsingContext context)
        {
            requestCount++;
            Console.WriteLine($"{requestCount} |\t {url}");
            return context.OpenAsync(url);
        }

        private async Task<IEnumerable<Product>> GetAllProducts(IEnumerable<Department> allDepartments, IBrowsingContext context)
        {
            var result = new List<Product>();
            foreach (var department in allDepartments)
            {
                var pageCount = GetPageCount(department);
                Console.WriteLine($"{pageCount} pages in department {department.SubdepartmentName}");

                for (var i = 0; i < pageCount; i++)
                {
                    var productListPageUrl = department.Url.ToString() + $"?q=%3Arelevance&page={i}";
                    var page = await GetProductListPage(productListPageUrl, context);

                    var productTaskList = GetProductPagesFromListPage(page, context);

                    while (productTaskList.Any())
                    {
                        var productTask = await Task.WhenAny(productTaskList);
                        result.Add(productTask.Result);
                        productTaskList.Remove(productTask);
                    }
                }
            }

            return result;
        }

        private List<Task<Product>> GetProductPagesFromListPage(IDocument page, IBrowsingContext context)
        {
            var pageUrls = page.QuerySelectorAll(".product-link");
            
            var productTasks = new List<Task<Product>>();

            foreach (var link in pageUrls)
            {
                productTasks.Add(Task.Run(async () =>
                {
                    var productDocument = await Get(BASE_URL + link.Attributes["href"].Value, context);

                    var productName = GetProductName(productDocument);
                    var productSku = GetProductSku(productDocument);
                    var productModel = GetProductModel(productDocument);
                    return new Product(new Uri(productDocument.Url), productName, productSku, productModel);
                }));
            }

            return productTasks;
        }

        private Task<IDocument> GetProductListPage(string productUrl, IBrowsingContext context)
        {
            return Get(productUrl, context);
        }

        private string GetProductName(IDocument productPage)
        {
            var productNameSelector = ".product--title";
            return productPage.QuerySelector(productNameSelector).TextContent.Trim();
        }

        private string GetProductSku(IDocument productPage)
        {
            var productSkuSelector = ".product--sku";
            return productPage.QuerySelector(productSkuSelector).TextContent.Trim();
        }

        private string GetProductModel(IDocument productPage)
        {
            var productSkuSelector = ".product--sku";
            return productPage.QuerySelector(productSkuSelector).TextContent.Trim();
        }

        private Task<IDocument> GetDepartmentListPage(IBrowsingContext context)
        {
            return Get(START_URL, context);
        }

        private int GetPageCount(Department department)
        {
            var document = department.Document;

            // this looks like "1 - 20 of 510" or "18 results found"
            var pageIndicator = document.QuerySelector(".result-count").TextContent;

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

    public struct Department
    {
        public string SubdepartmentName { get; set; }
        public IDocument Document { get; set; }
        public Uri Url { get { return new Uri(Document.Url); } }

        public Department(string subdepartmentName, IDocument department)
        {
            SubdepartmentName = subdepartmentName;
            Document = department;
        }
    }

    public struct Product
    {
        public Uri Url { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public string Model { get; set; }

        public Product(Uri url, string name, string sku, string model)
        {
            Url = url;
            Name = name;
            Sku = sku;
            Model = model;
        }
    }
}
