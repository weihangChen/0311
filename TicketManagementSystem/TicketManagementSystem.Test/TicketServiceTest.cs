using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Moq;
using EmailService;

namespace TicketManagementSystem.Test
{
    [TestClass]
    public class TicketServiceTest
    {
        [TestMethod]
        public void RaiseExceptionIfInputIsInValid()
        {
            var service = new TicketService();
            try
            {
                service.CreateTicket(
                    "",
                    Priority.Medium,
                    "Johan",
                    "The system crashed when user performed a search",
                    DateTime.UtcNow,
                    true);
                Assert.Fail("should have raised InvalidTicketException");
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidTicketException);
            }


            try
            {
                service.CreateTicket(
                    "System Crash",
                    Priority.Medium,
                    "",
                    "The system crashed when user performed a search",
                    DateTime.UtcNow,
                    true);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidTicketException);
            }

            try
            {
                service.CreateTicket(
                            "System Crash",
                            Priority.Medium,
                            "Johan",
                            "",
                            DateTime.UtcNow,
                            true);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e is InvalidTicketException);
            }
        }


        [TestMethod]
        [ExpectedException(typeof(UnknownUserException))]
        public void RaiseExceptionIfUserIsNotFound()
        {
            var service = new TicketService();

            var ticketId = service.CreateTicket(
                "System Crash",
                Priority.Medium,
                "Invalid Name",
                "The system crashed when user performed a search",
                DateTime.UtcNow,
                true);
        }


        [TestMethod]
        public void CreateNormalTicket()
        {
            var userRepoMock = new Mock<IUserRepository>();
            var userName = "Johan C";
            userRepoMock.Setup(x => x.GetUser(userName)).Returns(new User { Username = userName, FirstName = "Johan", LastName = "C" });
            var service = new TicketService(userRepoMock.Object, new EmailServiceProxy());

            var title = "test";
            var priority = Priority.Medium;
            var desp = "The system crashed when user performed a search";
            var createDate = DateTime.UtcNow;

            var ticketId = service.CreateTicket(title, priority, userName, desp, createDate, false);
            Assert.IsTrue(ticketId != 0);
            var ticket = TicketRepository.GetTicket(ticketId);
            Assert.IsTrue(ticket.Title == title);
            Assert.IsTrue(ticket.Priority == priority);
            Assert.IsTrue(ticket.AssignedUser.Username == userName);
            Assert.IsTrue(ticket.Description == desp);
            Assert.IsTrue(ticket.Created == createDate);

            
        }


        [TestMethod]
        public void CreatePayingTicketShouldVerifyAccountManagerAndPrice()
        {
            var userRepoMock = new Mock<IUserRepository>();
            var userName = "Johan C";
            var accountManager = "Sara L";
            userRepoMock.Setup(x => x.GetUser(userName)).Returns(new User { Username = userName, FirstName = "Johan", LastName = "C" });
            userRepoMock.Setup(x => x.GetAccountManager()).Returns(new User { Username = accountManager, FirstName = "Sara", LastName = "L" });

            var service = new TicketService(userRepoMock.Object, new EmailServiceProxy());

            var title = "test";
            var priority = Priority.Medium;
            var desp = "The system crashed when user performed a search";
            var createDate = DateTime.UtcNow;

            var ticketId = service.CreateTicket(title, priority, userName, desp, createDate, true);
            Assert.IsTrue(ticketId != 0);
            TicketWithAccountManager ticket = (TicketWithAccountManager)TicketRepository.GetTicket(ticketId);
            Assert.IsTrue(ticket.Title == title);
            Assert.IsTrue(ticket.Priority == priority);
            Assert.IsTrue(ticket.AssignedUser.Username == userName);
            Assert.IsTrue(ticket.Description == desp);
            Assert.IsTrue(ticket.Created == createDate);
            Assert.IsTrue(ticket.AccountManager.Username == accountManager);
            Assert.IsTrue(ticket.PriceDollars == 50);
            // verify priority high should give price of 100
            var ticketId1 = service.CreateTicket(title, Priority.High, userName, desp, createDate, true);
            TicketWithAccountManager ticket1 = (TicketWithAccountManager)TicketRepository.GetTicket(ticketId1);
            Assert.IsTrue(ticket1.PriceDollars == 100);
            // verify priority high should give price of 50
            var ticketId2 = service.CreateTicket(title, Priority.Low, userName, desp, createDate, true);
            TicketWithAccountManager ticket2 = (TicketWithAccountManager)TicketRepository.GetTicket(ticketId2);
            Assert.IsTrue(ticket2.PriceDollars == 50);

        }



        [TestMethod]
        public void TicketPriorityUpdateCheck()
        {
            var userRepoMock = new Mock<IUserRepository>();
            userRepoMock.Setup(x => x.GetUser("Johan")).Returns(new User { Username = "Johan C", FirstName = "Johan", LastName = "C" });
            var service = new TicketService(userRepoMock.Object, new EmailServiceProxy());
            // should not raise priority
            var ticketId1 = service.CreateTicket(
                "test",
                Priority.Medium,
                "Johan",
                "The system crashed when user performed a search",
                DateTime.UtcNow,
                false);
            var ticket1 = TicketRepository.GetTicket(ticketId1);
            Assert.IsTrue(ticket1.Priority == Priority.Medium);
            // priority increase one step because "Crash" is in title
            var ticketId2 = service.CreateTicket(
                "Crash",
                Priority.Low,
                "Johan",
                "The system crashed when user performed a search",
                DateTime.UtcNow,
                false);
            var ticket2 = TicketRepository.GetTicket(ticketId2);
            Assert.IsTrue(ticket1.Priority == Priority.Medium);

            // priority increase one step because incident happens more than one hour ago
            var ticketId3 = service.CreateTicket(
                "test",
                Priority.Low,
                "Johan",
                "The system crashed when user performed a search",
                DateTime.UtcNow.AddHours(-1.1),
                false);
            var ticket3 = TicketRepository.GetTicket(ticketId3);
            Assert.IsTrue(ticket1.Priority == Priority.Medium);

            // priority should increase one step even it is older than one hour and title contains "Crash"
            var ticketId4 = service.CreateTicket(
                "Crash",
                Priority.Low,
                "Johan",
                "The system crashed when user performed a search",
                DateTime.UtcNow.AddHours(-1.1),
                false);
            var ticket4 = TicketRepository.GetTicket(ticketId4);
            Assert.IsTrue(ticket1.Priority == Priority.Medium);


        }

        [TestMethod]
        public void AssignTicketShouldUpdateUser()
        {
            var userRepoMock = new Mock<IUserRepository>();
            var userName = "Johan C";
            var newUserName = "Marc C";
            userRepoMock.Setup(x => x.GetUser(userName)).Returns(new User { Username = userName, FirstName = "Johan", LastName = "C" });
            userRepoMock.Setup(x => x.GetUser(newUserName)).Returns(new User { Username = newUserName, FirstName = "Chen", LastName = "C" });

            var service = new TicketService(userRepoMock.Object, new EmailServiceProxy());

            var title = "test";
            var priority = Priority.Medium;
            var desp = "The system crashed when user performed a search";
            var createDate = DateTime.UtcNow;

            var ticketId = service.CreateTicket(title, priority, userName, desp, createDate, false);
            Ticket ticket = TicketRepository.GetTicket(ticketId);
            Assert.IsTrue(ticket.AssignedUser.Username == userName);

            service.AssignTicket(ticket.Id, newUserName);
            Ticket updatedTicket = TicketRepository.GetTicket(ticketId);
            Assert.IsTrue(updatedTicket.AssignedUser.Username == newUserName);
        }
    }
}
