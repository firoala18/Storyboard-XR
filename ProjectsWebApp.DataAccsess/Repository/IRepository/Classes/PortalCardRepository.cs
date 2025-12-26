using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.DataAccsess.Repository;

public class PortalCardRepository : Repository<PortalCard>, IPortalCardRepository
{
    private readonly ApplicationDbContext _db;

    public PortalCardRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public void Update(PortalCard card)
    {
        var objFromDb = _db.PortalCards.FirstOrDefault(c => c.Id == card.Id);
        if (objFromDb != null)
        {
            objFromDb.Title = card.Title;
            objFromDb.Content = card.Content;
            objFromDb.DisplayOrder = card.DisplayOrder;
           
        }
    }
}
