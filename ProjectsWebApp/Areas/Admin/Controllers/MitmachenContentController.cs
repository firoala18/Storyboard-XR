using Microsoft.AspNetCore.Mvc;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.Models;
using Microsoft.AspNetCore.Authorization;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class MitmachenContentController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public MitmachenContentController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        var contents = _unitOfWork.mitMachenContent.GetAll();
        return View(contents);
    }

    public IActionResult Upsert(int? id)
    {
        MitmachenContent content = new();
        if (id != null)
        {
            content = _unitOfWork.mitMachenContent.Get(x => x.Id == id);
            if (content == null)
                return NotFound();
        }
        return View(content);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert(MitmachenContent mitmachenContent)
    {
   
        if (string.IsNullOrWhiteSpace(mitmachenContent.Title))
        {
            ModelState.AddModelError("Title", "Content cannot be empty.");
            return View(mitmachenContent);
        }
        if (string.IsNullOrWhiteSpace(mitmachenContent.Content))
        {
            ModelState.AddModelError("Title", "Content cannot be empty.");
            return View(mitmachenContent);
        }
        if (string.IsNullOrWhiteSpace(mitmachenContent.SectionType))
        {
            ModelState.AddModelError("Title", "Content cannot be empty.");
            return View(mitmachenContent);
        }
     
        if (ModelState.IsValid)
        {
            if (mitmachenContent.Id == 0)
                _unitOfWork.mitMachenContent.Add(mitmachenContent);
            else
                _unitOfWork.mitMachenContent.Update(mitmachenContent);

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }
        return View(mitmachenContent);
    }

    [HttpPost]
    public IActionResult UpdateOrder(List<int> ids)
    {
        var contents = _unitOfWork.mitMachenContent.GetAll().ToList();
        for (int i = 0; i < ids.Count; i++)
        {
            var content = contents.FirstOrDefault(c => c.Id == ids[i]);
            if (content != null)
            {
                content.DisplayOrder = i + 1;
            }
        }
        _unitOfWork.Save();
        return Json(new { success = true });
    }


    [HttpPost]
    public IActionResult Delete(int id)
    {
        var content = _unitOfWork.mitMachenContent.Get(x => x.Id == id);
        if (content == null)
            return Json(new { success = false, message = "Content not found." });

        _unitOfWork.mitMachenContent.Remove(content);
        _unitOfWork.Save();
        return Json(new { success = true, message = "Content deleted successfully." });
    }
}
