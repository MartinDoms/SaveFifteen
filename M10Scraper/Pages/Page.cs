using AngleSharp;
using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Linq;

namespace SaveFifteen.M10Scraper.Pages
{
    internal abstract class Page
    {
        internal const string BASE_URL = "https://www.mitre10.co.nz";

        public string Url { get; private set; }
        public IBrowsingContext Context { get; private set; }
        private IDocument _document;
        public async Task<IDocument> GetDocument()
        {
            await Fetch();
            return _document;
        } 
        

        private static int fetches = 0;
        internal async Task<IDocument> Fetch(Boolean force = false)
        {
            if (_document == null || force)
            {
                Console.WriteLine($"{fetches++}\t| {Url}");
                _document = await Context.OpenAsync(Url);
            }
            return _document;
        }

        public Page(string url, IBrowsingContext context)
        {
            Url = url;
            Context = context;
        }
    }
}