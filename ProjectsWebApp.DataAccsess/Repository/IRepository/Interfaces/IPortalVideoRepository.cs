using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.DataAccsess.Repository.IRepository
{
    public interface IPortalVideoRepository : IRepository<PortalVideo>
    {
        void Update(PortalVideo card);
    }
}
