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
    public class AdminUserSkillsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminUserSkillsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/AdminUserSkills
        public async Task<IActionResult> Index()
        {
            var appDbContext = _context.UserSkills.Include(u => u.Skill).Include(u => u.User);
            return View(await appDbContext.ToListAsync());
        }
		[HttpGet]
		public IActionResult Grade(int? userId)
		{
			ViewBag.AllUsers = _context.Users.ToList();

			if (!userId.HasValue)
				return View();

			var user = _context.Users
				.Include(u => u.UserSkills)      // phải include
				.ThenInclude(us => us.Skill)     // include Skill để lấy SkillName
				.FirstOrDefault(u => u.UserId == userId.Value);

			if (user == null) return NotFound();

			return View(user);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Grade(int userId, List<UserSkills> userSkills)
		{
			foreach (var skill in userSkills)
			{
				var dbSkill = _context.UserSkills.FirstOrDefault(us => us.Id == skill.Id);
				if (dbSkill != null)
				{
					dbSkill.Score = skill.Score;
				}
			}

			_context.SaveChanges();

			return RedirectToAction("Grade", new { userId = userId });
		}


		// GET: Admin/AdminUserSkills/Details/5
		public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userSkill = await _context.UserSkills
                .Include(u => u.Skill)
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userSkill == null)
            {
                return NotFound();
            }

            return View(userSkill);
        }

        // GET: Admin/AdminUserSkills/Create
        public IActionResult Create()
        {
            ViewData["SkillId"] = new SelectList(_context.Skills, "SkillId", "SkillId");
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId");
            return View();
        }

        // POST: Admin/AdminUserSkills/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserSkillId,UserId,SkillId,Score")] UserSkills userSkills)
        {
            if (ModelState.IsValid)
            {
                _context.Add(userSkills);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["SkillId"] = new SelectList(_context.Skills, "SkillId", "SkillId", userSkills.SkillId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", userSkills.UserId);
            return View(userSkills);
        }

        // GET: Admin/AdminUserSkills/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userSkill = await _context.UserSkills.FindAsync(id);
            if (userSkill == null)
            {
                return NotFound();
            }
            ViewData["SkillId"] = new SelectList(_context.Skills, "SkillId", "SkillId", userSkill.SkillId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", userSkill.UserId);
            return View(userSkill);
        }

        // POST: Admin/AdminUserSkills/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserSkillId,UserId,SkillId,Score")] UserSkills userSkills)
        {
            if (id != userSkills.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(userSkills);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserSkillExists(userSkills.Id))
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
            ViewData["SkillId"] = new SelectList(_context.Skills, "SkillId", "SkillId", userSkills.SkillId);
            ViewData["UserId"] = new SelectList(_context.Users, "UserId", "UserId", userSkills.UserId);
            return View(userSkills);
        }

        // GET: Admin/AdminUserSkills/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userSkill = await _context.UserSkills
                .Include(u => u.Skill)
                .Include(u => u.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (userSkill == null)
            {
                return NotFound();
            }

            return View(userSkill);
        }

        // POST: Admin/AdminUserSkills/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userSkill = await _context.UserSkills.FindAsync(id);
            if (userSkill != null)
            {
                _context.UserSkills.Remove(userSkill);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserSkillExists(int id)
        {
            return _context.UserSkills.Any(e => e.Id == id);
        }
    }
}
