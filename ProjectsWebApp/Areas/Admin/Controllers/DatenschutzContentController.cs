using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectsWebApp.DataAccsess.Repository.IRepository;

using ProjectsWebApp.Models;

namespace ProjectsWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DatenschutzContentController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;

        public DatenschutzContentController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public IActionResult Index()
        {
            var contents = _unitOfWork.DatenschutzContent.GetAll().OrderBy(c => c.DisplayOrder);
            return View(contents);
        }

        public IActionResult Upsert(int? id)
        {
            DatenschutzContent content = id.HasValue
                ? _unitOfWork.DatenschutzContent.Get(x => x.Id == id)
                : new DatenschutzContent();

            return View(content);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(DatenschutzContent content)
        {
            if (!ModelState.IsValid)
                return View(content);

            if (content.Id == 0)
                _unitOfWork.DatenschutzContent.Add(content);
            else
                _unitOfWork.DatenschutzContent.Update(content);

            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var content = _unitOfWork.DatenschutzContent.Get(x => x.Id == id);
            if (content == null)
                return Json(new { success = false });

            _unitOfWork.DatenschutzContent.Remove(content);
            _unitOfWork.Save();
            return Json(new { success = true });
        }

        [HttpPost]
        public IActionResult UpdateOrder(List<int> ids)
        {
            var contents = _unitOfWork.DatenschutzContent.GetAll().ToList();
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
