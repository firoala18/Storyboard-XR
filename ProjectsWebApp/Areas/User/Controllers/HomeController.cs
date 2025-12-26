using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using ProjectsWebApp.DataAccsess.Data;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.Models;
using ProjectsWebApp.Models.ViewModels;
using System.Diagnostics;
using System.Drawing.Printing;

namespace ProjectsWebApp.Areas.User.Controllers
{
    [Area("User")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        public HomeController(ILogger<HomeController> logger, IUnitOfWork unitOfWork, ApplicationDbContext db, UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _unitOfWork = unitOfWork;
            _db = db;
            _userManager = userManager;
        }



        public async Task<IActionResult> Storyboards()
        {
            var items = await _db.Storyboards.AsNoTracking().ToListAsync();
            return View(items);
        }



        public IActionResult KIBarDetails(int id)
        {
            var project = _unitOfWork.MakerSpaceProject.Get(u => u.Id == id);
            if (project == null)
            {
                return NotFound();
            }

            return View(project);
        }

        public IActionResult Uebersicht()
        {
            var content = _unitOfWork.GetRepository<UebersichtContent>().Get(u => u.Id == 1);
            if (content == null)
            {
                content = new UebersichtContent { ContentHtml = "Kein Inhalt verfügbar." };
            }
            return View(content);
        }


        public IActionResult KIBar()
        {
            var allProjects = _unitOfWork.MakerSpaceProject.GetAll()
                .Select(p => new MakerSpaceProject
                {
                    Id = p.Id,
                    DisplayOrder = p.DisplayOrder,
                    Title = p.Title,
                    Tags = p.Tags,
                    Top = p.Top,
                    download = p.download,
                    lesezeichen = p.lesezeichen,
                    tutorial = p.tutorial,
                    netzwerk = p.netzwerk,
                    events = p.events,
                    Forschung = p.Forschung,
                    ITRecht = p.ITRecht,
                    Beitraege = p.Beitraege,
                    ProjectUrl = p.ProjectUrl,
                    ImageUrl = p.ImageUrl,
                    Description = p.Description
                })
                .ToList();

            var tags = allProjects.SelectMany(p => p.Tags?.Split(',') ?? new string[0])
                                  .Select(t => t.Trim())
                                  .Distinct()
                                  .OrderBy(t => t)
                                  .ToList();

            var description = _unitOfWork.MakerSpaceDescription.GetAll().FirstOrDefault();

            var email = _unitOfWork.GetRepository<ContactEmail>().Get(e => e.Id == 1)?.Email ?? "h.seehagen-marx@uni-wuppertal.de";

            ViewBag.Tags = tags;
            ViewBag.MakerSpaceDescription = description;
            ViewBag.ContactEmail = email;
            return View(allProjects);
        }

      

        [HttpGet]
        public IActionResult StoryBoard()
        {
         
            return View();
        }



        public IActionResult GoBack()
        {
            // Hole die zuletzt besuchte Seite aus der Session
            int lastPage = HttpContext.Session.GetInt32("LastPage") ?? 1;

            // Leite den Benutzer zur letzten Seite um
            return RedirectToAction("Index", new { page = lastPage });
        }
        public IActionResult Impressum()
        {
            // Retrieve all DatenschutzContent entries ordered by DisplayOrder
            var model = _unitOfWork.ImpressumContent.GetAll().OrderBy(dc => dc.DisplayOrder);
            return View(model);
        }

        public IActionResult Datenschutz()
        {
            // Retrieve all DatenschutzContent entries ordered by DisplayOrder
            var model = _unitOfWork.DatenschutzContent.GetAll().OrderBy(dc => dc.DisplayOrder);
            return View(model);
        }



      

        private string GetNamesFromIds(string ids, Dictionary<int, string> mapping)
        {
            if (string.IsNullOrWhiteSpace(ids))
                return string.Empty;

            return string.Join(", ", ids.Split(',')
                                        .Select(id => int.TryParse(id, out var parsedId) && mapping.ContainsKey(parsedId) ? mapping[parsedId] : null)
                                        .Where(name => name != null));
        }


        public IActionResult Home()
        {
            var cards = _unitOfWork.PortalCard.GetAll();
            var video = _unitOfWork.PortalVideo
                .GetAll()
                .OrderByDescending(v => v.Id)
                .FirstOrDefault();
            return View((cards, video));
        }
        [Area("User")]
        public IActionResult Kontakt()
        {
            var kontaktCards = _unitOfWork.GetRepository<KontaktCard>().GetAll().OrderBy(k => k.DisplayOrder);
            return View(kontaktCards);
        }

       

        public IActionResult Urheberrecht()
        {
            // Retrieve all DatenschutzContent entries ordered by DisplayOrder
            var model = _unitOfWork.UrheberechtContent.GetAll().OrderBy(dc => dc.DisplayOrder);
            return View(model);
        }

        public IActionResult Leichtesprache()
        {
            var content = _unitOfWork.GetRepository<LeichteSpracheContent>().Get(u => u.Id == 1);
            if (content == null)
            {
                content = new LeichteSpracheContent { ContentHtml = "No content available." };
            }
            return View(content);
        }


        public IActionResult Tipps()
        {
            var contents = _unitOfWork.mitMachenContent.GetAll();
            return View(contents);
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }


}
