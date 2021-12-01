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

        private static string TextFromSelector(IDocument document, string selector)
        {
            if (document.QuerySelector(selector) == null) Console.WriteLine($"[{selector}] element not found on {document.Url}");
            else if (document.QuerySelector(selector)?.TextContent == null) Console.WriteLine($"No text found in [{selector}] element on {document.Url}");

            return document.QuerySelector(selector)?.TextContent?.Trim() ?? string.Empty;
        }

        
        public async Task<string> GetName()
        {
            var productNameSelector = ".product--title";
            return TextFromSelector(await GetDocument(), productNameSelector);
        }

        public async Task<string> GetSku()
        {
            var productSkuSelector = ".product--sku";
            var skuText = TextFromSelector(await GetDocument(), productSkuSelector);

            return Regex.Replace(skuText, "^SKU: ", "");
        }

        public async Task<string> GetModel()
        {
            var productModelSelector = ".product--model-number";
            var modelText = TextFromSelector(await GetDocument(), productModelSelector);

            return Regex.Replace(modelText, "^MODEL: ", "");
        }

        public async Task<decimal> GetPrice()
        {
            var dollarsSelector = ".product--price-dollars";
            var centsSelector = ".product--price-cents";
            var dollarsText = TextFromSelector(await GetDocument(), dollarsSelector);
            var centsText = TextFromSelector(await GetDocument(), centsSelector);

            if (Decimal.TryParse(dollarsText + centsText, out var result))
            {
                return result;
            }
            else
            {
                Console.WriteLine($"Could not generate price from string {dollarsText + centsText}");
                return Decimal.MaxValue;
            }
        }
    }
}