using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EmployeeSurvey.Models;
using Microsoft.AspNetCore.Http;

namespace EmployeeSurvey.Controllers
{
	public class MytestsController : Controller
	{
		private readonly AppDbContext _context;

		public MytestsController(AppDbContext context)
		{
			_context = context;
		}

		// ========== DANH SÁCH BÀI TEST ĐÃ ĐƯỢC GIAO ==========
		public IActionResult Index()
		{
			var userId = GetCurrentUserId();
			if (userId == null) return RedirectToAction("Login", "Home");

			// Lấy danh sách bài test được giao
			var assignments = _context.Assignments
				.Include(a => a.Test)
					.ThenInclude(t => t.TestAttempts.Where(ta => ta.UserId == userId))
				.Where(a => a.UserId == userId)
				.ToList();

			// Tạo danh sách TestAttempt "ảo" để bind vào View
			var model = assignments.Select(a =>
			{
				var attempt = a.Test.TestAttempts.FirstOrDefault(); // lấy attempt nếu đã làm
				if (attempt == null)
				{
					// nếu chưa làm, tạo dummy object để View dùng
					attempt = new TestAttempt
					{
						TestId = a.TestId,
						Test = a.Test,
						Status = "NotStarted"
					};
				}
				return attempt;
			}).ToList();

			return View(model);
		}


		// ========== LÀM BÀI TEST ==========
		public IActionResult Take(int id) // id = TestID
		{
			var userId = GetCurrentUserId();
			if (userId == null) return RedirectToAction("Login", "Home");

			// Kiểm tra bài test tồn tại
			var test = _context.Tests
				.Include(t => t.TestQuestions)
					.ThenInclude(tq => tq.Question)
						.ThenInclude(q => q.QuestionOptions)
				.FirstOrDefault(t => t.TestId == id);

			if (test == null) return NotFound();

			// Kiểm tra user được assign bài test này
			var assignment = _context.Assignments
				.FirstOrDefault(a => a.TestId == id && a.UserId == userId);

			if (assignment == null) return Forbid();

			return View(test);
		}

		// ========== SUBMIT KẾT QUẢ ==========
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Submit(int testId, List<Answer> answers, int timeSpent)
		{
			var userId = GetCurrentUserId();
			if (userId == null) return RedirectToAction("Login", "Home");

			// Lấy attempt đang làm
			var attempt = _context.TestAttempts
				.FirstOrDefault(a => a.TestId == testId && a.UserId == userId && a.Status == "InProgress");

			if (attempt == null)
			{
				// nếu chưa có thì tạo mới (fallback)
				attempt = new TestAttempt
				{
					UserId = userId.Value,
					TestId = testId,
					StartTime = DateTime.Now,
					Status = "InProgress"
				};
				_context.TestAttempts.Add(attempt);
				_context.SaveChanges();
			}

			// Kết thúc attempt
			attempt.EndTime = DateTime.Now;
			attempt.Status = "Submitted";
			// Nếu muốn lưu thời gian làm bài
			if (timeSpent > 0)
			{
				var start = attempt.StartTime ?? DateTime.Now;
				attempt.EndTime = start.AddSeconds(timeSpent);
			}

			int correct = 0;
			int total = _context.TestQuestions.Count(tq => tq.TestId == testId);

			foreach (var ans in answers)
			{
				if (ans.QuestionId <= 0) continue;

				ans.AttemptId = attempt.AttemptId;

				var question = _context.Questions
					.Include(q => q.QuestionOptions)
					.FirstOrDefault(q => q.QuestionId == ans.QuestionId);

				if (question != null)
				{
					switch (question.QuestionType)
					{
						case "MCQ":
						case "TrueFalse":
							var selectedOption = _context.QuestionOptions
								.FirstOrDefault(o => o.OptionId == ans.OptionId);
							if (selectedOption != null && selectedOption.IsCorrect == true)
							{
								ans.IsCorrect = true;
								correct++;
							}
							else ans.IsCorrect = false;
							break;

						case "MultipleResponse":
							var correctOptions = question.QuestionOptions
								.Where(o => o.IsCorrect == true)
								.Select(o => o.OptionId)
								.ToList();

							var userOptions = answers
								.Where(a => a.QuestionId == question.QuestionId && a.OptionId.HasValue)
								.Select(a => a.OptionId.Value)   // chỉ lấy giá trị khi chắc chắn có
								.ToList();

							// so sánh 2 tập hợp
							if (correctOptions.Count == userOptions.Count &&
								!correctOptions.Except(userOptions).Any())
							{
								ans.IsCorrect = true;
								correct++;
							}
							else ans.IsCorrect = false;
							break;

						case "TextInput":
							// chưa auto-chấm
							ans.IsCorrect = false;
							break;
					}
				}

				_context.Answers.Add(ans);
			}

			// Tính điểm
			decimal score = (total > 0) ? Math.Round(10.0m * correct / total, 2) : 0;
			attempt.Score = score;
			attempt.Status = "Completed";

			_context.Update(attempt);
			_context.SaveChanges();

			return RedirectToAction("Result", new { id = attempt.AttemptId });
		}


		// =================== XEM KẾT QUẢ ===================
		public IActionResult Result(int id)
		{
			var userId = GetCurrentUserId();
			if (userId == null)
				return RedirectToAction("Login", "Home");

			var attempt = _context.TestAttempts
				.Include(ta => ta.Test)
				.Include(ta => ta.Answers)
					.ThenInclude(a => a.Question)
						.ThenInclude(q => q.QuestionOptions) // load cả option của câu hỏi
				.Include(ta => ta.Answers)
					.ThenInclude(a => a.Option) // option user chọn
				.FirstOrDefault(ta => ta.AttemptId == id && ta.UserId == userId);

			if (attempt == null)
				return NotFound();

			// Truyền xuống view đầy đủ dữ liệu
			return View(attempt);
		}

		// =================== HỖ TRỢ LẤY USER ID TỪ SESSION ===================
		private int? GetCurrentUserId()
		{
			return HttpContext.Session.GetInt32("UserId");
		}
	}
}
