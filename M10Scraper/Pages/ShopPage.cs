using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;

namespace SaveFifteen.M10Scraper.Pages
{
    internal class ShopPage : Page
    {

        public ShopPage(string url, IBrowsingContext context) : base(url, context)
        {
        }

        public async Task<IEnumerable<DepartmentPage>> GetAllDepartments()
        {
            var departmentUrls = await GetDepartmentUrls();
            return departmentUrls.Select(url => new DepartmentPage(url, Context));
        }

        private string GetTitle(IDocument page)
        {
            return Regex.Replace(page.Title, "\\s*\\|.+", "");
        }

        /*private async Task<IEnumerable<DepartmentPage>> GetSubDepartmentPages(string departmentUrl, IBrowsingContext context)
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

            var result = new List<DepartmentPage>();
            foreach (var subdepartmentPage in completedTasks)
            {
                var subdepartmentName = GetTitle(subdepartmentPage);
                var subdepartment = new Department(subdepartmentName, subdepartmentPage);
                Console.WriteLine($"Found department {departmentTitle} -> {subdepartmentName} | {subdepartment.Url}");
                result.Add(subdepartment);
            }

            return result;
        }*/

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
    }
}