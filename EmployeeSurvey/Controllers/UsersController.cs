using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EmployeeSurvey.Models;

namespace EmployeeSurvey.Controllers
{
	public class UsersController : Controller
	{
		private readonly AppDbContext _context;

		public UsersController(AppDbContext context)
		{
			_context = context;
		}

		// GET: Users
		public async Task<IActionResult> Index()
		{
			var users = _context.Users.Include(u => u.Role);
			return View(await users.ToListAsync());
		}

		// GET: Users/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var user = await _context.Users
				.Include(u => u.Role)
				.FirstOrDefaultAsync(m => m.UserId == id);

			if (user == null) return NotFound();
			return View(user);
		}

		// GET: Users/Create
		public IActionResult Create()
		{
			ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleId");
			return View();
		}

		// POST: Users/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(
			[Bind("FullName,Email,PasswordHash,RoleId,Level,Status,ResetToken,ResetTokenExpiry")] User user)
		{
			if (ModelState.IsValid)
			{
				_context.Add(user);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}

			// Đưa lỗi ra để nhìn thấy ngay tại View
			ViewBag.Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

			ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleId", user.RoleId);
			return View(user);
		}

		// GET: Users/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();

			var user = await _context.Users.FindAsync(id);
			if (user == null) return NotFound();

			ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleId", user.RoleId);
			return View(user);
		}

		// POST: Users/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id,
			[Bind("UserId,FullName,Email,PasswordHash,RoleId,Level,Status,ResetToken,ResetTokenExpiry")] User user)
		{
			if (id != user.UserId) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(user);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!_context.Users.Any(e => e.UserId == user.UserId)) return NotFound();
					throw;
				}
				return RedirectToAction(nameof(Index));
			}

			ViewBag.Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();

			ViewData["RoleId"] = new SelectList(_context.Roles, "RoleId", "RoleId", user.RoleId);
			return View(user);
		}

		// GET: Users/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();

			var user = await _context.Users
				.Include(u => u.Role)
				.FirstOrDefaultAsync(m => m.UserId == id);

			if (user == null) return NotFound();
			return View(user);
		}

		// POST: Users/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var user = await _context.Users.FindAsync(id);
			if (user != null)
			{
				_context.Users.Remove(user);
				await _context.SaveChangesAsync();
			}
			return RedirectToAction(nameof(Index));
		}
	}
}
