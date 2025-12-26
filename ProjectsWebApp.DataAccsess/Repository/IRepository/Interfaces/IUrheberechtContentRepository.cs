using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.DataAccsess.Repository.IRepository.Interfaces
{
    public interface IUrheberechtContentRepository : IRepository<UrheberrechtContent>
    {
        void Update(UrheberrechtContent card);
    }

}
