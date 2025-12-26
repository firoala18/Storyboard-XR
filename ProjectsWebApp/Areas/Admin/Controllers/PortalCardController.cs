using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ProjectsWebApp.DataAccsess.Repository.IRepository;

using ProjectsWebApp.Models;
using System;
using System.IO;
using System.Linq;

namespace ProjectsWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PortalCardController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<PortalCardController> _logger;

        private const long MaxImageSizeBytes = 2 * 1024 * 1024;

        public PortalCardController(
            IUnitOfWork unitOfWork,
            IWebHostEnvironment webHostEnvironment,
            ILogger<PortalCardController> logger)
        {
            _unitOfWork = unitOfWork;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        // Optionally add [RequestSizeLimit(100_000_000)] if you need to allow up to 100MB files.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(500_000_000)]
        public IActionResult UpdatePortalVideo(IFormFile? videoFile)
        {
            try
            {
                string randomText = Guid.NewGuid().ToString();

                // Validate that a file was received.
                if (videoFile == null || videoFile.Length == 0 || string.IsNullOrEmpty(videoFile.FileName))
                {
                    return Json(new { success = false, message = "No valid video file received" });
                }

                // Validate file extension.
                var validExtensions = new[] { ".mp4", ".mov", ".avi" };
                var fileExtension = Path.GetExtension(videoFile.FileName).ToLower();
                if (!validExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, message = "Only MP4, MOV, and AVI files are allowed" });
                }

                // Validate file size (100 MB maximum).
                if (videoFile.Length > 100 * 1024 * 1024)
                {
                    return Json(new { success = false, message = "File size exceeds 100MB limit" });
                }

                // Retrieve existing portal video record or create a new one.
                var portalVideo = _unitOfWork.PortalVideo
                    .GetAll()
                    .OrderByDescending(v => v.Id)
                    .FirstOrDefault() ?? new PortalVideo();

                // Define the portal video folder path.
                string portalVideoFolder = Path.Combine(_webHostEnvironment.WebRootPath, "videos", "PortalVideo");

                // Delete all files in the PortalVideo folder.
                if (Directory.Exists(portalVideoFolder))
                {
                    foreach (var file in Directory.GetFiles(portalVideoFolder))
                    {
                        System.IO.File.Delete(file);
                    }
                }
                else
                {
                    Directory.CreateDirectory(portalVideoFolder);
                }

                // Save the new video file.
                string fileName = $"portal-intro{randomText}{fileExtension}";
                string relativePath = Path.Combine("videos", "PortalVideo", fileName);
                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    videoFile.CopyTo(stream);
                }

                // Update the record with the new video path.
                portalVideo.VideoPath = $"/{relativePath.Replace("\\", "/")}";
                if (portalVideo.Id == 0)
                    _unitOfWork.PortalVideo.Add(portalVideo);
                else
                    _unitOfWork.PortalVideo.Update(portalVideo);

                _unitOfWork.Save();

                return Json(new
                {
                    success = true,
                    message = "Video updated successfully",
                    newPath = portalVideo.VideoPath
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading video");
                return Json(new { success = false, message = $"Upload failed: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(10_000_000)]
        public IActionResult UpdatePortalImage(IFormFile? imageFile)
        {
            try
            {
                string randomText = Guid.NewGuid().ToString();

                if (imageFile == null || imageFile.Length == 0)
                    return Json(new { success = false, message = "No image file provided." });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".svg" };
                var extension = Path.GetExtension(imageFile.FileName).ToLower();

                if (!allowedExtensions.Contains(extension))
                    return Json(new { success = false, message = "Only JPG, PNG, WEBP, SVG are allowed." });

                if (imageFile.Length > MaxImageSizeBytes)
                    return Json(new { success = false, message = "Image exceeds 2MB size limit." });

                var portalVideo = _unitOfWork.PortalVideo
                    .GetAll()
                    .OrderByDescending(v => v.Id)
                    .FirstOrDefault() ?? new PortalVideo();
                string folder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "PortalImage");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                else
                {
                    foreach (var file in Directory.GetFiles(folder))
                        System.IO.File.Delete(file);
                }

                string fileName = $"portal-intro-img-{randomText}{extension}";
                string relPath = Path.Combine("images", "PortalImage", fileName);
                string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relPath);

                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    imageFile.CopyTo(stream);
                }

                portalVideo.ImagePath = "/" + relPath.Replace("\\", "/");
                // Ensure NOT NULL column VideoPath always has a value when saving
                if (string.IsNullOrEmpty(portalVideo.VideoPath))
                    portalVideo.VideoPath = string.Empty;
                if (portalVideo.Id == 0)
                    _unitOfWork.PortalVideo.Add(portalVideo);
                else
                    _unitOfWork.PortalVideo.Update(portalVideo);

                _unitOfWork.Save();

                return Json(new { success = true, message = "Image uploaded", imagePath = portalVideo.ImagePath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image upload failed");
                return Json(new { success = false, message = "Image upload failed: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult TogglePortalDisplay([FromBody] ToggleDisplayRequest data)
        {
            var portalVideo = _unitOfWork.PortalVideo
                .GetAll()
                .OrderByDescending(v => v.Id)
                .FirstOrDefault() ?? new PortalVideo();
            portalVideo.ShowImageInsteadOfVideo = data.UseImage;
            // Satisfy NOT NULL constraint even when only an image is configured
            if (string.IsNullOrEmpty(portalVideo.VideoPath))
                portalVideo.VideoPath = string.Empty;

            if (portalVideo.Id == 0)
                _unitOfWork.PortalVideo.Add(portalVideo);
            else
                _unitOfWork.PortalVideo.Update(portalVideo);

            _unitOfWork.Save();

            return Json(new { success = true });
        }

        // Add this class somewhere (e.g., inside the controller or in a shared DTO file)
        public class ToggleDisplayRequest
        {
            public bool UseImage { get; set; }
        }




        public IActionResult Index()
        {
            var cards = _unitOfWork.PortalCard.GetAll();
            var portalVideo = _unitOfWork.PortalVideo
                .GetAll()
                .OrderByDescending(v => v.Id)
                .FirstOrDefault() ?? new PortalVideo();
            return View((cards, portalVideo));
        }
        public IActionResult Upsert(int? id)
        {
            PortalCard card = new();
            if (id == null)
            {
                return View(card);
            }
            else
            {
                card = _unitOfWork.PortalCard.Get(x => x.Id == id.GetValueOrDefault());
                if (card == null)
                {
                    return NotFound();
                }
            }
            return View(card);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(PortalCard card)
        {
            if (ModelState.IsValid)
            {
                if (card.Id == 0)
                {
                    _unitOfWork.PortalCard.Add(card);
                }
                else
                {
                    _unitOfWork.PortalCard.Update(card);
                }
                _unitOfWork.Save();
                return RedirectToAction(nameof(Index));
            }
            return View(card);
        }

        [HttpPost]
        public IActionResult UpdateOrder(List<int> ids)
        {
            var cards = _unitOfWork.PortalCard.GetAll().ToList();
            for (int i = 0; i < ids.Count; i++)
            {
                var card = cards.FirstOrDefault(c => c.Id == ids[i]);
                if (card != null)
                {
                    card.DisplayOrder = i + 1;
                }
            }
            _unitOfWork.Save();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var card = _unitOfWork.PortalCard.Get(x => x.Id == id);
            if (card == null)
            {
                return Json(new { success = false, message = "Card not found." });
            }

            _unitOfWork.PortalCard.Remove(card);
            _unitOfWork.Save();

            return Json(new { success = true, message = "Card deleted successfully." });
        }
    }
}
