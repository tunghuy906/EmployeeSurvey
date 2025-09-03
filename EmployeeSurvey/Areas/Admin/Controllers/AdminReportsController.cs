using EmployeeSurvey.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Linq;

namespace EmployeeSurvey.Admin.Areas.Admin.Controllers
{
	[Area("Admin")]
	public class AdminReportsController : Controller
	{
		private readonly AppDbContext _context;

		public AdminReportsController(AppDbContext context)
		{
			_context = context;
		}
		public IActionResult Index()
		{
			ViewBag.TotalUsers = _context.Users.Count();
			ViewBag.TotalDepartments = _context.Departments.Count();
			ViewBag.TotalTests = _context.Tests.Count();

			return View();
		}
		// Báo cáo theo Role
		public IActionResult ByRole()
		{
			var report = _context.Users
				.Include(u => u.Role)
				.GroupBy(u => u.Role.RoleName)
				.Select(g => new
				{
					RoleName = g.Key,
					TotalUsers = g.Count()
				})
				.ToList();

			ViewBag.ReportByRole = report;

			return View();
		}
		public IActionResult ByLevel()
		{
			var reportByLevel = _context.Users
				.GroupBy(u => u.Level)
				.Select(g => new
				{
					LevelName = g.Key,
					TotalUsers = g.Count()
				})
				.ToList();

			ViewBag.ReportByLevel = reportByLevel;
			return View();
		}
		public IActionResult PersonalSkill(int? userId)
		{
			if (userId == null)
				userId = _context.Users.Select(u => u.UserId).FirstOrDefault();

			var user = _context.Users.FirstOrDefault(u => u.UserId == userId);
			if (user == null)
				return NotFound("Không tìm thấy user.");

			// Điểm năng lực cá nhân
			var skillsReport = _context.UserSkills
				.Where(us => us.UserId == userId)
				.Select(us => new
				{
					SkillName = us.Skill.SkillName,
					Score = us.Score
				})
				.ToList();

			// Điểm trung bình công ty (theo từng kỹ năng)
			var avgReport = _context.UserSkills
				.GroupBy(us => us.Skill.SkillName)
				.Select(g => new
				{
					SkillName = g.Key,
					AvgScore = g.Average(x => x.Score)
				})
				.ToList();

			// Join để có cùng danh sách skill (tránh lệch)
			var mergedReport = (from s in skillsReport
								join a in avgReport on s.SkillName equals a.SkillName
								select new
								{
									SkillName = s.SkillName,
									UserScore = s.Score,
									AvgScore = a.AvgScore
								}).ToList();

			ViewBag.User = user;
			ViewBag.MergedJson = JsonConvert.SerializeObject(mergedReport);
			ViewBag.Users = _context.Users.ToList();

			return View();
		}

	}
}
