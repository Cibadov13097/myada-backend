using NotificationService.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NotificationService.Application.Services
{
    public class NotificationService : INotificationService
    {
        public Task SendEmailAsync(string email, string message)
        {
            throw new NotImplementedException();
        }
    }
}
