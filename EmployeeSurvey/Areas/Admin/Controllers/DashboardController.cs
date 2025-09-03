using EmployeeSurvey.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSurvey.Admin.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class DashboardController : Controller
	{
		private readonly AppDbContext _context;

		public DashboardController(AppDbContext context)
		{
			_context = context;
		}

		public IActionResult Index()
		{
			// Đếm số user
			ViewBag.TotalUsers = _context.Users.Count();

			// Đếm số bài test
			ViewBag.TotalTests = _context.Tests.Count();

			// Đếm số feedback (báo cáo)
			ViewBag.TotalReports = _context.Feedbacks.Count();

			// Đếm số user có role Admin
			ViewBag.TotalAdmins = _context.Users
										  .Count(u => u.Role.RoleName == "Admin");

			// Lấy tên user đăng nhập
			var userId = HttpContext.Session.GetInt32("UserId");
			if (userId != null)
			{
				var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
				if (user != null)
				{
					ViewBag.FullName = user.FullName;
				}
			}

			// -----------------------------
			// Biểu đồ: số user đăng ký theo 7 ngày gần nhất
			// -----------------------------
			var today = DateTime.Today;
			var last7Days = Enumerable.Range(0, 7)
									  .Select(i => today.AddDays(-i))
									  .OrderBy(d => d)
									  .ToList();

			var registrations = _context.Users
				.Where(u => u.CreatedAt >= last7Days.First())
				.GroupBy(u => u.CreatedAt.Date)
				.Select(g => new { Date = g.Key, Count = g.Count() })
				.ToList();

			// Tạo nhãn (label) cho chart: dd/MM
			var chartLabels = last7Days.Select(d => d.ToString("dd/MM")).ToList();
			var chartData = last7Days.Select(d =>
				registrations.FirstOrDefault(r => r.Date == d)?.Count ?? 0
			).ToList();

			ViewBag.ChartLabels = chartLabels;
			ViewBag.ChartData = chartData;

			return View();
		}
	}
}
