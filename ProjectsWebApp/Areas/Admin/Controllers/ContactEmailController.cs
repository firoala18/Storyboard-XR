using Microsoft.AspNetCore.Mvc;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.Models;
using Microsoft.AspNetCore.Authorization;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ContactEmailController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public ContactEmailController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // GET: /Admin/ContactEmail/Edit
    public IActionResult Edit()
    {
        var emailEntry = _unitOfWork.GetRepository<ContactEmail>().Get(e => e.Id == 1);
        if (emailEntry == null)
        {
            emailEntry = new ContactEmail { Id = 1, Email = "" };
            _unitOfWork.GetRepository<ContactEmail>().Add(emailEntry);
            _unitOfWork.Save();
        }

        return View(emailEntry);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(ContactEmail emailEntry)
    {
        if (ModelState.IsValid)
        {
            var existing = _unitOfWork.GetRepository<ContactEmail>().Get(e => e.Id == 1);
            if (existing != null)
            {
                existing.Email = emailEntry.Email;
                _unitOfWork.GetRepository<ContactEmail>().Update(existing);
            }
            else
            {
                _unitOfWork.GetRepository<ContactEmail>().Add(emailEntry);
            }

            _unitOfWork.Save();
            TempData["success"] = "Kontakt-E-Mail erfolgreich gespeichert.";
            return RedirectToAction("Edit");
        }

        TempData["error"] = "Fehler beim Speichern der E-Mail.";
        return View(emailEntry);
    }
}
