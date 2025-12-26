using Microsoft.AspNetCore.Mvc;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.Models;
using Microsoft.AspNetCore.Authorization;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class LeichteSpracheController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public LeichteSpracheController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // View the LeichteSprache content
    public IActionResult Edit()
    {
        var content = _unitOfWork.GetRepository<LeichteSpracheContent>().Get(u => u.Id == 1);
        if (content == null)
        {
            content = new LeichteSpracheContent { Id = 1, ContentHtml = "" };
            _unitOfWork.GetRepository<LeichteSpracheContent>().Add(content);
            _unitOfWork.Save();
        }
        return View(content);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(LeichteSpracheContent LeichteSpracheContent)
    {
        if (ModelState.IsValid)
        {
            var existingContent = _unitOfWork.GetRepository<LeichteSpracheContent>().Get(u => u.Id == 1);
            if (existingContent != null)
            {
                existingContent.ContentHtml = LeichteSpracheContent.ContentHtml; // Update content
                _unitOfWork.GetRepository<LeichteSpracheContent>().Update(existingContent);
                _unitOfWork.Save();
                TempData["success"] = "Content updated successfully!";
                // Redirect to the Urheberrecht action in the HomeController
                return RedirectToAction("Leichtesprache", "Home", new { area = "" });
            }
            else
            {
                // If no existing content, create a new one
                LeichteSpracheContent.Id = 1; // Ensure ID is set
                _unitOfWork.GetRepository<LeichteSpracheContent>().Add(LeichteSpracheContent);
                _unitOfWork.Save();
                TempData["success"] = "Content created successfully!";
                // Redirect to the Urheberrecht action in the HomeController
                return RedirectToAction("Leichtesprache", "Home", new { area = "" });
            }
        }
        // If model state is invalid, return to the edit view with validation errors
        TempData["error"] = "Invalid content. Please check your input.";
        return View(LeichteSpracheContent);
    }
}
