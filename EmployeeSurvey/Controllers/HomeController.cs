using EmployeeSurvey.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;

public class HomeController : Controller
{
	private readonly AppDbContext _context;

	public HomeController(AppDbContext context)
	{
		_context = context;
	}

	// ========== LOGIN ==========
	[HttpGet]
	public IActionResult Login()
	{
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Login(string email, string password)
	{
		if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
		{
			ViewBag.Error = "Email và mật khẩu là bắt buộc.";
			return View();
		}

		var user = await _context.Users
			.Include(u => u.Role)
			.FirstOrDefaultAsync(u => u.Email == email
								   && u.PasswordHash == password
								   && u.Status == true);

		if (user != null)
		{
			HttpContext.Session.SetInt32("UserId", user.UserId);
			HttpContext.Session.SetString("FullName", user.FullName ?? "");
			HttpContext.Session.SetString("Role", user.Role?.RoleName ?? "");

			return RedirectToAction("Index", "Users");
		}

		ViewBag.Error = "Sai email hoặc mật khẩu.";
		return View();
	}

	public IActionResult Logout()
	{
		HttpContext.Session.Clear();
		return RedirectToAction("Login");
	}

	public IActionResult Index()
	{
		if (HttpContext.Session.GetInt32("UserId") == null)
			return RedirectToAction("Login");

		ViewBag.FullName = HttpContext.Session.GetString("FullName");
		return View();
	}

	// ========== FORGOT PASSWORD ==========
	[HttpGet]
	public IActionResult ForgotPassword()
	{
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ForgotPassword(string email)
	{
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
		if (user == null)
		{
			ViewBag.Error = "Email không tồn tại.";
			return View();
		}

		// Tạo token và hạn sử dụng
		var token = Guid.NewGuid().ToString();
		user.ResetToken = token;
		user.ResetTokenExpiry = DateTime.Now.AddHours(1);
		await _context.SaveChangesAsync();

		// (Demo) Hiển thị link reset thay vì gửi mail
		ViewBag.ResetLink = Url.Action("ResetPassword", "Home",
			new { token = token, email = email }, Request.Scheme);

		return View("ForgotPasswordConfirmation");
	}

	// ========== RESET PASSWORD ==========
	[HttpGet]
	public async Task<IActionResult> ResetPassword(string token, string email)
	{
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.ResetToken == token);
		if (user == null || user.ResetTokenExpiry < DateTime.Now)
		{
			return BadRequest("Token không hợp lệ hoặc đã hết hạn.");
		}

		ViewBag.Email = email;
		ViewBag.Token = token;
		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ResetPassword(string email, string token, string newPassword)
	{
		var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.ResetToken == token);
		if (user == null || user.ResetTokenExpiry < DateTime.Now)
		{
			return BadRequest("Token không hợp lệ hoặc đã hết hạn.");
		}

		// TODO: Hash mật khẩu thay vì lưu plain text
		user.PasswordHash = newPassword;
		user.ResetToken = null;
		user.ResetTokenExpiry = null;

		await _context.SaveChangesAsync();

		ViewBag.Message = "Đổi mật khẩu thành công. Mời bạn đăng nhập.";
		return RedirectToAction("Login");
	}
}
