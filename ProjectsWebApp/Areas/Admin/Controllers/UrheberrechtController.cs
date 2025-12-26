using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.Models;

namespace ProjectsWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UrheberrechtController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public UrheberrechtController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var contents = _unitOfWork.UrheberechtContent.GetAll().OrderBy(c => c.DisplayOrder);
            return View(contents);
        }

        public IActionResult Upsert(int? id)
        {
            UrheberrechtContent content = id.HasValue
                ? _unitOfWork.GetRepository<UrheberrechtContent>().Get(x => x.Id == id)
                : new UrheberrechtContent();

            return View(content);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(UrheberrechtContent content)
        {
            if (!ModelState.IsValid)
                return View(content);

            if (content.Id == 0)
                _unitOfWork.UrheberechtContent.Add(content);
            else
                _unitOfWork.UrheberechtContent.Update(content);

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var content = _unitOfWork.UrheberechtContent.Get(x => x.Id == id);
            if (content == null)
                return Json(new { success = false });

            _unitOfWork.UrheberechtContent.Remove(content);
            _unitOfWork.Save();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateOrder(List<int> ids)
        {
            var contents = _unitOfWork.UrheberechtContent.GetAll().ToList();
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
