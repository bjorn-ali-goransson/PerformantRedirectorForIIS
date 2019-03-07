using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;

namespace PerformantRedirectorForIIS
{
    public class Redirector : IHttpModule
    {
        static IDictionary<string, string> Entries { get; set; }

        public void Init(HttpApplication context)
        {
            context.BeginRequest += Process;
        }

        void Process(object sender, EventArgs e)
        {
            if(Entries == null)
            {
                Entries = new Dictionary<string, string>();

                foreach (var line in File.ReadLines(HttpContext.Current.Server.MapPath("~/redirects.txt")))
                {
                    var key = RemoveProtocol(line.Substring(0, line.IndexOf('\t'))).ToLower().TrimEnd('/');

                    if (!Entries.ContainsKey(key))
                    {
                        Entries[key] = line.Substring(line.IndexOf('\t') + 1);
                    }
                }
            }

            var url = RemoveProtocol(HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Path).ToString()).ToLower().TrimEnd('/');
            var match = Entries.ContainsKey(url) ? Entries[url] : Entries.ContainsKey("*") ? Entries["*"] : null;

            if(HttpContext.Current.Request["debug-redirects"] == "1")
            {
                HttpContext.Current.Response.Write($"Your URL: {url}\n");

                if (match != null)
                {
                    if (Entries.ContainsKey(url))
                    {
                        HttpContext.Current.Response.Write($"Match: {match}\n");
                    }
                    else
                    {
                        HttpContext.Current.Response.Write($"Match: {match} (wildcard)\n");
                    }
                }
                else
                {
                    HttpContext.Current.Response.Write($"No match\n");
                }

                HttpContext.Current.Response.End();
            }

            if (match != null)
            {
                HttpContext.Current.Response.Redirect(match);
            }

            HttpContext.Current.Response.End();
        }

        string RemoveProtocol(string url)
        {
            if (url.StartsWith("https://"))
            {
                return url.Substring("https://".Length);
            }

            if (url.StartsWith("http://"))
            {
                return url.Substring("http://".Length);
            }

            return url;
        }

        public void Dispose() { }
    }
}