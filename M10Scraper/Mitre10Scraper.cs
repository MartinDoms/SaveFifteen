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
            var topLevelDepartments = (await frontPage.GetAllDepartments()).Skip(4).Take(1);
            var departmentTasks = topLevelDepartments.Select(dpt => dpt.GetAllSubDepartmentPages());
            var departmentLists = await Task.WhenAll(departmentTasks);
            var allDepartments = departmentLists.SelectMany(departments => departments).Take(3);

            var productListPageTasks = allDepartments.Select(dpt => dpt.GetAllProductListPages()).ToList();
            var pageList = await Task.WhenAll(productListPageTasks);
            var productListPages = pageList.SelectMany(pageList => pageList).Take(2);

            var allProductPageTasks = productListPages.Select(listPage => listPage.GetAllProducts());
            var allProductPages = (await Task.WhenAll(allProductPageTasks)).SelectMany(prod => prod);

            var allProducts = await Task.WhenAll(allProductPages.Select(async p => new Product(new Uri(p.Url), await p.GetName(), await p.GetSku(), await p.GetModel(), await p.GetPrice())).ToList());
            Console.WriteLine($"{allProducts.Count()} producs found");

            //return allProductPages;
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
        public Uri Url { get; }
        public string Name { get; }
        public string Sku { get; }
        public string Model { get; }
        public decimal Price { get; }

        public Product(Uri url, string name, string sku, string model, decimal price)
        {
            Url = url;
            Name = name;
            Sku = sku;
            Model = model;
            Price = price;
        }
    }
}
