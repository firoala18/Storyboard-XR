using Microsoft.AspNetCore.Mvc;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.Models;
using Microsoft.AspNetCore.Authorization;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UebersichtController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public UebersichtController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // View the Übersicht content
    public IActionResult Edit()
    {
        var content = _unitOfWork.GetRepository<UebersichtContent>().Get(u => u.Id == 1);
        if (content == null)
        {
            content = new UebersichtContent { Id = 1, ContentHtml = "" };
            _unitOfWork.GetRepository<UebersichtContent>().Add(content);
            _unitOfWork.Save();
        }
        return View(content);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(UebersichtContent uebersichtContent)
    {
        if (ModelState.IsValid)
        {
            var existingContent = _unitOfWork.GetRepository<UebersichtContent>().Get(u => u.Id == 1);
            if (existingContent != null)
            {
                existingContent.ContentHtml = uebersichtContent.ContentHtml;
                _unitOfWork.GetRepository<UebersichtContent>().Update(existingContent);
                _unitOfWork.Save();
                TempData["success"] = "Inhalt erfolgreich aktualisiert!";
                return RedirectToAction("Uebersicht", "Home", new { area = "" });
            }
            else
            {
                uebersichtContent.Id = 1;
                _unitOfWork.GetRepository<UebersichtContent>().Add(uebersichtContent);
                _unitOfWork.Save();
                TempData["success"] = "Inhalt erfolgreich erstellt!";
                return RedirectToAction("Uebersicht", "Home", new { area = "" });
            }
        }

        TempData["error"] = "Ungültige Eingabe.";
        return View(uebersichtContent);
    }
}
