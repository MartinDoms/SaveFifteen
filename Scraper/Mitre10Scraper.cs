using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;

namespace Scraper
{
    public class Mitre10Scraper
    {
        private const string START_URL = "https://www.mitre10.co.nz/shop";
        private const string BASE_URL = "https://www.mitre10.co.nz";


        public async Task Scrape()
        {
            var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());

            var startingPage = await GetDepartmentPage(context);
            var allDepartments = await GetAllDepartmentPages(startingPage, context);

            Task.WaitAll(allDepartments.ToArray());

            Console.WriteLine("Done!");
            Console.ReadLine();
        }

        private async Task<List<Task<Subdepartment>>> GetAllDepartmentPages(IDocument startPage, IBrowsingContext context)
        {
            var departments = GetDepartmentUrls(startPage);

            departments.Select(d => GetSubDepartmentPageTasks(d, context));
        }

        private async Task<List<Task<Subdepartment>>> GetSubDepartmentPageTasks(string departmentUrl, IBrowsingContext context)
        {
            var departmentPage = await context.OpenAsync(departmentUrl);
            var departmentTitle = GetTitle(departmentPage);
            var subDepartmentUrls = GetDepartmentUrls(departmentPage);

            var result = subDepartmentUrls.Select(async url =>
            {
                var subdepartmentPage = await context.OpenAsync(url);
                var result = new Subdepartment(departmentTitle, subdepartmentPage);
                Console.WriteLine($"Found department {result.ParentDepartment} -> {GetTitle(result.Department)}");
                return result;
            }).ToList();

            return result;
        }

        private string GetTitle(IDocument page)
        {
            return Regex.Replace(page.Title, "\\s*\\|.+", "");
        }

        private Task<IDocument> GetDepartmentPage(IBrowsingContext context)
        {
            return context.OpenAsync(START_URL);
        }

        private IEnumerable<string> GetDepartmentUrls(IDocument page)
        {
            var linkSelector = "#department-count-component li a";
            // selector for department links from shop and department screens "#department-count-component li a"
            var links = page.QuerySelectorAll(linkSelector);

            foreach (var link in links)
            {
                yield return BASE_URL + link.Attributes["href"].Value;
            }
        }
    }

    struct Subdepartment
    {
        public string ParentDepartment { get; set; }
        public IDocument Department { get; set; }

        public Subdepartment(string parentDepartment, IDocument department)
        {
            ParentDepartment = parentDepartment;
            Department = department;
        }
    }
}
