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
    public class AdminAnswersController : Controller
    {
        private readonly AppDbContext _context;

        public AdminAnswersController(AppDbContext context)
        {
            _context = context;
        }
		// GET: Admin chấm 1 câu trả lời tự luận
		public async Task<IActionResult> Grade(int id)
		{
			var answer = await _context.Answers
				.Include(a => a.Question)
				.Include(a => a.Attempt)
				.FirstOrDefaultAsync(a => a.AnswerId == id);

			if (answer == null)
				return NotFound();

			return View(answer); // Trả về form để admin nhập
		}

		// POST: Admin chấm xong
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Grade(int id, bool isCorrect, string? adminComment)
		{
			// Load answer + attempt + tất cả answers của attempt + question cho từng answer
			var answer = await _context.Answers
				.Include(a => a.Attempt)
					.ThenInclude(at => at.Answers)
						.ThenInclude(ans => ans.Question)
				.Include(a => a.Question)
				.FirstOrDefaultAsync(a => a.AnswerId == id);

			if (answer == null)
				return NotFound();

			// Update answer (admin grading)
			answer.IsCorrect = isCorrect;
			answer.IsGraded = true;
			answer.AdminComment = adminComment;

			// Cập nhật điểm tổng của Attempt
			var attempt = answer.Attempt;
			if (attempt != null)
			{
				int totalQuestions = attempt.Answers?.Count ?? 0;

				// dùng null-conditional để tránh NullReference nếu a.Question null (phòng thủ)
				int autoCorrect = attempt.Answers?.Count(a =>
					(a.Question?.QuestionType == "MCQ" || a.Question?.QuestionType == "TrueFalse")
					&& (a.IsCorrect ?? false)) ?? 0;

				int gradedTextCorrect = attempt.Answers?.Count(a =>
					a.Question?.QuestionType == "TextInput"
					&& a.IsGraded   // vì IsGraded là bool, không nullable
					&& (a.IsCorrect ?? false)) ?? 0;

				decimal tempScore = totalQuestions > 0
					? Math.Round(((decimal)(autoCorrect + gradedTextCorrect) / totalQuestions) * 10m, 2)
					: 0m;

				attempt.Score = tempScore;
			}

			// Lưu 1 lần (answer + attempt)
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Index));
		}


		// GET: Admin/AdminAnswers
		public async Task<IActionResult> Index()
		{
			var answers = await _context.Answers
				.Include(a => a.Attempt)
					.ThenInclude(at => at.Answers)
						.ThenInclude(ans => ans.Question) // load Question cho từng answer trong attempt
				.Include(a => a.Question) // load Question cho answer hiện tại
				.Where(a => a.Question.QuestionType == "TextInput") // chỉ lấy câu TextInput
				.ToListAsync();

			return View(answers);
		}



		// GET: Admin/AdminAnswers/Details/5
		public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var answer = await _context.Answers
                .Include(a => a.Attempt)
                .Include(a => a.Option)
                .Include(a => a.Question)
                .FirstOrDefaultAsync(m => m.AnswerId == id);
            if (answer == null)
            {
                return NotFound();
            }

            return View(answer);
        }

        // GET: Admin/AdminAnswers/Create
        public IActionResult Create()
        {
            ViewData["AttemptId"] = new SelectList(_context.TestAttempts, "AttemptId", "AttemptId");
            ViewData["OptionId"] = new SelectList(_context.QuestionOptions, "OptionId", "OptionId");
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "Content");
            return View();
        }

        // POST: Admin/AdminAnswers/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AnswerId,AttemptId,QuestionId,OptionId,AnswerText,IsCorrect")] Answer answer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(answer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AttemptId"] = new SelectList(_context.TestAttempts, "AttemptId", "AttemptId", answer.AttemptId);
            ViewData["OptionId"] = new SelectList(_context.QuestionOptions, "OptionId", "OptionId", answer.OptionId);
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "Content", answer.QuestionId);
            return View(answer);
        }

        // GET: Admin/AdminAnswers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var answer = await _context.Answers.FindAsync(id);
            if (answer == null)
            {
                return NotFound();
            }
            ViewData["AttemptId"] = new SelectList(_context.TestAttempts, "AttemptId", "AttemptId", answer.AttemptId);
            ViewData["OptionId"] = new SelectList(_context.QuestionOptions, "OptionId", "OptionId", answer.OptionId);
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "Content", answer.QuestionId);
            return View(answer);
        }

		// POST: Admin/AdminAnswers/Edit/5
		// To protect from overposting attacks, enable the specific properties you want to bind to.
		// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(int id, [Bind("AnswerId,AttemptId,QuestionId,OptionId,AnswerText,IsCorrect")] Answer answer)
		{
			if (id != answer.AnswerId)
			{
				return NotFound();
			}

			if (ModelState.IsValid)
			{
				try
				{
					// ✅ Lấy bản ghi Answer gốc từ DB
					var existingAnswer = await _context.Answers
						.AsNoTracking()
						.FirstOrDefaultAsync(a => a.AnswerId == id);

					if (existingAnswer == null)
						return NotFound();

					// ✅ Cập nhật lại Answer
					_context.Update(answer);
					await _context.SaveChangesAsync();

					// ✅ Sau khi cập nhật Answer thì tính lại điểm cho Attempt
					var attempt = await _context.TestAttempts
						.Include(t => t.Answers)
						.FirstOrDefaultAsync(t => t.AttemptId == answer.AttemptId);

					if (attempt != null)
					{
						// Tính lại điểm: đếm số câu có IsCorrect = true
						attempt.Score = attempt.Answers.Count(a => a.IsCorrect == true);

						// Đánh dấu là đã chấm
						answer.IsGraded = true;

						_context.Update(attempt);
						await _context.SaveChangesAsync();
					}
				}
				catch (DbUpdateConcurrencyException)
				{
					if (!AnswerExists(answer.AnswerId))
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

			ViewData["AttemptId"] = new SelectList(_context.TestAttempts, "AttemptId", "AttemptId", answer.AttemptId);
			ViewData["OptionId"] = new SelectList(_context.QuestionOptions, "OptionId", "OptionId", answer.OptionId);
			ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "Content", answer.QuestionId);
			return View(answer);
		}


		// GET: Admin/AdminAnswers/Delete/5
		public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var answer = await _context.Answers
                .Include(a => a.Attempt)
                .Include(a => a.Option)
                .Include(a => a.Question)
                .FirstOrDefaultAsync(m => m.AnswerId == id);
            if (answer == null)
            {
                return NotFound();
            }

            return View(answer);
        }

        // POST: Admin/AdminAnswers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var answer = await _context.Answers.FindAsync(id);
            if (answer != null)
            {
                _context.Answers.Remove(answer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AnswerExists(int id)
        {
            return _context.Answers.Any(e => e.AnswerId == id);
        }
    }
}
