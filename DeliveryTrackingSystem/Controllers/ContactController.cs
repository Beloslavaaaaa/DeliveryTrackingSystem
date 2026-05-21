using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Threading.Tasks;

public class HomeController : Controller
{
    private readonly IConfiguration _config;

    public HomeController(IConfiguration config)
    {
        _config = config;
    }

    // This loads your view when a user goes to /Home/Contact
    [HttpGet]
    public IActionResult Contact()
    {
        return View();
    }

    // This catches the form submit action, sends the email, and goes back
    [HttpPost]
    public async Task<IActionResult> SendTransmission(string name, string email, string messageContent)
    {
        var smtpServer = _config["EmailSettings:SmtpServer"];
        var port = int.Parse(_config["EmailSettings:Port"]);
        var senderEmail = _config["EmailSettings:SenderEmail"];
        var senderPassword = _config["EmailSettings:SenderPassword"];
        var receiverEmail = _config["EmailSettings:ReceiverEmail"];

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Cargobell System", senderEmail));
        message.To.Add(new MailboxAddress("Global Concierge Admin", receiverEmail));
        message.Subject = $"NEW SECURE TRANSMISSION FROM: {name.ToUpper()}";

        message.Body = new TextPart("html")
        {
            Text = $@"
                <h3>New Contact Request Recieved via Portal</h3>
                <p><strong>Name:</strong> {name}</p>
                <p><strong>Reply Email:</strong> {email}</p>
                <br/>
                <p><strong>Secure Message Content:</strong></p>
                <p style='padding:10px; background:#f4f4f4; border-left:3px solid #D4AF37;'>{messageContent}</p>"
        };

        using (var client = new SmtpClient())
        {
            await client.ConnectAsync(smtpServer, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(senderEmail, senderPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }

        // Save the alert banner text into temporary cache memory
        TempData["SuccessMessage"] = "TRANSMISSION SUCCESSFUL";

        // FIXED: Clean redirection straight back to /Home/Contact URL
        return RedirectToAction("Contact", "Home");
    }
}