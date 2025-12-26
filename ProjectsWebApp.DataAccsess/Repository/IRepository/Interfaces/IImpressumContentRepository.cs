using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.DataAccsess.Repository.IRepository.Interfaces
{
    public interface IImpressumContentRepository : IRepository<ImpressumContent>
    {
        void Update(ImpressumContent card);
    }

}
