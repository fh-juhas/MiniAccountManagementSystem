using Microsoft.AspNetCore.Identity.UI.Services;
using System.Threading.Tasks;

namespace MiniAccountManagementSystem.Services
{
    public class EmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            // Do nothing - no-op implementation
            return Task.CompletedTask;
        }
    }
}