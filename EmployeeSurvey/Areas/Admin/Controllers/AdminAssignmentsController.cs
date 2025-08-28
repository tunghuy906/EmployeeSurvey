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
	public class AdminAssignmentsController : Controller
	{
		private readonly AppDbContext _context;

		public AdminAssignmentsController(AppDbContext context)
		{
			_context = context;
		}

		// ================== DANH SÁCH ==================
		public async Task<IActionResult> Index()
		{
			var assignments = _context.Assignments
				.Include(a => a.Dept)
				.Include(a => a.Test)
				.Include(a => a.User);

			return View(await assignments.ToListAsync());
		}

		// ================== PHÂN CÔNG THEO NHÓM ==================
		[HttpGet]
		public IActionResult AssignGroup()
		{
			ViewData["TestID"] = new SelectList(_context.Tests, "TestId", "Title");
			ViewData["RoleID"] = new SelectList(_context.Roles, "RoleId", "RoleName");
			ViewData["DeptID"] = new SelectList(_context.Departments, "DeptId", "DeptName");

			// lấy Level distinct từ bảng Users
			ViewBag.Levels = _context.Users
				.Where(u => u.Level != null)
				.Select(u => u.Level)
				.Distinct()
				.ToList();

			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult AssignGroup(int TestID, int? RoleID, int? DeptID, string Level, DateTime? Deadline)
		{
			var users = _context.Users
				.Include(u => u.Depts) // load phòng ban
				.AsQueryable();

			if (RoleID.HasValue)
				users = users.Where(u => u.RoleId == RoleID.Value);

			if (!string.IsNullOrEmpty(Level))
				users = users.Where(u => u.Level == Level);

			if (DeptID.HasValue)
				users = users.Where(u => u.Depts.Any(d => d.DeptId == DeptID.Value));

			var userList = users.ToList();

			foreach (var u in userList)
			{
				var assign = new Assignment
				{
					TestId = TestID,
					UserId = u.UserId,
					DeptId = DeptID, // nếu muốn lấy deptId theo filter
					Deadline = Deadline,
					AssignedDate = DateTime.Now
				};
				_context.Assignments.Add(assign);
			}

			_context.SaveChanges();
			TempData["Message"] = $"Đã phân công test cho {userList.Count} nhân viên.";
			return RedirectToAction(nameof(Index));
		}

		// ================== CRUD ==================
		public IActionResult Create()
		{
			LoadDropdowns();
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create([Bind("AssignId,TestId,UserId,DeptId,AssignedDate,Deadline")] Assignment assignment)
		{
			if (ModelState.IsValid)
			{
				_context.Add(assignment);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}
			LoadDropdowns(assignment);
			return View(assignment);
		}

		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();

			var assignment = await _context.Assignments.FindAsync(id);
			if (assignment == null) return NotFound();

			LoadDropdowns(assignment);
			return View(assignment);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("AssignId,TestId,UserId,DeptId,AssignedDate,Deadline")] Assignment assignment)
		{
			if (id != assignment.AssignId) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					_context.Update(assignment);
					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!AssignmentExists(assignment.AssignId))
						return NotFound();
					else
						throw;
				}
				return RedirectToAction(nameof(Index));
			}

			LoadDropdowns(assignment);
			return View(assignment);
		}

		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var assignment = await _context.Assignments
				.Include(a => a.Dept)
				.Include(a => a.Test)
				.Include(a => a.User)
				.FirstOrDefaultAsync(m => m.AssignId == id);

			if (assignment == null) return NotFound();
			return View(assignment);
		}

		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();

			var assignment = await _context.Assignments
				.Include(a => a.Dept)
				.Include(a => a.Test)
				.Include(a => a.User)
				.FirstOrDefaultAsync(m => m.AssignId == id);

			if (assignment == null) return NotFound();
			return View(assignment);
		}

		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var assignment = await _context.Assignments.FindAsync(id);
			if (assignment != null) _context.Assignments.Remove(assignment);

			await _context.SaveChangesAsync();
			return RedirectToAction(nameof(Index));
		}

		private bool AssignmentExists(int id) =>
			_context.Assignments.Any(e => e.AssignId == id);

		// ================== HELPER ==================
		private void LoadDropdowns(Assignment assignment = null)
		{
			ViewData["DeptId"] = new SelectList(_context.Departments, "DeptId", "DeptName", assignment?.DeptId);
			ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "Title", assignment?.TestId);
			ViewData["UserId"] = new SelectList(_context.Users, "UserId", "FullName", assignment?.UserId);
		}
	}
}
