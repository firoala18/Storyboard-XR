using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectsWebApp.DataAccsess.Repository.IRepository;
using ProjectsWebApp.Utility;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace ProjectsWebApp.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class BaseController<T> : Controller where T : class
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly string _entityName;

        public BaseController(IUnitOfWork unitOfWork, string entityName)
        {
            _unitOfWork = unitOfWork;
            _entityName = entityName;
        }

        public IActionResult Index()
        {
            var entities = _unitOfWork.GetRepository<T>().GetAll();
            return View(entities);
        }

        public IActionResult Create() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(T entity)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.GetRepository<T>().Add(entity);
                _unitOfWork.Save();
                TempData["success"] = $"{_entityName} created successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(entity);
        }

        public IActionResult Edit(int id)
        {
            var entity = _unitOfWork.GetRepository<T>().Get(e => EF.Property<int>(e, "Id") == id);
            if (entity == null) return NotFound();
            return View(entity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(T entity)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.GetRepository<T>().Update(entity);
                _unitOfWork.Save();
                TempData["success"] = $"{_entityName} updated successfully";
                return RedirectToAction(nameof(Index));
            }
            return View(entity);
        }

        public IActionResult Delete(int id)
        {
            var entity = _unitOfWork.GetRepository<T>().Get(e => EF.Property<int>(e, "Id") == id);
            if (entity == null) return NotFound();
            return View(entity);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var entity = _unitOfWork.GetRepository<T>().Get(e => EF.Property<int>(e, "Id") == id);
            if (entity == null) return NotFound();

            _unitOfWork.GetRepository<T>().Remove(entity);
            _unitOfWork.Save();
            TempData["success"] = $"{_entityName} deleted successfully";
            return RedirectToAction(nameof(Index));
        }
    }
}
