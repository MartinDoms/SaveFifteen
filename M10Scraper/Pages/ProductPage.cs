using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;

namespace SaveFifteen.M10Scraper.Pages
{
    internal class ProductPage : Page
    {
        public ProductPage(string url, IBrowsingContext context) : base(url, context)
        {
        }
    }
}