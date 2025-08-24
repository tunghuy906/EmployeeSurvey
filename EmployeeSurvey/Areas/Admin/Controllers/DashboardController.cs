using Microsoft.AspNetCore.Mvc;

namespace EmployeeSurvey.Admin.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class DashboardController : Controller
	{
		public IActionResult Index()
		{
			// Lấy tên người dùng từ session để hiển thị
			ViewBag.FullName = HttpContext.Session.GetString("FullName");
			return View();
		}
	}
}
