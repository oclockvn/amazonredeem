using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace amazonredeem
{
    public class WebRequestExtend : WebClient
    {
        public CookieContainer Cookies { get; set; }

        public WebRequestExtend() : base()
        {
            this.Cookies = new CookieContainer();
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address) as HttpWebRequest;

            if (request.Method == "POST")
            {
                request.ContentType = "application/x-www-form-urlencoded";
            }
            request.AutomaticDecompression = DecompressionMethods.GZip;
            request.UserAgent = "Mozilla/5.0 (Windows; U; Windows NT 6.1; en-US; rv:1.9.2.12) Gecko/20101026 Firefox/3.6.12";
            request.CookieContainer = this.Cookies;
            request.Timeout = 10000;

            return request;
        }
    }
}
