using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace LoanReviewAutomation
{
    public class EmailMessage
    {
        private string MessageSubject { get; set; } //MA: stores the email subject to be sent.
        private string MessageBody { get; set; } //MA: Stores the email message to be sent.
        private string SenderEmail { get; set; } //MA: Stores the email address of the sender.
        private string ReceiverEmail { get; set; } //MA: Stores the email address of the receiver.
        private Attachment file { get; set; } //DR: stores the attachment


        /// <summary>
        /// 
        /// </summary>
        /// <param name="SendToEmail"></param>
        /// <param name="Message"></param>
        public EmailMessage(string SendToEmail, string Subject, string MessageToSend)
        {
            SenderEmail = "LoanReviewTechSupport@bayviewloanservicing.com";
            ReceiverEmail = SendToEmail;
            MessageSubject = Subject;
            MessageBody = MessageToSend;
            SendEmail();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="SendFromEmail"></param>
        /// <param name="SendToEmail"></param>
        /// <param name="Message"></param>
        public EmailMessage(string SendFromEmail, string SendToEmail, string Subject, string MessageToSend)
        {
            SenderEmail = SendFromEmail;
            ReceiverEmail = SendToEmail;
            MessageSubject = Subject;
            MessageBody = MessageToSend;
            SendEmail();
        }

        public EmailMessage(string SendToEmail, string Subject, string MessageToSend, Attachment file)
        {
            SenderEmail = "LoanReviewTechSupport@bayviewloanservicing.com";
            ReceiverEmail = SendToEmail;
            MessageSubject = Subject;
            MessageBody = MessageToSend;
            this.file = file;
            SendEmailWithAttachment();
        }

        /// <summary>
        /// 
        /// </summary>        
        private void SendEmail()
        {
            SmtpClient client = new SmtpClient("smtplb.bftg.com");
            MailMessage mail = new MailMessage();
            try
            {
                mail.From = new MailAddress(SenderEmail);
                mail.Subject = MessageSubject;
                mail.To.Add(ReceiverEmail);
                mail.Body = MessageBody;
                client.Send(mail);
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                client.Dispose();
                mail.Dispose();
            }


        }

        private void SendEmailWithAttachment()
        {
            SmtpClient client = new SmtpClient("smtplb.bftg.com");
            MailMessage mail = new MailMessage();
            try
            {
                mail.From = new MailAddress(SenderEmail);
                mail.Subject = MessageSubject;
                mail.To.Add(ReceiverEmail);
                mail.Body = MessageBody;
                mail.Attachments.Add(file);
                client.Send(mail);
            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {
                client.Dispose();
                mail.Dispose();
            }


        }
    }
}
