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
    public class AdminDepartmentsController : Controller
    {
        private readonly AppDbContext _context;

        public AdminDepartmentsController(AppDbContext context)
        {
            _context = context;
        }

		// GET: Admin/AdminDepartments
		public async Task<IActionResult> Index()
		{
			var departments = await _context.Departments
				.Include(d => d.Users) // Include nhân viên trong phòng ban
				.ToListAsync();

			return View(departments);
		}

		// GET: Admin/AdminDepartments/Details/5
		public async Task<IActionResult> Details(int? id)
		{
			if (id == null)
			{
				return NotFound();
			}

			var department = await _context.Departments
				.Include(d => d.Users) // lấy luôn danh sách nhân viên
				.FirstOrDefaultAsync(m => m.DeptId == id);

			if (department == null)
			{
				return NotFound();
			}

			// Lấy toàn bộ user để cho dropdown
			ViewBag.AllUsers = await _context.Users.ToListAsync();

			return View(department);
		}
		[HttpGet]
		public async Task<IActionResult> AssignToDepartment(int deptId)
		{
			var department = await _context.Departments
				.Include(d => d.Users)
				.FirstOrDefaultAsync(d => d.DeptId == deptId);

			if (department == null)
			{
				return NotFound();
			}

			// Lấy danh sách user chưa nằm trong phòng ban
			var allUsers = await _context.Users.ToListAsync();
			var usersNotInDept = allUsers
				.Where(u => !department.Users.Any(d => d.UserId == u.UserId))
				.ToList();

			ViewBag.Users = usersNotInDept;

			return View(department); // Trả về view AssignToDepartment.cshtml
		}

		// POST: Admin/AdminDepartments/AddUser
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> AddUser(int deptId, int userId)
		{
			var department = await _context.Departments
				.Include(d => d.Users)
				.FirstOrDefaultAsync(d => d.DeptId == deptId);

			var user = await _context.Users.FindAsync(userId);

			if (department != null && user != null && !department.Users.Contains(user))
			{
				department.Users.Add(user);
				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(Index));
		}

		// POST: Admin/AdminDepartments/RemoveUser
		[HttpPost]
		public async Task<IActionResult> RemoveUser(int deptId, int userId)
		{
			var department = await _context.Departments
				.Include(d => d.Users)
				.FirstOrDefaultAsync(d => d.DeptId == deptId);

			if (department == null)
			{
				return NotFound();
			}

			var user = department.Users.FirstOrDefault(u => u.UserId == userId);
			if (user != null)
			{
				department.Users.Remove(user);
				await _context.SaveChangesAsync();
			}

			return RedirectToAction("AssignToDepartment", new { deptId = deptId });
		}
		// GET: Admin/AdminDepartments/Create
		public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/AdminDepartments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("DeptId,DeptName,Description")] Department department)
        {
            if (ModelState.IsValid)
            {
                _context.Add(department);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(department);
        }

        // GET: Admin/AdminDepartments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments.FindAsync(id);
            if (department == null)
            {
                return NotFound();
            }
            return View(department);
        }

        // POST: Admin/AdminDepartments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("DeptId,DeptName,Description")] Department department)
        {
            if (id != department.DeptId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(department);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DepartmentExists(department.DeptId))
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
            return View(department);
        }

        // GET: Admin/AdminDepartments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var department = await _context.Departments
                .FirstOrDefaultAsync(m => m.DeptId == id);
            if (department == null)
            {
                return NotFound();
            }

            return View(department);
        }

        // POST: Admin/AdminDepartments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var department = await _context.Departments.FindAsync(id);
            if (department != null)
            {
                _context.Departments.Remove(department);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool DepartmentExists(int id)
        {
            return _context.Departments.Any(e => e.DeptId == id);
        }
    }
}
