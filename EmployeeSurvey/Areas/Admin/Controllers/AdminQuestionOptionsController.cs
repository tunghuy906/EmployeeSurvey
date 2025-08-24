﻿using System;
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
    public class AdminQuestionOptionsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminQuestionOptionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminQuestionOptions
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.QuestionOptions.Include(q => q.Question);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Admin/AdminQuestionOptions/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var questionOption = await _context.QuestionOptions
                .Include(q => q.Question)
                .FirstOrDefaultAsync(m => m.OptionId == id);
            if (questionOption == null)
            {
                return NotFound();
            }

            return View(questionOption);
        }

        // GET: Admin/AdminQuestionOptions/Create
        public IActionResult Create()
        {
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "QuestionId");
            return View();
        }

        // POST: Admin/AdminQuestionOptions/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OptionId,QuestionId,Content,IsCorrect")] QuestionOption questionOption)
        {
            if (ModelState.IsValid)
            {
                _context.Add(questionOption);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "QuestionId", questionOption.QuestionId);
            return View(questionOption);
        }

        // GET: Admin/AdminQuestionOptions/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var questionOption = await _context.QuestionOptions.FindAsync(id);
            if (questionOption == null)
            {
                return NotFound();
            }
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "QuestionId", questionOption.QuestionId);
            return View(questionOption);
        }

        // POST: Admin/AdminQuestionOptions/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("OptionId,QuestionId,Content,IsCorrect")] QuestionOption questionOption)
        {
            if (id != questionOption.OptionId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(questionOption);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!QuestionOptionExists(questionOption.OptionId))
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
            ViewData["QuestionId"] = new SelectList(_context.Questions, "QuestionId", "QuestionId", questionOption.QuestionId);
            return View(questionOption);
        }

        // GET: Admin/AdminQuestionOptions/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var questionOption = await _context.QuestionOptions
                .Include(q => q.Question)
                .FirstOrDefaultAsync(m => m.OptionId == id);
            if (questionOption == null)
            {
                return NotFound();
            }

            return View(questionOption);
        }

        // POST: Admin/AdminQuestionOptions/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var questionOption = await _context.QuestionOptions.FindAsync(id);
            if (questionOption != null)
            {
                _context.QuestionOptions.Remove(questionOption);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool QuestionOptionExists(int id)
        {
            return _context.QuestionOptions.Any(e => e.OptionId == id);
        }
    }
}
