using Microsoft.AspNetCore.Mvc;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.Models;
using Microsoft.AspNetCore.Authorization;
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class SliderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWebHostEnvironment _hostEnvironment;

    public SliderController(IUnitOfWork unitOfWork, IWebHostEnvironment hostEnvironment)
    {
        _unitOfWork = unitOfWork;
        _hostEnvironment = hostEnvironment;
    }

    public IActionResult Index(bool isVK = false)
    {
        var sliderItems = _unitOfWork.SliderItem
            .GetAll()
            .Where(s => s.IsForVirtuellesKlassenzimmer == isVK)
            .OrderBy(s => s.DisplayOrder)
            .ToList();

        ViewBag.IsVK = isVK;
        return View(sliderItems);
    }


    public IActionResult Upsert(int? id, bool isVK = false)
    {
        var sliderItem = id == null ? new SliderItem() : _unitOfWork.SliderItem.Get(x => x.Id == id);
        sliderItem.IsForVirtuellesKlassenzimmer = isVK;
        return View(sliderItem);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Upsert(SliderItem sliderItem)
    {
        if (ModelState.IsValid)
        {
            string wwwRootPath = _hostEnvironment.WebRootPath;

            if (sliderItem.ImageFile != null && sliderItem.ImageFile.Length > 0)
            {
                string fileName = Guid.NewGuid().ToString();
                var uploads = Path.Combine(wwwRootPath, "images", "slider");
                var extension = Path.GetExtension(sliderItem.ImageFile.FileName);

                // Create directory if it doesn't exist
                if (!Directory.Exists(uploads))
                {
                    Directory.CreateDirectory(uploads);
                }

                // Delete old image if it exists
                if (!string.IsNullOrEmpty(sliderItem.ImageUrl))
                {
                    var oldImagePath = Path.Combine(wwwRootPath, sliderItem.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Save new image
                using (var fileStream = new FileStream(Path.Combine(uploads, fileName + extension), FileMode.Create))
                {
                    sliderItem.ImageFile.CopyTo(fileStream);
                }
                sliderItem.ImageUrl = $"/images/slider/{fileName}{extension}";
            }

            // Handle update case when no new image is uploaded
            else if (sliderItem.Id != 0)
            {
                // Preserve existing image URL
                var existingItem = _unitOfWork.SliderItem.Get(x => x.Id == sliderItem.Id);
                sliderItem.ImageUrl = existingItem.ImageUrl;
            }

            if (sliderItem.Id == 0)
            {
                _unitOfWork.SliderItem.Add(sliderItem);
            }
            else
            {
                _unitOfWork.SliderItem.Update(sliderItem);
            }

            _unitOfWork.Save();
            return RedirectToAction("Index");
        }
        return View(sliderItem);
    }

    [HttpPost]
    public IActionResult Delete(int id)
    {
        try
        {
            var sliderItem = _unitOfWork.SliderItem.Get(x => x.Id == id);
            if (sliderItem == null)
            {
                return Json(new { success = false, message = "Slide not found" });
            }

            // Delete image file if exists
            if (!string.IsNullOrEmpty(sliderItem.ImageUrl))
            {
                var imagePath = Path.Combine(_hostEnvironment.WebRootPath, sliderItem.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _unitOfWork.SliderItem.Remove(sliderItem);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Deleted successfully" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = $"Error: {ex.Message}" });
        }
    }

    [HttpPost]
    public IActionResult UpdateOrder(List<int> ids)
    {
        var sliderItems = _unitOfWork.SliderItem.GetAll().ToList();
        for (int i = 0; i < ids.Count; i++)
        {
            var slider = sliderItems.FirstOrDefault(s => s.Id == ids[i]);
            if (slider != null)
            {
                slider.DisplayOrder = i + 1; // Starts from 1 instead of 0
            }
        }
        _unitOfWork.Save();
        return Json(new { success = true });
    }

}