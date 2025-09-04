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
			// Lưu session
			HttpContext.Session.SetInt32("UserId", user.UserId);
			HttpContext.Session.SetString("FullName", user.FullName ?? "");
			HttpContext.Session.SetString("Role", user.Role?.RoleName ?? "");

			// ✅ Ghi lịch sử đăng nhập
			var log = new AuditLog
			{
				UserId = user.UserId,
				Action = "Đăng nhập",
				Detail = $"Người dùng '{user.FullName}' (Email: {user.Email}, Role: {user.Role?.RoleName}) đã đăng nhập",
				Timestamp = DateTime.Now
			};
			_context.AuditLogs.Add(log);
			await _context.SaveChangesAsync();

			// 👉 Kiểm tra phân quyền
			if (user.Role?.RoleName == "Admin" ||
				user.Role?.RoleName == "HR" ||
				user.Role?.RoleName == "Manager")
			{
				return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
			}
			else
			{
				return RedirectToAction("Index", "Home");
			}
		}

		ViewBag.Error = "Sai email hoặc mật khẩu.";
		return View();
	}

	public IActionResult Logout()
	{
		HttpContext.Session.Clear();
		return RedirectToAction("Login");
	}

	public async Task<IActionResult> Index()
	{
		var userId = HttpContext.Session.GetInt32("UserId");
		if (userId == null) return RedirectToAction("Login");

		var user = await _context.Users
			.Include(u => u.Role)
			.FirstOrDefaultAsync(u => u.UserId == userId);

		if (user == null) return NotFound();

		// 👉 Lấy danh sách phòng ban bằng SQL
		var departments = await _context.Departments
			.FromSqlRaw(@"
            SELECT d.* FROM Departments d
            INNER JOIN User_Department ud ON d.DeptId = ud.DeptId
            WHERE ud.UserId = {0}", userId)
			.Select(d => d.DeptName)
			.ToListAsync();

		ViewBag.Departments = string.Join(", ", departments);

		return View(user);
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

		if (string.IsNullOrWhiteSpace(newPassword))
		{
			return BadRequest("Mật khẩu mới không được để trống.");
		}

		// 👉 Lưu trực tiếp mật khẩu mới vào DB
		user.PasswordHash = newPassword;

		// Reset lại token để không dùng lại được
		user.ResetToken = null;
		user.ResetTokenExpiry = null;

		await _context.SaveChangesAsync();

		TempData["Success"] = "Đổi mật khẩu thành công. Mời bạn đăng nhập.";
		return RedirectToAction("Login");
	}
	// ========== PROFILE ==========
	// Xem hồ sơ cá nhân
	public async Task<IActionResult> Profile()
	{
		var userId = HttpContext.Session.GetInt32("UserId");
		if (userId == null) return RedirectToAction("Login");

		var user = await _context.Users
			.Include(u => u.Role)
			.FirstOrDefaultAsync(u => u.UserId == userId);

		if (user == null) return NotFound();

		return View(user); // Trả về View và bind trực tiếp entity User
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Profile(User model)
	{
		var userId = HttpContext.Session.GetInt32("UserId");
		if (userId == null) return RedirectToAction("Login");

		var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
		if (user == null) return NotFound();

		// 👉 chỉ cho phép cập nhật thông tin cơ bản
		user.FullName = model.FullName;
		user.Email = model.Email;
		user.Level = model.Level;

		await _context.SaveChangesAsync();

		ViewBag.Message = "Cập nhật hồ sơ thành công!";
		return View(user);
	}


	// ========== LỊCH SỬ BÀI TEST ==========
	public async Task<IActionResult> History()
	{
		var userId = HttpContext.Session.GetInt32("UserId");
		if (userId == null) return RedirectToAction("Login");

		var histories = await _context.TestAttempts
			.Include(t => t.Test)
			.Where(t => t.UserId == userId)
			.OrderByDescending(t => t.StartTime)
			.ToListAsync();

		return View(histories);
	}
	// ========== CHANGE PASSWORD ==========
	[HttpGet]
	public IActionResult ChangePassword()
	{
		if (HttpContext.Session.GetInt32("UserId") == null)
			return RedirectToAction("Login");

		return View();
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
	{
		var userId = HttpContext.Session.GetInt32("UserId");
		if (userId == null) return RedirectToAction("Login");

		var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
		if (user == null) return NotFound();

		// 1. Kiểm tra mật khẩu cũ
		if (user.PasswordHash != oldPassword)
		{
			ViewBag.Error = "Mật khẩu cũ không đúng.";
			return View();
		}

		// 2. Kiểm tra mật khẩu mới
		if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
		{
			ViewBag.Error = "Mật khẩu mới phải có ít nhất 6 ký tự.";
			return View();
		}

		if (newPassword != confirmPassword)
		{
			ViewBag.Error = "Xác nhận mật khẩu không khớp.";
			return View();
		}

		// 3. Cập nhật mật khẩu
		user.PasswordHash = newPassword;
		await _context.SaveChangesAsync();

		TempData["Success"] = "Đổi mật khẩu thành công!";
		return RedirectToAction("Index"); // quay lại trang cá nhân
	}

	[HttpGet]
	public async Task<IActionResult> EditProfile()
	{
		var userId = HttpContext.Session.GetInt32("UserId");
		if (userId == null) return RedirectToAction("Login");

		var user = await _context.Users
			.Include(u => u.Role)
			.FirstOrDefaultAsync(u => u.UserId == userId);

		if (user == null) return NotFound();

		// Lấy tên phòng ban của user bằng join
		var departments = await _context.Departments
			.FromSqlRaw(@"
            SELECT d.* 
            FROM Departments d
            INNER JOIN User_Department ud ON d.DeptId = ud.DeptId
            WHERE ud.UserId = {0}", userId)
			.Select(d => d.DeptName)
			.ToListAsync();

		ViewBag.Departments = string.Join(", ", departments);

		return View(user);
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> EditProfile(User model)
	{
		var userId = HttpContext.Session.GetInt32("UserId");
		if (userId == null) return RedirectToAction("Login");

		var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
		if (user == null) return NotFound();

		// ✅ Chỉ cho sửa FullName và Email
		user.FullName = model.FullName;
		user.Email = model.Email;

		await _context.SaveChangesAsync();

		TempData["Success"] = "Cập nhật hồ sơ thành công!";
		return RedirectToAction("Index");
	}

}
