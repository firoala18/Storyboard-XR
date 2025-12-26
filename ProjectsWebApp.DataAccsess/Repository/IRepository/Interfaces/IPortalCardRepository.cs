using ProjectsWebApp.DataAccsess.Repository.IRepository;

public interface IPortalCardRepository : IRepository<PortalCard>
{
    void Update(PortalCard card);
}
