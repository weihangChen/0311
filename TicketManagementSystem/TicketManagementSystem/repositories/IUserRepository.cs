namespace TicketManagementSystem
{

    public interface IUserRepository
    {
        public User GetUser(string username);
        public User GetAccountManager();

    }
}
