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
            if (Request.QueryString["Cookie"] != null)
            {
                HttpCookie httpCookie = new HttpCookie("UserAddedCookie");

                httpCookie.Value = Encoder.JavaScriptEncode(Request.QueryString["Cookie"]);

                Response.Cookies.Add(httpCookie);
            }
            else if (Request.QueryString["Header"] != null)
            {
                NameValueCollection newHeader = new NameValueCollection();
                newHeader.Add("newHeader", Encoder.JavaScriptEncode(Request.QueryString["Header"]));
                Response.Headers.Add(newHeader);
            }



            //Headers
            lblHeaders.Text = Request.Headers.ToString().Replace("&", "<br />");

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