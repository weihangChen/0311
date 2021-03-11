using System;
namespace TicketManagementSystem
{
    public class Ticket
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public Priority Priority { get; set; }

        public string Description { get; set; }

        public User AssignedUser { get; set; }

        public DateTime Created { get; set; }
    }
}
