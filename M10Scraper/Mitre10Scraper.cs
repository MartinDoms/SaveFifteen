using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;

namespace SaveFifteen.M10Scraper
{
    public class Mitre10Scraper
    {
        private const string START_URL = "https://www.mitre10.co.nz/shop";
        private const string BASE_URL = "https://www.mitre10.co.nz";


        public async Task Scrape()
        {
            var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

            var startingPage = await GetDepartmentListPage(context);
            var allDepartments = await GetAllDepartments(startingPage, context);
            var allProductPages = await GetAllProducts(allDepartments, context);

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        private static int requestCount = 0;
        private Task<IDocument> Get(string url, IBrowsingContext context)
        {
            requestCount++;
            Console.WriteLine($"{requestCount} |\t {url}");
            return context.OpenAsync(url);
        }

        private async Task<IEnumerable<Product>> GetAllProducts(IEnumerable<Subdepartment> allDepartments, IBrowsingContext context)
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

        private async Task<IEnumerable<Subdepartment>> GetAllDepartments(IDocument startPage, IBrowsingContext context)
        {
            var departments = GetDepartmentUrls(startPage);
            var result = new List<Subdepartment>();
            foreach (var department in departments.Take(2)) 
            {
                result.AddRange(await GetSubDepartmentPages(department, context));
            }

            return result;
        }

        private async Task<IEnumerable<Subdepartment>> GetSubDepartmentPages(string departmentUrl, IBrowsingContext context)
        {
            var departmentPage = await Get(departmentUrl, context);
            var departmentTitle = GetTitle(departmentPage);
            var subDepartmentUrls = GetDepartmentUrls(departmentPage);

            var subdepartmentTasks = new List<Task<IDocument>>();
            foreach (var url in subDepartmentUrls)
            {
                subdepartmentTasks.Add(Get(url, context));
            }
            var completedTasks = await Task.WhenAll(subdepartmentTasks);

            var result = new List<Subdepartment>();
            foreach (var subdepartmentPage in completedTasks)
            {
                var subdepartmentName = GetTitle(subdepartmentPage);
                var subdepartment = new Subdepartment(subdepartmentName, subdepartmentPage);
                Console.WriteLine($"Found department {departmentTitle} -> {subdepartmentName} | {subdepartment.Url}");
                result.Add(subdepartment);
            }

            return result;
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

        private string GetTitle(IDocument page)
        {
            return Regex.Replace(page.Title, "\\s*\\|.+", "");
        }

        private Task<IDocument> GetDepartmentListPage(IBrowsingContext context)
        {
            return Get(START_URL, context);
        }

        private IEnumerable<string> GetDepartmentUrls(IDocument page)
        {
            var linkSelector = "#department-count-component li a";
            // selector for department links from shop and department screens "#department-count-component li a"
            var links = page.QuerySelectorAll(linkSelector);

            foreach (var link in links)
            {
                var href = link.Attributes["href"].Value;
                if (Regex.Match(href, "^/shop").Success)
                {
                    yield return BASE_URL + link.Attributes["href"].Value;
                }
            }
        }

        private int GetPageCount(Subdepartment department)
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

    struct Subdepartment
    {
        public string SubdepartmentName { get; set; }
        public IDocument Document { get; set; }
        public Uri Url { get { return new Uri(Document.Url); } }

        public Subdepartment(string subdepartmentName, IDocument department)
        {
            SubdepartmentName = subdepartmentName;
            Document = department;
        }
    }

    struct Product
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
