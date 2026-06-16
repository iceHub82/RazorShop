using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace RazorShop.Web.Email;

public class EmailHandler(IConfiguration config, ILogger logger)
{
    public void SendEmail(Message message)
    {
        var host = config["EmailProvider:Host"];
        var portRaw = config["EmailProvider:Port"];
        var user = config["EmailProvider:User"];
        var password = config["EmailProvider:Password"];

        if (string.IsNullOrEmpty(host)
            || !int.TryParse(portRaw, out var port)
            || string.IsNullOrEmpty(user)
            || string.IsNullOrEmpty(password))
        {
            logger.LogError("EmailProvider settings missing; skipping send to {Recipients}", string.Join(",", message.To));
            return;
        }

        var emailMessage = CreateEmailMessage(message, user);

        Send(emailMessage, host, port, user, password);
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

    private MimeMessage CreateEmailMessage(Message message, string fromAddress)
    {
        var fromName = config["Shop:Name"] ?? string.Empty;

        var emailMessage = new MimeMessage();
        emailMessage.From.Add(new MailboxAddress(fromName, fromAddress));
        emailMessage.To.AddRange(message.To);
        emailMessage.Subject = message.Subject;
        emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html) { Text = message.Content };

        return emailMessage;
    }

    private void Send(MimeMessage mailMessage, string host, int port, string user, string password)
    {
        using (var client = new SmtpClient())
        {
            try
            {
                client.Connect(host, port, SecureSocketOptions.StartTls);
                client.AuthenticationMechanisms.Remove("XOAUTH2");
                client.Authenticate(user, password);

                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send email to {Recipients}", string.Join(",", mailMessage.To));
            }
            finally
            {
                client.Disconnect(true);
                client.Dispose();
            }
        }
    }
}