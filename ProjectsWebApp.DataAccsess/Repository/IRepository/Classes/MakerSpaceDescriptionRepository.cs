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
    public class MakerSpaceDescriptionRepository : Repository<MakerSpaceDescription>, IMakerSpaceDescriptionRepository
    {

        private ApplicationDbContext _db;

        public MakerSpaceDescriptionRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }


        public void Update(MakerSpaceDescription obj)
        {
            var objFromDb = _db.MakerSpaceDescriptions.FirstOrDefault(u => u.Id == obj.Id);
            if (objFromDb != null)
            {
                // General Project Overview
                objFromDb.Title = obj.Title;
                objFromDb.SubTitle = obj.SubTitle;
                objFromDb.Content = obj.Content;
                

            }
        }

    }
}
