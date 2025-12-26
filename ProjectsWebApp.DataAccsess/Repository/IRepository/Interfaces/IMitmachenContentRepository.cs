using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.Models;

public interface IMitmachenContentRepository: IRepository<MitmachenContent>
{
    void Update(MitmachenContent card);
}
