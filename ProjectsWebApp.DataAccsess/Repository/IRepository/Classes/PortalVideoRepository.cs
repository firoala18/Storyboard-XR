using ProjectsWebApp.DataAccsess.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.DataAccsess.Repository.IRepository.Classes
{
    public class PortalVideoRepository : Repository<PortalVideo>, IPortalVideoRepository
    {
        private readonly ApplicationDbContext _db;

        public PortalVideoRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(PortalVideo card)
        {
            var objFromDb = _db.PortalVideo.FirstOrDefault(c => c.Id == card.Id);
            if (objFromDb != null)
            {
                objFromDb.VideoPath = card.VideoPath;
                objFromDb.UploadDate = card.UploadDate;
                objFromDb.ImagePath = card.ImagePath;
                objFromDb.ShowImageInsteadOfVideo = card.ShowImageInsteadOfVideo;

            }
        }
    }
}
