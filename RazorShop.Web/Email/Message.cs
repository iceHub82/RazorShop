using MimeKit;

namespace RazorShop.Web.Email;

public class Message(IEnumerable<string> to, string subject, string content)
{
    public List<MailboxAddress> To { get; set; } = [.. to.Select(x => new MailboxAddress("Customer", x))];
    public string Subject { get; set; } = subject;
    public string Content { get; set; } = content;
}