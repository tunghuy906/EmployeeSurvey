using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EmployeeSurvey.Models;

namespace EmployeeSurvey.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class AdminAuditLogsController : Controller
	{
		private readonly AppDbContext _context;

		public AdminAuditLogsController(AppDbContext context)
		{
			_context = context;
		}

		// ================== HÀM GHI LOG ==================
		private async Task AddAuditLog(int userId, string action, string? detail = null)
		{
			var log = new AuditLog
			{
				UserId = userId,
				Action = action,
				Detail = detail,
				Timestamp = DateTime.Now
			};

			_context.AuditLogs.Add(log);
			await _context.SaveChangesAsync();
		}

		// ================== DANH SÁCH LOG ==================
		public async Task<IActionResult> Index()
		{
			var logs = await _context.AuditLogs
				.Include(a => a.User)
				.OrderByDescending(a => a.Timestamp)
				.Take(200) // ✅ chỉ lấy 200 log gần nhất
				.ToListAsync();

			return View(logs);
		}
		[HttpGet]
		public async Task<IActionResult> ClearAll()
		{
			var allLogs = _context.AuditLogs.ToList();
			if (allLogs.Any())
			{
				_context.AuditLogs.RemoveRange(allLogs);
				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(Index));
		}

		// ================== CHI TIẾT LOG ==================
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var auditLog = await _context.AuditLogs
				.Include(a => a.User)
				.FirstOrDefaultAsync(m => m.LogId == id);

			if (auditLog == null) return NotFound();

			return View(auditLog);
		}

		// ================== TẠO LOG (chỉ để test thủ công) ==================
		public IActionResult Create()
		{
			ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username");
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("LogId,UserId,Action,Detail,Timestamp")] AuditLog auditLog)
		{
			if (ModelState.IsValid)
			{
				_context.Add(auditLog);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username", auditLog.UserId);
			return View(auditLog);
		}

		// ================== SỬA LOG ==================
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();

			var auditLog = await _context.AuditLogs.FindAsync(id);
			if (auditLog == null) return NotFound();

			ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username", auditLog.UserId);
			return View(auditLog);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("LogId,UserId,Action,Detail,Timestamp")] AuditLog auditLog)
		{
			if (id != auditLog.LogId) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(auditLog);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!AuditLogExists(auditLog.LogId))
						return NotFound();
					else
						throw;
				}
				return RedirectToAction(nameof(Index));
			}
			ViewData["UserId"] = new SelectList(_context.Users, "UserId", "Username", auditLog.UserId);
			return View(auditLog);
		}

		// ================== XÓA LOG ==================
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();

			var auditLog = await _context.AuditLogs
				.Include(a => a.User)
				.FirstOrDefaultAsync(m => m.LogId == id);

			if (auditLog == null) return NotFound();

			return View(auditLog);
		}

		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var auditLog = await _context.AuditLogs.FindAsync(id);
			if (auditLog != null)
			{
				_context.AuditLogs.Remove(auditLog);

				// ✅ Ghi log cho hành động xóa log
				if (User.Identity.IsAuthenticated)
				{
					int currentUserId = int.Parse(User.FindFirst("UserId").Value);
					await AddAuditLog(currentUserId, "Xóa AuditLog", $"Đã xóa log #{auditLog.LogId}");
				}
			}

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool AuditLogExists(int id)
		{
			return _context.AuditLogs.Any(e => e.LogId == id);
		}

		// ================== LỊCH SỬ ĐĂNG NHẬP ==================
		// Gọi chỗ này khi user đăng nhập thành công
		public async Task LogLogin(int userId)
		{
			await AddAuditLog(userId, "Đăng nhập", "Người dùng đã đăng nhập hệ thống");
		}

		// Gọi chỗ này khi user đăng xuất
		public async Task LogLogout(int userId)
		{
			await AddAuditLog(userId, "Đăng xuất", "Người dùng đã đăng xuất hệ thống");
		}
	}
}
