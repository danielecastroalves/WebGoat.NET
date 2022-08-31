using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.IO;
using Microsoft.Security.Application;

namespace OWASP.WebGoat.NET
{
    public partial class PathManipulation : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {	
			//if(Request.QueryString["filename"] == null)
        	//{
				DirectoryInfo di = new DirectoryInfo(Server.MapPath("~/Downloads"));
	        	int i = 0;
	        	
	        	foreach(FileInfo fi in di.GetFiles())
	        	{
	            	HyperLink HL = new HyperLink();
	            	HL.ID = "HyperLink" + i++;
	            	HL.Text = HttpUtility.HtmlEncode(fi.Name);
	            	HL.NavigateUrl = Request.FilePath + "?filename=" + HL.Text;
	            	ContentPlaceHolder cph = (ContentPlaceHolder)this.Master.FindControl("BodyContentPlaceholder");
	            	cph.Controls.Add(HL);
	            	cph.Controls.Add(new LiteralControl("<br/>"));
	        	}
        	//}
        	//else
        	//{
        		string filename = Encoder.UrlEncode(Request.QueryString["filename"]);
        		if(filename != null)
        		{
                    try
                    {
                        ResponseFile(Request, Response, filename, MapPath("~/Downloads/" + filename), 100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        lblStatus.Text = "File not found: " + filename;   
                    }
                }
        	//}
        }
        
        public static bool ResponseFile(HttpRequest _Request, HttpResponse _Response, string _fileName, string _fullPath, long _speed)
    	{
	        try
	        {
	            FileStream myFile =	new FileStream(_fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
	            BinaryReader br = new BinaryReader(myFile);
	            try
	            {
	          
	                _Response.AddHeader("Accept-Ranges", "bytes");
	                _Response.Buffer = false;
	                long fileLength = myFile.Length;
	                long startBytes = 0;
			
	                int pack = 10240; //10K bytes
	                if (_Request.Headers["Range"] != null)
	                {
	                    _Response.StatusCode = 206;
	                    string[] range = _Request.Headers["Range"].Split(new char[] { '=', '-' });
	                    startBytes = Convert.ToInt64(range[1]);
	                }
	                _Response.AddHeader("Content-Length", (fileLength - startBytes).ToString());
	        
	                if (startBytes != 0)
	                {
	                    _Response.AddHeader("Content-Range", string.Format(" bytes {0}-{1}/{2}", startBytes, fileLength - 1, fileLength));
	                }
	        
	                _Response.AddHeader("Connection", "Keep-Alive");
	                _Response.ContentType = "application/octet-stream";
	                _Response.AddHeader("Content-Disposition", "attachment;filename=" + HttpUtility.UrlEncode(_fileName, System.Text.Encoding.UTF8));
			
	                br.BaseStream.Seek(startBytes, SeekOrigin.Begin);
	                int maxCount = (int)Math.Floor((double)((fileLength - startBytes) / pack)) + 1;
					
	                for (int i = 0; i < maxCount; i++)
	                {
	                    if (_Response.IsClientConnected)
	                    {
	                        _Response.BinaryWrite(HttpUtility.HtmlEncode(br.ReadBytes(pack)));
	                    }
	                    else
	                    {
	                        i = maxCount;
	                    }
	                }
				}
	            catch(Exception ex)
	            {
	            	Console.WriteLine(ex.Message);
	                return false;
	            }
	            finally
	            {
	                br.Close();
	                myFile.Close();
	            }
	        }
	        catch(Exception ex)
	        {
	        	Console.WriteLine(ex.Message);
	            return false;
	        }
	        return true;
    	}
        
    }
}