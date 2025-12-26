using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ProjectsWebApp.DataAccsess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll(string includeProperties = null);
        T Get(Expression<Func<T, bool>> filter, string includeProperties = null);
        T GetFirstOrDefault(Expression<Func<T, bool>> filter, string? includeProperties = null);
        void Add(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
        void Update(T entity); 
        public void Detach(T entity);
    }  
}