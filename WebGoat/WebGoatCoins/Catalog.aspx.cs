using OWASP.WebGoat.NET.App_Code;
using OWASP.WebGoat.NET.App_Code.DB;
using System;
using System.Data;

namespace OWASP.WebGoat.NET.WebGoatCoins
{
    public partial class Catalog : System.Web.UI.Page
    {
        private IDbProvider du = Settings.CurrentDbProvider;
        
        protected void Page_Load(object sender, EventArgs e)
        {
            DataSet ds = du.GetProductsAndCategories();

            foreach (DataRow catRow in ds.Tables["categories"].Rows)
            {
                lblOutput.Text += "<p/><h2 class='title-regular-2 clearfix'>Category: " + Server.HtmlEncode(catRow["catName"].ToString()) + "</h2><hr/><p/>\n";
                foreach (DataRow prodRow in catRow.GetChildRows("cat_prods"))
                {
                    lblOutput.Text += "<div class='product' align='center'>\n";
                    lblOutput.Text += "<img src='./images/products/" + Server.HtmlEncode(prodRow[3].ToString()) + "'/><br/>\n";
                    lblOutput.Text += "" + Server.HtmlEncode(prodRow[1].ToString()) + "<br/>\n";
                    lblOutput.Text += "<a href=\"ProductDetails.aspx?productNumber=" + Server.HtmlEncode(prodRow[0].ToString()) + "\"><br/>\n";
                    lblOutput.Text += "<img src=\"../resources/images/moreinfo1.png\" onmouseover=\"this.src='../resources/images/moreinfo2.png';\" onmouseout=\"this.src='../resources/images/moreinfo1.png';\" />\n";
                    lblOutput.Text += "</a>\n";
                    lblOutput.Text += "</div>\n";
                }
            }
            

            /*
            foreach(DataRow row in ds.Tables["products"].Rows)
            {
                lblOutput.Text += row[1] + "<br/>";
                lblOutput.Text += "<img src='./images/products/" + row[3] + "'/><br/>";

            }
            */
            /*
                foreach (DataRow custRow in customerOrders.Tables["Customers"].Rows)
                {
                    Console.WriteLine(custRow["CustomerID"].ToString());
                    foreach (DataRow orderRow in custRow.GetChildRows(customerOrdersRelation))
                    {
                        Console.WriteLine(orderRow["OrderID"].ToString());
                    }
                }
            */
        }
    }
}