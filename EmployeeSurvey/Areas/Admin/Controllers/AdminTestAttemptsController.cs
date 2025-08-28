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
    public class AdminTestAttemptsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminTestAttemptsController(AppDbContext context)
        {
            _context = context;
        }

		// GET: Admin/AdminTestAttempts
		// ✅ 1. Danh sách các lần thi
		public async Task<IActionResult> Index()
		{
			var attempts = await _context.TestAttempts
				.Include(t => t.User)
				.Include(t => t.Test)
				.OrderByDescending(t => t.StartTime)
				.ToListAsync();

			return View(attempts);
		}

		// ✅ 2. Xem chi tiết 1 lần thi (Answers)
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null) return NotFound();

			var attempt = await _context.TestAttempts
				.Include(t => t.User)
				.Include(t => t.Test)
				.Include(t => t.Answers)
					.ThenInclude(a => a.Question)
				.Include(t => t.Answers)
					.ThenInclude(a => a.Option)
				.FirstOrDefaultAsync(m => m.AttemptId == id);

			if (attempt == null) return NotFound();

			return View(attempt);
		}

		// ✅ 3. Chấm thủ công (chỉ cho câu tự luận)
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> GradeAnswer(int answerId, bool isCorrect)
		{
			var answer = await _context.Answers
				.Include(a => a.Attempt)
				.ThenInclude(at => at.Answers)
				.FirstOrDefaultAsync(a => a.AnswerId == answerId);

			if (answer == null) return NotFound();

			// Cập nhật kết quả cho câu trả lời
			answer.IsCorrect = isCorrect;
			_context.Update(answer);
			await _context.SaveChangesAsync();

			// Tính lại tổng điểm của Attempt
			var attempt = answer.Attempt;
			int total = attempt.Answers.Count;
			int correct = attempt.Answers.Count(a => a.IsCorrect == true);

			attempt.Score = Math.Round((decimal)correct / total * 100, 2);
			_context.Update(attempt);
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Details), new { id = attempt.AttemptId });
		}

		// ✅ 4. Thêm Feedback cho ứng viên
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddFeedback(int attemptId, string content)
		{
			var attempt = await _context.TestAttempts
				.Include(t => t.User)
				.FirstOrDefaultAsync(t => t.AttemptId == attemptId);

			if (attempt == null) return NotFound();

			var feedback = new Feedback
			{
				UserId = attempt.UserId,
				TestId = attempt.TestId,
				Content = content,
				CreatedDate = DateTime.Now
			};

			_context.Feedbacks.Add(feedback);
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Details), new { id = attempt.AttemptId });
		}

		// GET: Admin/AdminTestAttempts/Create
		public IActionResult Create()
        {
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: Admin/AdminTestAttempts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AttemptId,UserId,TestId,StartTime,EndTime,Score,Status")] TestAttempt testAttempt)
        {
            if (ModelState.IsValid)
            {
                _context.Add(testAttempt);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId", testAttempt.TestId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", testAttempt.UserId);
            return View(testAttempt);
        }

        // GET: Admin/AdminTestAttempts/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testAttempt = await _context.TestAttempts.FindAsync(id);
            if (testAttempt == null)
            {
                return NotFound();
            }
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId", testAttempt.TestId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", testAttempt.UserId);
            return View(testAttempt);
        }

        // POST: Admin/AdminTestAttempts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AttemptId,UserId,TestId,StartTime,EndTime,Score,Status")] TestAttempt testAttempt)
        {
            if (id != testAttempt.AttemptId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(testAttempt);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TestAttemptExists(testAttempt.AttemptId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["TestId"] = new SelectList(_context.Tests, "TestId", "TestId", testAttempt.TestId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", testAttempt.UserId);
            return View(testAttempt);
        }

        // GET: Admin/AdminTestAttempts/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var testAttempt = await _context.TestAttempts
                .Include(t => t.Test)
                .Include(t => t.User)
                .FirstOrDefaultAsync(m => m.AttemptId == id);
            if (testAttempt == null)
            {
                return NotFound();
            }

            return View(testAttempt);
        }

        // POST: Admin/AdminTestAttempts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var testAttempt = await _context.TestAttempts.FindAsync(id);
            if (testAttempt != null)
            {
                _context.TestAttempts.Remove(testAttempt);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TestAttemptExists(int id)
        {
            return _context.TestAttempts.Any(e => e.AttemptId == id);
        }
    }
}
