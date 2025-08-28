using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EmployeeSurvey.Models;

namespace EmployeeSurvey.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class AdminTestsController : Controller
	{
		private readonly AppDbContext _context;

		public AdminTestsController(AppDbContext context)
		{
			_context = context;
		}

		// GET: Admin/AdminTests
		public async Task<IActionResult> Index()
		{
			var appDbContext = _context.Tests.Include(t => t.CreatedByNavigation);
			return View(await appDbContext.ToListAsync());
		}

		// GET: Admin/AdminTests/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var test = await _context.Tests
				.Include(t => t.CreatedByNavigation)
				.Include(t => t.TestQuestions)
					.ThenInclude(tq => tq.Question)
				.FirstOrDefaultAsync(m => m.TestId == id);

			if (test == null) return NotFound();

			return View(test);
		}

		// GET: Admin/AdminTests/Create
		// GET: Create Test + Assign
		public IActionResult Create()
		{
			ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName");
			ViewData["Questions"] = _context.Questions.ToList();
			ViewBag.Departments = _context.Departments.Select(d => new SelectListItem { Value = d.DeptId.ToString(), Text = d.DeptName }).ToList();
			ViewBag.Roles = _context.Users.Select(u => u.Role.RoleName).Distinct().Select(r => new SelectListItem { Value = r, Text = r }).ToList();
			ViewBag.Levels = _context.Users.Select(u => u.Level).Distinct().Select(l => new SelectListItem { Value = l, Text = l }).ToList();

			return View();
		}

		// POST: Create Test + Assign
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(
			[Bind("TestId,Title,Description,Duration,PassScore,CreatedBy,CreatedDate,RandomQuestionCount")] Test test,
			int[] selectedQuestions,
			int[] selectedDeptIDs,
			string[] selectedRoles,
			string[] selectedLevels)
		{
			if (!ModelState.IsValid)
			{
				// Load lại dropdown nếu ModelState invalid
				ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName", test.CreatedBy);
				ViewData["Questions"] = _context.Questions.ToList();
				ViewBag.Departments = _context.Departments.Select(d => new SelectListItem { Value = d.DeptId.ToString(), Text = d.DeptName }).ToList();
				ViewBag.Roles = _context.Users.Select(u => u.Role.RoleName).Distinct().Select(r => new SelectListItem { Value = r, Text = r }).ToList();
				ViewBag.Levels = _context.Users.Select(u => u.Level).Distinct().Select(l => new SelectListItem { Value = l, Text = l }).ToList();
				return View(test);
			}

			// 1️⃣ Thêm Test
			_context.Tests.Add(test);
			await _context.SaveChangesAsync();

			// 2️⃣ Thêm TestQuestions thủ công + ngẫu nhiên
			if (selectedQuestions != null && selectedQuestions.Length > 0)
			{
				int order = 1;
				foreach (var qid in selectedQuestions)
				{
					_context.TestQuestions.Add(new TestQuestion { TestId = test.TestId, QuestionId = qid, OrderNo = order++ });
				}
			}
			if (test.RandomQuestionCount.HasValue && test.RandomQuestionCount.Value > 0)
			{
				var random = new Random();
				var allQuestions = await _context.Questions.ToListAsync();
				var randomQuestions = allQuestions.OrderBy(q => random.Next()).Take(test.RandomQuestionCount.Value).ToList();
				int order = 1;
				foreach (var q in randomQuestions)
				{
					_context.TestQuestions.Add(new TestQuestion { TestId = test.TestId, QuestionId = q.QuestionId, OrderNo = order++ });
				}
			}
			await _context.SaveChangesAsync();

			// 3️⃣ Phân công bài test
			var usersQuery = _context.Users.Include(u => u.Depts).Include(u => u.Role).AsQueryable();
			if (selectedDeptIDs?.Any() == true)
				usersQuery = usersQuery.Where(u => u.Depts.Any(d => selectedDeptIDs.Contains(d.DeptId)));
			if (selectedRoles?.Any() == true)
				usersQuery = usersQuery.Where(u => selectedRoles.Contains(u.Role.RoleName));
			if (selectedLevels?.Any() == true)
				usersQuery = usersQuery.Where(u => selectedLevels.Contains(u.Level));

			var users = await usersQuery.ToListAsync();
			foreach (var user in users)
			{
				foreach (var dept in user.Depts)
				{
					if (selectedDeptIDs == null || selectedDeptIDs.Contains(dept.DeptId))
					{
						if (!_context.Assignments.Any(a => a.TestId == test.TestId && a.UserId == user.UserId && a.DeptId == dept.DeptId))
						{
							_context.Assignments.Add(new Assignment { TestId = test.TestId, UserId = user.UserId, DeptId = dept.DeptId, AssignedDate = DateTime.Now });
						}
					}
				}
			}
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Index));
		}


		// GET: Admin/AdminTests/Edit/5
		public async Task<IActionResult> Edit(int? id)
		{
			if (id == null) return NotFound();

			var test = await _context.Tests
				.Include(t => t.TestQuestions)
				.FirstOrDefaultAsync(t => t.TestId == id);

			if (test == null) return NotFound();

			ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName", test.CreatedBy);
			ViewData["Questions"] = _context.Questions.ToList();
			ViewData["SelectedQuestions"] = test.TestQuestions.Select(tq => tq.QuestionId).ToList();

			return View(test);
		}

		// POST: Admin/AdminTests/Edit/5
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id,[Bind("TestId,Title,Description,Duration,PassScore,CreatedBy,CreatedDate,RandomQuestionCount")] Test test,int[] selectedQuestions)
		{
			if (id != test.TestId) return NotFound();

			if (ModelState.IsValid)
			{
				try
				{
					// Update Test
					_context.Update(test);
					await _context.SaveChangesAsync();

					// Xóa TestQuestions cũ
					var oldQuestions = _context.TestQuestions.Where(tq => tq.TestId == test.TestId);
					_context.TestQuestions.RemoveRange(oldQuestions);

					// Nếu chọn câu hỏi thủ công
					if (selectedQuestions != null && selectedQuestions.Length > 0)
					{
						int order = 1;
						foreach (var qid in selectedQuestions)
						{
							_context.TestQuestions.Add(new TestQuestion
							{
								TestId = test.TestId,
								QuestionId = qid,
								OrderNo = order++
							});
						}
					}

					// Nếu chọn câu hỏi ngẫu nhiên
					if (test.RandomQuestionCount.HasValue && test.RandomQuestionCount.Value > 0)
					{
						var random = new Random();
						var allQuestions = await _context.Questions.ToListAsync();

						var randomQuestions = allQuestions
							.OrderBy(q => random.Next())
							.Take(test.RandomQuestionCount.Value)
							.ToList();

						int order = 1;
						foreach (var q in randomQuestions)
						{
							_context.TestQuestions.Add(new TestQuestion
							{
								TestId = test.TestId,
								QuestionId = q.QuestionId,
								OrderNo = order++
							});
						}
					}

					await _context.SaveChangesAsync();
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!TestExists(test.TestId)) return NotFound();
					else throw;
				}

				return RedirectToAction(nameof(Index));
			}

			ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName", test.CreatedBy);
			ViewData["Questions"] = _context.Questions.ToList();
			return View(test);
		}

		// GET: Admin/AdminTests/Delete/5
		public async Task<IActionResult> Delete(int? id)
		{
			if (id == null) return NotFound();

			var test = await _context.Tests
				.Include(t => t.CreatedByNavigation)
				.Include(t => t.TestQuestions)
					.ThenInclude(tq => tq.Question)
				.FirstOrDefaultAsync(m => m.TestId == id);

			if (test == null) return NotFound();

			return View(test);
		}

		// POST: Admin/AdminTests/Delete/5
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			var test = await _context.Tests
				.Include(t => t.TestQuestions)
				.FirstOrDefaultAsync(t => t.TestId == id);

			if (test != null)
			{
				// Xóa TestQuestions trước
				_context.TestQuestions.RemoveRange(test.TestQuestions);

				// Xóa Test
				_context.Tests.Remove(test);

				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(Index));
		}

		private bool TestExists(int id)
		{
			return _context.Tests.Any(e => e.TestId == id);
		}
	}
}
