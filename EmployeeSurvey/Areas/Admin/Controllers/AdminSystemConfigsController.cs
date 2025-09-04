using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeSurvey.Models;

namespace EmployeeSurvey.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class AdminSystemConfigsController : Controller
	{
		private readonly AppDbContext _context;
		private readonly IWebHostEnvironment _env;

		public AdminSystemConfigsController(AppDbContext context, IWebHostEnvironment env)
		{
			_context = context;
			_env = env;
		}

		// GET: Admin/AdminSystemConfigs
		public async Task<IActionResult> Index()
		{
			return View(await _context.SystemConfigs.ToListAsync());
		}

		// GET: Admin/AdminSystemConfigs/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var systemConfig = await _context.SystemConfigs
				.FirstOrDefaultAsync(m => m.Id == id);
			if (systemConfig == null) return NotFound();

			return View(systemConfig);
		}

		// GET: Admin/AdminSystemConfigs/Create
		public IActionResult Create()
		{
			return View();
		}

		// POST: Admin/AdminSystemConfigs/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SystemConfig systemConfig, IFormFile? LogoFile)
		{
			if (LogoFile != null && LogoFile.Length > 0)
			{
				var fileName = Path.GetFileName(LogoFile.FileName);
				var filePath = Path.Combine("wwwroot/Images", fileName);

				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await LogoFile.CopyToAsync(stream);
				}

				systemConfig.LogoPath = "/Images/" + fileName;
			}

			_context.Add(systemConfig);
			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}


		// GET: Admin/AdminSystemConfigs/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();

			var systemConfig = await _context.SystemConfigs.FindAsync(id);
			if (systemConfig == null) return NotFound();

			return View(systemConfig);
		}
		// POST: Admin/AdminSystemConfigs/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, SystemConfig systemConfig, IFormFile? logoFile)
		{
			if (id != systemConfig.Id) return NotFound();

			var dbConfig = await _context.SystemConfigs.FindAsync(id);
			if (dbConfig == null) return NotFound();

			if (ModelState.IsValid)
			{
				// Cập nhật tên hệ thống
				dbConfig.SystemName = systemConfig.SystemName;

				// Nếu có upload logo mới
				if (logoFile != null && logoFile.Length > 0)
				{
					// Xóa file cũ
					if (!string.IsNullOrEmpty(dbConfig.LogoPath))
					{
						var oldPath = Path.Combine(_env.WebRootPath, dbConfig.LogoPath.TrimStart('/'));
						if (System.IO.File.Exists(oldPath))
							System.IO.File.Delete(oldPath);
					}

					// Upload file mới
					var uploadDir = Path.Combine(_env.WebRootPath, "Images");
					Directory.CreateDirectory(uploadDir); // Tự tạo nếu chưa có

					var fileName = Guid.NewGuid() + Path.GetExtension(logoFile.FileName);
					var filePath = Path.Combine(uploadDir, fileName);

					using var stream = new FileStream(filePath, FileMode.Create);
					await logoFile.CopyToAsync(stream);

					dbConfig.LogoPath = "/Images/" + fileName;
				}

				_context.Update(dbConfig);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}

			return View(systemConfig);
		}


		// GET: Admin/AdminSystemConfigs/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();

			var systemConfig = await _context.SystemConfigs
				.FirstOrDefaultAsync(m => m.Id == id);
			if (systemConfig == null) return NotFound();

			return View(systemConfig);
		}

		// POST: Admin/AdminSystemConfigs/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var systemConfig = await _context.SystemConfigs.FindAsync(id);
			if (systemConfig != null)
			{
				// Xóa file logo nếu có
				if (!string.IsNullOrEmpty(systemConfig.LogoPath))
				{
					string oldPath = Path.Combine(_env.WebRootPath, systemConfig.LogoPath.TrimStart('/'));
					if (System.IO.File.Exists(oldPath))
						System.IO.File.Delete(oldPath);
				}

				_context.SystemConfigs.Remove(systemConfig);
				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(Index));
		}

		private bool SystemConfigExists(int id)
		{
			return _context.SystemConfigs.Any(e => e.Id == id);
		}
	}
}
