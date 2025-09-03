using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeSurvey.Models;

namespace EmployeeSurvey.Controllers
{
	public class FeedbacksController : Controller
	{
		private readonly AppDbContext _context;

		public FeedbacksController(AppDbContext context)
		{
			_context = context;
		}

		// ✅ Admin xem danh sách feedback
		public async Task<IActionResult> Index()
		{
			var feedbacks = _context.Feedbacks
									.Include(f => f.Test)
									.Include(f => f.User)
									.OrderByDescending(f => f.CreatedDate);
			return View(await feedbacks.ToListAsync());
		}

		// ✅ Admin xem chi tiết
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var feedback = await _context.Feedbacks
				.Include(f => f.Test)
				.Include(f => f.User)
				.FirstOrDefaultAsync(m => m.FeedbackId == id);

			if (feedback == null) return NotFound();

			return View(feedback);
		}

		// ✅ User gửi feedback
		[HttpGet]
		public IActionResult Create(int testId)
		{
			var userId = HttpContext.Session.GetInt32("UserId");
			if (userId == null) return RedirectToAction("Login", "Home");

			ViewBag.TestId = testId;
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(int attemptId, string content, string? returnUrl)
		{
			var userId = HttpContext.Session.GetInt32("UserId");
			if (userId == null) return RedirectToAction("Login", "Home");

			if (string.IsNullOrWhiteSpace(content))
			{
				TempData["Error"] = "❌ Nội dung không được để trống";
				return Redirect(returnUrl ?? Request.Headers["Referer"].ToString());
			}

			var attempt = _context.TestAttempts.FirstOrDefault(a => a.AttemptId == attemptId && a.UserId == userId);
			if (attempt == null)
			{
				TempData["Error"] = "❌ Không tìm thấy lần làm bài.";
				return RedirectToAction("Index", "MyTests");
			}

			var fb = new Feedback
			{
				UserId = userId.Value,
				TestId = attempt.TestId,     // DB Feedbacks đang cần TestID (NOT NULL)
				Content = content,
				CreatedDate = DateTime.Now
			};

			_context.Feedbacks.Add(fb);
			_context.SaveChanges();

			TempData["Success"] = "✅ Cảm ơn bạn đã gửi phản hồi!";
			return Redirect(returnUrl ?? Url.Action("Result", "MyTests", new { id = attempt.AttemptId }));
		}


		// ✅ Admin xóa feedback
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();

			var feedback = await _context.Feedbacks
				.Include(f => f.Test)
				.Include(f => f.User)
				.FirstOrDefaultAsync(m => m.FeedbackId == id);

			if (feedback == null) return NotFound();

			return View(feedback);
		}

		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var feedback = await _context.Feedbacks.FindAsync(id);
			if (feedback != null)
			{
				_context.Feedbacks.Remove(feedback);
				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(Index));
		}
	}
}
