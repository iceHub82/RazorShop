using MimeKit;
using MailKit.Net.Smtp;

namespace RazorShop.Web.Email;

public class EmailHandler(IConfiguration config) : IEmailHandler
{
    //private readonly EmailConfig _config = config;

    public void SendEmail(Message message)
    {
        var emailMessage = CreateEmailMessage(message);

        Send(emailMessage);
    }

    public string CreateMessageBody(string templatePath, params string[] args)
    {
        var body = string.Empty;

        // Read in the template
        using (var SourceReader = File.OpenText(templatePath))
            body = SourceReader.ReadToEnd();

        // Now add the data
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
        var fromName = config["Email:FromName"];
        var from = config["Email:From"];

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
                var host = config["Email:Host"];
                var port = config["Email:Port"];
                var user = config["Email:User"];
                var password = config["Email:Password"];

                client.Connect(host, int.Parse(port!), true);
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