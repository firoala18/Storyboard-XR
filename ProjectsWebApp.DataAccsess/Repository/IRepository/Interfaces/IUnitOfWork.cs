using ProjectsWebApp.DataAccsess.Repository.IRepository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.DataAccsess.Repository.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<T> GetRepository<T>() where T : class;
  
        IPortalCardRepository PortalCard { get; }
        IPortalVideoRepository PortalVideo { get; }
        IMitmachenContentRepository mitMachenContent { get; }
        ISliderItemRepository SliderItem { get; }
        IImpressumContentRepository ImpressumContent { get; }
        IDatenschutzContentRepository DatenschutzContent { get; }
        IUrheberechtContentRepository UrheberechtContent { get; }
        IMakerSpaceRepository MakerSpaceProject { get; }
        IMakerSpaceDescriptionRepository MakerSpaceDescription { get; }


        void Save();
    }
}
