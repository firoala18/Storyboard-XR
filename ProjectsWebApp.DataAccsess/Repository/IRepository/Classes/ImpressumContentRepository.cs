using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.DataAccsess.Repository.IRepository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.DataAccsess.Repository.IRepository.Classes
{
    public class ImpressumContentRepository : Repository<ImpressumContent>, IImpressumContentRepository
    {
        private readonly ApplicationDbContext _db;

        public ImpressumContentRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(ImpressumContent card)
        {
            var objFromDb = _db.ImpressumContents.FirstOrDefault(c => c.Id == card.Id);
            if (objFromDb != null)
            {
                objFromDb.SectionType = card.SectionType;
                objFromDb.Title = card.Title;
                objFromDb.ContentHtml = card.ContentHtml;
                objFromDb.DisplayOrder = card.DisplayOrder;
            }
        }
    }

}
