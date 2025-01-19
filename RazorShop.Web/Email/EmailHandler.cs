using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace RazorShop.Web.Email;

public class EmailHandler(IConfiguration config) : IEmailHandler
{
    public void SendEmail(Message message)
    {
        var emailMessage = CreateEmailMessage(message);

        Send(emailMessage);
    }

    public string CreateMessageBody(string templatePath, params string[] args)
    {
        var body = string.Empty;

        using (var SourceReader = File.OpenText(templatePath))
            body = SourceReader.ReadToEnd();

        int i = 0;
        foreach (string arg in args)
        {
            body = body.Replace("{" + i.ToString() + "}", arg);
            i++;
        }

        return body;
    }

    private MimeMessage CreateEmailMessage(Message message)
    {
        var fromName = config["Shop:Name"];
        var from = config["EmailProvider:User"];

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(fromName, from));
        emailMessage.To.AddRange(message.To);
        emailMessage.Subject = message.Subject;
        emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.Content };

        return emailMessage;
    }

    private void Send(MimeMessage mailMessage)
    {
        using (var client = new SmtpClient())
        {
            try
            {
                var host = config["EmailProvider:Host"];
                var port = config["EmailProvider:Port"];
                var user = config["EmailProvider:User"];
                var password = config["EmailProvider:Password"];

                client.Connect(host, int.Parse(port!), SecureSocketOptions.StartTls);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(user, password);

                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                //log.LogError($"Error sending email: {ex.Message}");
            }
            finally
            {
                client.Disconnect(true);
                client.Dispose();
            }
        }
    }
}