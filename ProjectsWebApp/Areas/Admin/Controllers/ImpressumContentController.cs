using Microsoft.AspNetCore.Mvc;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;

namespace ProjectsWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ImpressumContentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public ImpressumContentController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var contents = _unitOfWork.ImpressumContent.GetAll().OrderBy(x => x.DisplayOrder);
            return View(contents);
        }

        public IActionResult Upsert(int? id)
        {
            var content = id == null ? new ImpressumContent() : _unitOfWork.ImpressumContent.Get(x => x.Id == id);
            return View(content);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ImpressumContent content)
        {
            if (!ModelState.IsValid)
            {
                // Add debug logging
                Console.WriteLine($"ModelState Errors: {string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))}");
                return View(content);
            }


            if (content.Id == 0)
                _unitOfWork.ImpressumContent.Add(content);
            else
                _unitOfWork.ImpressumContent.Update(content);

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var content = _unitOfWork.ImpressumContent.Get(x => x.Id == id);
            if (content == null)
                return Json(new { success = false });

            _unitOfWork.ImpressumContent.Remove(content);
            _unitOfWork.Save();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateOrder(List<int> ids)
        {
            var contents = _unitOfWork.ImpressumContent.GetAll().ToList();
            for (int i = 0; i < ids.Count; i++)
            {
                var item = contents.FirstOrDefault(x => x.Id == ids[i]);
                if (item != null)
                    item.DisplayOrder = i + 1;
            }

            _unitOfWork.Save();
            return Json(new { success = true });
        }
    }

}
