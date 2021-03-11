using System;
using System.Collections.Generic;
using EmailService;
using System.Linq;
using System.Threading.Tasks;

namespace TicketManagementSystem
{
    /*
     * Notes of updates
     * 1. move files into different folders
     * 2. input parameters rename
     * 3. create subclass of Ticket becuase not all ticket has AccountManager and price
     * 4. UserRepository init in constructor, no need to wrap with using, as sqlconnection is disposed internally
     * 5. Remove as many "if" as possible by using readonly dictionary or list
     * 6. only send email if ticket is created successfully and run it in new thread
     * 7. add mock library and unit test cases
     * 
     */


    public class TicketService
    {
        private IUserRepository _userRepo;
        private IEmailService _emailService;
        public TicketService()
        {
            _userRepo = new UserRepository();
            _emailService = new EmailServiceProxy();
        }

        public TicketService(IUserRepository userRepo, IEmailService emailService)
        {
            _userRepo = userRepo;
            _emailService = emailService;
        }

        /// <summary>create ticket for both paid and normal customer</summary>
        /// <param name="title">title of the ticket.</param>
        /// <param name="priority">priority of the ticket</param>
        /// <param name="assignedTo">responsible person of the ticket</param>
        /// <param name="desc">description of the ticket.</param>
        /// <param name="incidentCreatedDate">datetime when ticket is created.</param>
        /// <param name="isPayingCustomer">ticket is created by a paying customer. </param>
        /// <returns>id of the created tiacket.</returns>
        public int CreateTicket(string title, Priority priority, string assignedTo,
                                string desc, DateTime incidentCreatedDate, bool isPayingCustomer)
        {
            // Check if t or desc are null or if they are invalid and throw exception
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(desc) || string.IsNullOrEmpty(assignedTo))
            {
                throw new InvalidTicketException("Title or description or assignedTo were null");
            }
            User user = _userRepo.GetUser(assignedTo);
            if (user == null)
            {
                throw new UnknownUserException("User " + assignedTo + " not found");
            }

            priority = GetUpdatedPriority(priority, incidentCreatedDate, title);
            Ticket ticket = isPayingCustomer ?
                GetPaidTicket(title, priority, user, desc, incidentCreatedDate) :
                GetNormalTicket(title, priority, user, desc, incidentCreatedDate);
            var id = TicketRepository.CreateTicket(ticket);
            if (priority == Priority.High)
            {
                Task.Run(() => _emailService.SendEmailToAdministrator(title, assignedTo));
            }

            // Return the id
            return id;
        }

        /// <summary>assign another user as responsbiel to the ticket.</summary>
        /// <param name="ticketId">existing ticket id.</param>
        /// <param name="newUserName">the new person that is responsbile for the ticket.</param>
        public void AssignTicket(int ticketId, string newUserName)
        {
            if (ticketId <= 0 || string.IsNullOrEmpty(newUserName))
            {
                throw new ArgumentException("invalid ticket id or username");
            }

            var ticket = TicketRepository.GetTicket(ticketId);
            if (ticket == null)
            {
                throw new ApplicationException("No ticket found for id " + ticketId);
            }

            // if it is same user, no need to update.
            if (ticket.AssignedUser.Username.Equals(newUserName))
            {
                return;
            }

            User user = _userRepo.GetUser(newUserName);
            if (user == null)
            {
                throw new UnknownUserException("User not found");
            }

            ticket.AssignedUser = user;
            TicketRepository.UpdateTicket(ticket);
        }


        /// <summary>raise priority if ticket is created _maxHour ago or contains urgen keywords.</summary>
        /// <param name="priority">priority of the ticket.</param>
        /// <param name="incidentCreatedDate">datetime when ticket is created.</param>
        /// <param name="title">title of the ticket.</param>
        private Priority GetUpdatedPriority(Priority priority, DateTime incidentCreatedDate, string title)
        {
            bool incidentOccursNHourAgo = incidentCreatedDate < (DateTime.UtcNow - TimeSpan.FromHours(1));
            IReadOnlyList<string> urgencyKeywords = new List<string> { "Crash", "Important", "Failure" };
            bool titleContainsUrgencyKeyword = urgencyKeywords.Any(x => title.Contains(x)); ;
            if (incidentOccursNHourAgo || titleContainsUrgencyKeyword)
            {
                IReadOnlyDictionary<Priority, Priority> priorityRaisingDic = new Dictionary<Priority, Priority>
                {
                    { Priority.Low, Priority.Medium},
                    { Priority.Medium, Priority.High},
                    { Priority.High, Priority.High}
                };

                return priorityRaisingDic[priority];
            }
            return priority;
        }


        private Ticket GetNormalTicket(string title, Priority priority, User assignedTo, string desc, DateTime created)
        {
            return new Ticket()
            {
                Title = title,
                AssignedUser = assignedTo,
                Priority = priority,
                Description = desc,
                Created = created,
            };
        }


        private TicketWithAccountManager GetPaidTicket(string title, Priority priority, User assignedTo, string desc, DateTime created)
        {
            // Only paid customers have an account manager.
            User accountManager = _userRepo.GetAccountManager();
            if (accountManager == null)
            {
                throw new UnknownUserException("account manager not found");
            }

            IReadOnlyDictionary<Priority, int> priceDic = new Dictionary<Priority, int> {
                { Priority.High, 100 },
                { Priority.Medium, 50 },
                { Priority.Low, 50 }
            };

            return new TicketWithAccountManager()
            {
                Title = title,
                AssignedUser = assignedTo,
                Priority = priority,
                Description = desc,
                Created = created,
                PriceDollars = priceDic[priority],
                AccountManager = accountManager
            };
        }
    }
}
