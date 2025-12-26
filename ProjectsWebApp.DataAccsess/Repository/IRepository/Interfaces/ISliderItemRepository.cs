using ProjectsWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.DataAccsess.Repository.IRepository
{
    public interface ISliderItemRepository : IRepository<SliderItem>
    {
        void Update(SliderItem obj);
    }
}
