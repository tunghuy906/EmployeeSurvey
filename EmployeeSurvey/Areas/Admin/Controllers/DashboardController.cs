using EmployeeSurvey.Models;
using Microsoft.AspNetCore.Mvc;

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

			// Đếm số feedback (coi như báo cáo)
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

			return View();
		}

	}

}
