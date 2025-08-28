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

        // GET: Admin/AdminAnswers
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.Answers.Include(a => a.Attempt).Include(a => a.Option).Include(a => a.Question);
            return View(await appDbContext.ToListAsync());
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
                    _context.Update(answer);
                    await _context.SaveChangesAsync();
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
