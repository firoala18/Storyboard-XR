using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.DataAccsess.Repository;
using ProjectsWebApp.Models;

public class MitmachenContentRepository : Repository<MitmachenContent>, IMitmachenContentRepository
{
    private readonly ApplicationDbContext _db;

    public MitmachenContentRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public void Update(MitmachenContent card)
    {
        var objFromDb = _db.MitmachenContents.FirstOrDefault(c => c.Id == card.Id);
        if (objFromDb != null)
        {
            objFromDb.SectionType = card.SectionType;
            objFromDb.Title = card.Title;
            objFromDb.Content = card.Content;
           objFromDb.DisplayOrder = card.DisplayOrder;
        }
    }
}
