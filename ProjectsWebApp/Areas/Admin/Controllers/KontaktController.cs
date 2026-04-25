using Microsoft.AspNetCore.Mvc;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class KontaktController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _hostEnvironment;

    private const long MaxImageSizeBytes = 2 * 1024 * 1024;

    public KontaktController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _hostEnvironment = hostEnvironment;
    }

    public IActionResult Index()
    {
        var kontaktCards = _unitOfWork.GetRepository<KontaktCard>().GetAll().OrderBy(k => k.DisplayOrder);
        return View(kontaktCards);
    }

    public IActionResult Upsert(int? id)
    {
        KontaktCard kontaktCard = new();
        if (id == null || id == 0)
        {
            return View(kontaktCard);
        }
        else
        {
            kontaktCard = _unitOfWork.GetRepository<KontaktCard>().Get(x => x.Id == id);
            return View(kontaktCard);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert(KontaktCard kontaktCard, IFormFile? imageFile)
    {
        if (ModelState.IsValid)
        {
            string wwwRootPath = _hostEnvironment.WebRootPath;

            if (imageFile != null)
            {
                if (imageFile.Length > MaxImageSizeBytes)
                {
                    ModelState.AddModelError("imageFile", "Bild darf maximal 2 MB groß sein.");
                    return View(kontaktCard);
                }

                string fileName = Guid.NewGuid().ToString();
                var uploads = Path.Combine(wwwRootPath, "images", "Kontakt");
                var extension = Path.GetExtension(imageFile.FileName);

                // Create directory if it doesn't exist
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(kontaktCard.ImageUrl))
                {
                    var oldImagePath = Path.Combine(wwwRootPath, kontaktCard.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new image
                using (var fileStream = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                {
                    imageFile.CopyTo(fileStream);
                }
                kontaktCard.ImageUrl = $"/images/Kontakt/{fileName}{extension}";
            }

            if (kontaktCard.Id == 0)
            {
                _unitOfWork.GetRepository<KontaktCard>().Add(kontaktCard);
            }
            else
            {
                _unitOfWork.GetRepository<KontaktCard>().Update(kontaktCard);
            }

            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        return View(kontaktCard);
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        var kontaktCard = _unitOfWork.GetRepository<KontaktCard>().Get(x => x.Id == id);
        if (kontaktCard == null)
        {
            return Json(new { success = false, message = "Error while deleting" });
        }

        if (!string.IsNullOrEmpty(kontaktCard.ImageUrl))
        {
            var imagePath = Path.Combine(_hostEnvironment.WebRootPath, kontaktCard.ImageUrl.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        _unitOfWork.GetRepository<KontaktCard>().Remove(kontaktCard);
        _unitOfWork.Save();
        return Json(new { success = true, message = "Deleted successfully" });
    }

    [HttpPost]
    public IActionResult UpdateOrder(List<int> ids)
    {
        var kontaktCards = _unitOfWork.GetRepository<KontaktCard>().GetAll().ToList();
        for (int i = 0; i < ids.Count; i++)
        {
            var kontakt = kontaktCards.FirstOrDefault(k => k.Id == ids[i]);
            if (kontakt != null) kontakt.DisplayOrder = i;
        }
        _unitOfWork.Save();
        return Json(new { success = true });
    }
}
