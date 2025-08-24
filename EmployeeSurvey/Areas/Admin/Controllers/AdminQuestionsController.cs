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
    public class AdminQuestionsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminQuestionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminQuestions
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Questions.Include(q => q.CreatedByNavigation).Include(q => q.Difficulty).Include(q => q.Skill);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Admin/AdminQuestions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.CreatedByNavigation)
                .Include(q => q.Difficulty)
                .Include(q => q.Skill)
                .FirstOrDefaultAsync(m => m.QuestionId == id);
            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

		// GET: Admin/AdminQuestions/Create
		[HttpGet]
		public IActionResult Create()
		{
			return View(new QuestionCreateViewModel
			{
				Content = string.Empty,        // gán mặc định
				QuestionType = string.Empty,   // gán mặc định
				Options = new List<QuestionOptionViewModel>
		{
			new QuestionOptionViewModel { Content = string.Empty, IsCorrect = false },
			new QuestionOptionViewModel { Content = string.Empty, IsCorrect = false },
			new QuestionOptionViewModel { Content = string.Empty, IsCorrect = false },
			new QuestionOptionViewModel { Content = string.Empty, IsCorrect = false }
		}
			});
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(QuestionCreateViewModel model)
		{
			if (ModelState.IsValid)
			{
				var question = new Question
				{
					Content = model.Content,
					QuestionType = model.QuestionType,
					SkillId = model.SkillID,
					DifficultyId = model.DifficultyID,
					CreatedBy = model.CreatedBy,
					CreatedDate = DateTime.Now,
					QuestionOptions = model.Options.Select(o => new QuestionOption
					{
						Content = o.Content,
						IsCorrect = o.IsCorrect
					}).ToList()
				};

				_context.Questions.Add(question);
				await _context.SaveChangesAsync();
				return RedirectToAction(nameof(Index));
			}

			// Nếu có lỗi validation thì quay lại view cùng model
			return View(model);
		}

		// GET: Admin/AdminQuestions/Edit/5
		public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions.FindAsync(id);
            if (question == null)
            {
                return NotFound();
            }
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", question.CreatedBy);
            ViewData["DifficultyId"] = new SelectList(_context.Difficulties, "DifficultyId", "DifficultyId", question.DifficultyId);
            ViewData["SkillId"] = new SelectList(_context.Skills, "SkillId", "SkillId", question.SkillId);
            return View(question);
        }

        // POST: Admin/AdminQuestions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("QuestionId,Content,QuestionType,SkillId,DifficultyId,CreatedBy,CreatedDate")] Question question)
        {
            if (id != question.QuestionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(question);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionExists(question.QuestionId))
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
            ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "UserId", question.CreatedBy);
            ViewData["DifficultyId"] = new SelectList(_context.Difficulties, "DifficultyId", "DifficultyId", question.DifficultyId);
            ViewData["SkillId"] = new SelectList(_context.Skills, "SkillId", "SkillId", question.SkillId);
            return View(question);
        }

        // GET: Admin/AdminQuestions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var question = await _context.Questions
                .Include(q => q.CreatedByNavigation)
                .Include(q => q.Difficulty)
                .Include(q => q.Skill)
                .FirstOrDefaultAsync(m => m.QuestionId == id);
            if (question == null)
            {
                return NotFound();
            }

            return View(question);
        }

        // POST: Admin/AdminQuestions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var question = await _context.Questions.FindAsync(id);
            if (question != null)
            {
                _context.Questions.Remove(question);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool QuestionExists(int id)
        {
            return _context.Questions.Any(e => e.QuestionId == id);
        }
    }
}
