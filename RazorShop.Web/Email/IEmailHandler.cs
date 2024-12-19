namespace RazorShop.Web.Email;

public interface IEmailHandler
{
    void SendEmail(Message message);
}