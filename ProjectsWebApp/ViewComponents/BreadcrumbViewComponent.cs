using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using ProjectsWebApp.DataAccsess.Repository.IRepository;

namespace ProjectsWebApp.ViewComponents
{
    public class BreadcrumbViewComponent : ViewComponent
    {
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly IUnitOfWork _unitOfWork;

        public BreadcrumbViewComponent(
            IActionContextAccessor actionContextAccessor,
            IUnitOfWork unitOfWork)
        {
            _actionContextAccessor = actionContextAccessor;
            _unitOfWork = unitOfWork;
        }

        public IViewComponentResult Invoke()
        {
            var rd = _actionContextAccessor.ActionContext.RouteData;
            var controller = rd.Values["controller"]?.ToString();
            var action = rd.Values["action"]?.ToString();
            var area = rd.Values["area"]?.ToString();

            var crumbs = new List<(string Title, string Url)>();

     

            /* 1) Root link ---------------------------------------------------- */
            crumbs.Add((
                "Story Board",
                Url.Action("Home", "Home", new { area = "User" })
            ));

            /* 2) Admin area --------------------------------------------------- */
            if (string.Equals(area, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                var pageTitle = ViewContext.ViewData["Title"]?.ToString() ?? action;
                crumbs.Add((pageTitle, ""));
                return View(crumbs);
            }

            if (controller == "Home" && action == "KIBarDetails")
            {
                crumbs.Add((
                    "KIBar",
                    Url.Action("KIBar", "Home", new { area = "User" })
                ));

                // “Procedual World Generation” usw. kommt aus ViewData["Title"]
                var title = ViewContext.ViewData["Title"]?.ToString() ?? "Details";
                crumbs.Add((title, ""));
                return View(crumbs);
            }

            /* 3) PromptDetails → Bibliothek > PromptDetails ------------------ */
            if (controller == "Home" && action == "PromptDetails")
            {
                crumbs.Add((
                    "Bibliothek",
                    Url.Action("Bibliothek", "Home", new { area = "User" })
                ));
                crumbs.Add(("Prompt-Details", ""));
                return View(crumbs);
            }

            if (controller == "MyPrompts" && action == "Index")
            {
               
                  
                crumbs.Add(("Prompt-Sammlung", ""));
                return View(crumbs);
            }

            /* 4) Landing page → Prompt Engineering --------------------------- */
            if (controller == "Home" && action == "Landing")
            {
                crumbs.Add(("PromptEngineering", ""));
                return View(crumbs);
            }

            /* 5) 🆕 PromptAssistent → PromptEngineering > PromptAssistent ---- */
            if (controller == "Home" && action == "PromptAssistent")
            {
                crumbs.Add((
                    "Prompt-Engineering",
                    Url.Action("PromptEngineering", "Home", new { area = "User" })
                ));
                crumbs.Add(("PromptAssistent", ""));
                return View(crumbs);
            }
            if (controller == "Home" && action == "PromptEngineering")
            {
              
                crumbs.Add(("Prompt-Engineering", ""));
                return View(crumbs);
            }

            /* 6) MyPrompts/Details ------------------------------------------- */
            if (controller == "MyPrompts" && action == "Details")
            {
                crumbs.Add((
                    "Prompt-Sammlung",
                    Url.Action("Index", "MyPrompts", new { area = "User" })
                ));
                crumbs.Add(("Prompt-Detail", ""));
                return View(crumbs);
            }



            /* 7) Home/Home (start page) -------------------------------------- */
            if (controller == "Home" && action == "Home")
            {
                return View(crumbs);
            }

            /* 8) Index actions in user area ---------------------------------- */
            if (string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase))
            {
                var indexTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "Home",       "Projekte"          },
                    { "Event",      "Events"            },
                    { "Skills",     "Skills"            },
                    { "Kontakt",    "Team"              },
                    { "Bibliothek", "Bibliothek"        },
                    
                };

                var title = indexTitles.TryGetValue(controller ?? "", out var t)
                            ? t
                            : (controller ?? "Übersicht");

                crumbs.Add((title, ""));
                return View(crumbs);
            }

            /* 9) Generic Details branch (e.g. Event/Details) ----------------- */
            if (string.Equals(action, "Details", StringComparison.OrdinalIgnoreCase))
            {
                if (controller == "Event")
                {
                    crumbs.Add(("Events",
                        Url.Action("Index", "Event", new { area = "User" })));
                    crumbs.Add(("Details", ""));
                    return View(crumbs);
                }
                // ... add further Details cases here if needed ...
            }

            /* 10) Fallback ---------------------------------------------------- */
            var fallback = ViewContext.ViewData["Title"]?.ToString() ?? action;
            crumbs.Add((fallback, ""));
            return View(crumbs);
        }
    }
}
