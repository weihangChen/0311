using System;
namespace TicketManagementSystem
{
    public class TicketWithAccountManager:Ticket
    {
        public User AccountManager { get; set; }
        public double PriceDollars { get; set; }
    }
}
