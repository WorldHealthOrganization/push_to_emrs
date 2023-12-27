using System;
using System.Net;
using System.Net.Mail;
using DataAccess;
using System.Configuration;
using System.Text;
using System.IO;

namespace PushToEMRS
{
    
    public class SendEmail 
    {
        private string smtpServer;
        private bool useCredentials;
        private string smtpUserName;
        private string smtpPassword;
        private readonly IOperations operations;
        private readonly CommonFunctions commonfunction;
       
        public SendEmail()
        {
            this.operations = new Operations();
            commonfunction = new CommonFunctions();
            this.smtpServer = GetSMTPInfo("SMTPServer");
            var SMTPUseCredentialsSetting = GetSMTPInfo("smtp_use_credentials");
            this.useCredentials = !string.IsNullOrEmpty(SMTPUseCredentialsSetting) && SMTPUseCredentialsSetting == "1";
            this.smtpUserName = useCredentials ? GetSMTPInfo("smtp_user_name") : string.Empty;
            this.smtpPassword = useCredentials ? GetSMTPInfo("smtp_password") : string.Empty;
            

        }

        public void SendMail(string smtpDomain )
        {
            bool isSuccess = isErrorOccured();
            string from = ConfigurationManager.AppSettings.Get("FromEmailAddress");                    
            string recipients = ConfigurationManager.AppSettings.Get("ToEmailAddress");
            string subject = isSuccess ? "EMRS - EMS2 Datasync Success" : "EMRS - EMS2 Datasync Failed";
            string  ccList = ConfigurationManager.AppSettings.Get("ccList");
            string body =BuildEmailtemplate(isSuccess);
            bool isdebug = Convert.ToBoolean(ConfigurationManager.AppSettings.Get("DebugMode"));

            if (!isdebug) //false will send emails else will write to a file
            {
                var msg = CreateMail(from, recipients, subject, body);
                SendRegularEmail(msg, smtpDomain);
            }
            else
             {
                WriteSampleEmail(from, recipients, subject, body);

            }
           
        }
        private bool isErrorOccured()
        {
            bool isSuccess =false;
            var commandText = string.Format("select isnull(count(*),0) from tbl_emrs_logging where CAST(CreatedDate AS date) =CAST(getdate() AS date)");
            try
            {
                var result = operations.Read(commandText, ConfigurationManager.AppSettings.Get("vShocDbConnection"));
                if (result != null)
                {
                    var cnt = Convert.ToInt32(result.Rows[0][0]);
                    if (cnt > 0)
                        isSuccess = true;
                }

            }
            catch (Exception ex)
            {
                commonfunction.InsertErrorCode(0, null, null, 0, "ErrorCount", null, null, ex.Message, ex.StackTrace);
                //log.Error("SMTP Info unable to be retrieved from database.", e);
                
            }
            return isSuccess;
        }
        private MailMessage CreateMail(
            string from,
            string recipients,
            string subject,
            string body,
            MailAddressCollection ccList = null,
            MailAddressCollection bccList = null)
        {
            var msg = new MailMessage();
            var HTMLType = new System.Net.Mime.ContentType("text/html");
            var HTMLView = AlternateView.CreateAlternateViewFromString(body, HTMLType);
            msg.AlternateViews.Add(HTMLView);
            // Address the message
            msg.From = new MailAddress(from);
            msg.To.Add(recipients);
            ccList = ccList ?? new MailAddressCollection();
            bccList = bccList ?? new MailAddressCollection();
            foreach (MailAddress cc in ccList)
            {
                msg.CC.Add(cc);
            }
            foreach (MailAddress bcc in bccList)
            {
                msg.Bcc.Add(bcc);
            }
            msg.Subject = subject;
            return msg;
        }

        private void SendRegularEmail(MailMessage message, string smtpDomain)
        {
            using (var smtp = new SmtpClient(this.smtpServer))
            {
                if (this.useCredentials)
                {
                    smtp.Credentials = !string.IsNullOrEmpty(smtpDomain) ?
                        new NetworkCredential(this.smtpUserName, this.smtpPassword, smtpDomain) :
                        new NetworkCredential(this.smtpUserName, this.smtpPassword);
                }
                smtp.Send(message);
            };
        }

        private string GetSMTPInfo(string value)
        {
            var commandText = string.Format("select value from WebEOCINI where name = '{0}'", value);
            var smtpval = string.Empty;
            try
            {
              var  result = operations.Read(commandText, ConfigurationManager.AppSettings.Get("vShocDbConnection"));
                if( result!= null) 
                smtpval = result.Rows[0][0].ToString();
            }
            catch (Exception ex)
            {
                commonfunction.InsertErrorCode(0, null, null, 0, "WebEOCINI-SMTP", null, null, ex.Message, ex.StackTrace);
                //log.Error("SMTP Info unable to be retrieved from database.", e);
                return smtpval;
            }

            return smtpval;
        }

        private static void WriteSampleEmail(string fromAddress, string toAddress,string subject, string body )
        {
            string filePath = ConfigurationManager.AppSettings.Get("DebugFileLocation");
            var sb = new StringBuilder();
            sb.AppendLine("<b>Beginning of e-mail</b><br/>")
                .AppendFormat("<b>Date</b>: {0}<br/>", DateTime.Now)
                .AppendFormat("<b>From</b>: {0}<br/>", fromAddress)
                .AppendFormat("<b>To</b>: {0}<br/>", toAddress)
                .AppendFormat("<b>Subject</b>: {0}<br/>", subject)
                .AppendFormat("<b>Body:</b><br/>{0}<br/>", body)
                .AppendLine("<b>End of e-mail</b><br/>")
                .AppendLine("<hr>");

            string path = $@"{filePath}\{subject}.html";
            // This text is added only once to the file.
            if (!File.Exists(path))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(sb.ToString());
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine(sb.ToString());
                }
            }
        }

        private string BuildEmailtemplate(bool isSuccess)
        {
            var sb = new StringBuilder();
            if (isSuccess)
            {
                sb.AppendLine("<b>Data synchronization between vSHOC and EMRS is successfull </b><br/>")
                   .AppendFormat("<b>Please check errors if any in  tbl_emrs_logging </b>")
                   .AppendFormat("Regards,<b> vshoc Team </b>")
                   .AppendLine("<hr>");
            }
            else
            {

                sb.AppendLine("<b>Data synchronization between vSHOC and EMRS failed</b><br/>")
                    .AppendFormat("<b>Please check error in  tbl_emrs_logging </b>")
                    .AppendFormat("Regards,<b> vshoc Team </b>")
                    .AppendLine("<hr>");

            }

            return sb.ToString();
        }
    }
}
