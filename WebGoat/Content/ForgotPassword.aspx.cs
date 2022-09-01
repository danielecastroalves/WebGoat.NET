using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Microsoft.Security.Application;
using OWASP.WebGoat.NET.App_Code;
using OWASP.WebGoat.NET.App_Code.DB;
using Encoder = OWASP.WebGoat.NET.App_Code.Encoder;

namespace OWASP.WebGoat.NET
{
    public partial class ForgotPassword : System.Web.UI.Page
    {
        private IDbProvider du = Settings.CurrentDbProvider;

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                PanelForgotPasswordStep2.Visible = false;
                PanelForgotPasswordStep3.Visible = false;
            }
        }

        protected void ButtonCheckEmail_Click(object sender, EventArgs e)
        {
            string[] result = du.GetSecurityQuestionAndAnswer(txtEmail.Text);

            if (string.IsNullOrEmpty(result[0]))
            {
                labelQuestion.Text = "That email address was not found in our database!";
                PanelForgotPasswordStep2.Visible = false;
                PanelForgotPasswordStep3.Visible = false;

                return;
            }
            labelQuestion.Text = Sanitizer.GetSafeHtmlFragment("Here is the question we have on file for you: <strong>" + result[0] + "</strong>");
            PanelForgotPasswordStep2.Visible = true;
            PanelForgotPasswordStep3.Visible = false;


            HttpCookie cookie = new HttpCookie("encr_sec_qu_ans");

            //encode twice for more security!

            cookie.Value = Encoder.Encode(Encoder.Encode(result[1]));

            Response.Cookies.Add(cookie);
        }

        protected void ButtonRecoverPassword_Click(object sender, EventArgs e)
        {
            try
            {
                //decode it (twice for extra security!)
                string security_answer = Encoder.Decode(Encoder.Decode(Request.Cookies["encr_sec_qu_ans"].Value));

                if (security_answer.Trim().ToLower().Equals(txtAnswer.Text.Trim().ToLower()))
                {
                    PanelForgotPasswordStep1.Visible = false;
                    PanelForgotPasswordStep2.Visible = false;
                    PanelForgotPasswordStep3.Visible = true;
                    labelPassword.Text = "Security Question Challenge Successfully Completed! <br/>Your password is: " + getPassword(txtEmail.Text);
                }
            }
            catch (Exception)
            {
                labelMessage.Text = "An unknown error occurred - Do you have cookies turned on? Further Details in logs ";
            }
        }

        string getPassword(string email)
        {
            return du.GetPasswordByEmail(email);
        }

    }
}