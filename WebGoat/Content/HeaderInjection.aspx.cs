using Microsoft.Security.Application;
using System;
using System.Collections;
using System.Collections.Specialized;
using System.Web;

namespace OWASP.WebGoat.NET
{
    public partial class HeaderInjection : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var cookieQuery = Encoder.UrlEncode(Request.QueryString["Cookie"]);
            var headerQuery = Encoder.UrlEncode(Request.QueryString["Header"]);

            if (cookieQuery != null)
            {
                HttpCookie cookie = new HttpCookie("UserAddedCookie");
                cookie.Value = cookieQuery;

                Response.Cookies.Add(cookie);
            }
            else if (headerQuery != null)
            {
                NameValueCollection newHeader = new NameValueCollection();
                newHeader.Add("newHeader", headerQuery);
                Response.Headers.Add(newHeader);
            }



            //Headers
            lblHeaders.Text = Sanitizer.GetSafeHtmlFragment(Request.Headers.ToString().Replace("&", "<br />"));

            //Cookies
            ArrayList colCookies = new ArrayList();
            for (int i = 0; i < Request.Cookies.Count; i++)
                colCookies.Add(Request.Cookies[i]);

            gvCookies.DataSource = colCookies;
            gvCookies.DataBind();

            //possibly going to be used later for something interesting

        }
    }
}