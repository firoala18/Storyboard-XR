using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.DataAccsess.Repository.IRepository.Interfaces;
using ProjectsWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.DataAccsess.Repository.IRepository.Classes
{
    public class MakerSpaceRepository : Repository<MakerSpaceProject>, IMakerSpaceRepository
    {

        private ApplicationDbContext _db;

        public MakerSpaceRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


        public void Update(MakerSpaceProject obj)
        {
            var objFromDb = _db.MakerSpaceProjects.FirstOrDefault(u => u.Id == obj.Id);
            if (objFromDb != null)
            {
                // General Project Overview
                objFromDb.Title = obj.Title;
                objFromDb.Tags = obj.Tags;
                objFromDb.ITRecht = obj.ITRecht;
                objFromDb.Beitraege = obj.Beitraege;
                objFromDb.lesezeichen = obj.lesezeichen;
                objFromDb.events = obj.events;
                objFromDb.Top = obj.Top;
                objFromDb.Forschung = obj.Forschung;
                objFromDb.tutorial = obj.tutorial;

                objFromDb.Description = obj.Description;
                objFromDb.ProjectUrl = obj.ProjectUrl;
                objFromDb.DisplayOrder = obj.DisplayOrder;
                objFromDb.ImageUrl = obj.ImageUrl;


            }
        }

    }
}
