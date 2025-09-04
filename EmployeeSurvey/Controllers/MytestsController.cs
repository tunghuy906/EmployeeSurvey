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
				.FirstOrDefault(a => a.TestId == testId && a.UserId == userId && (a.Status == "InProgress" || a.Status == "Draft"));

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

			if (attempt.StartTime != null && timeSpent > 0)
			{
				attempt.EndTime = attempt.StartTime.Value.AddSeconds(timeSpent);
			}
			else
			{
				attempt.EndTime = DateTime.Now; // fallback
			}
			int correct = 0;

			// chỉ tính câu hỏi auto-chấm
			int total = _context.TestQuestions
				.Include(tq => tq.Question)
				.Count(tq => tq.TestId == testId && tq.Question.QuestionType != "TextInput");

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
						// ✅ MCQ
						case "MCQ":
							var selectedOption = _context.QuestionOptions
								.FirstOrDefault(o => o.OptionId == ans.OptionId);

							ans.IsCorrect = (selectedOption != null && selectedOption.IsCorrect == true);
							if (ans.IsCorrect == true) correct++;
							break;

						// ✅ True/False
						case "TrueFalse":
							if (ans.OptionId.HasValue)
							{
								var tfOption = _context.QuestionOptions
									.FirstOrDefault(o => o.OptionId == ans.OptionId);

								ans.IsCorrect = (tfOption != null && tfOption.IsCorrect == true);
							}
							else if (!string.IsNullOrEmpty(ans.AnswerText))
							{
								var tfOption = question.QuestionOptions
									.FirstOrDefault(o => o.Content.Equals(ans.AnswerText, StringComparison.OrdinalIgnoreCase));

								ans.IsCorrect = (tfOption != null && tfOption.IsCorrect == true);
								if (ans.IsCorrect == true) ans.OptionId = tfOption.OptionId;
							}
							if (ans.IsCorrect == true) correct++;
							break;

						// ❌ TextInput (chưa chấm → để null)
						case "TextInput":
							ans.IsCorrect = null;
							break;
					}
				}

				_context.Answers.Add(ans);
			}
			// Tổng số câu trong bài test
			int totalQuestions = attempt.Answers.Count;

			// Số câu auto đã trả lời đúng
			int autoCorrect = attempt.Answers.Count(a =>
				(a.Question.QuestionType == "MCQ" || a.Question.QuestionType == "TrueFalse")
				&& a.IsCorrect == true);

			// Số câu tự luận đã chấm đúng
			int gradedTextCorrect = attempt.Answers.Count(a =>
				a.Question.QuestionType == "TextInput" && a.IsGraded == true && a.IsCorrect == true);

			// Tính điểm (auto + text đã chấm)
			decimal tempScore = (totalQuestions > 0)
				? Math.Round(((decimal)(autoCorrect + gradedTextCorrect) / totalQuestions) * 10m, 2)
				: 0;

			// Lưu vào DB
			attempt.Score = tempScore;
			_context.Update(attempt);
			_context.SaveChanges();

			return RedirectToAction("Result", new { id = attempt.AttemptId });


		}

		public IActionResult Result(int id) // ✅ đổi về id cho khớp route
		{
			var userId = GetCurrentUserId();
			if (userId == null)
				return RedirectToAction("Login", "Home");

			// Lấy attempt theo AttemptId và UserId
			var attempt = _context.TestAttempts
				.Include(ta => ta.Test)
				.Include(ta => ta.Answers)
					.ThenInclude(a => a.Question)
						.ThenInclude(q => q.QuestionOptions)
				.Include(ta => ta.Answers)
					.ThenInclude(a => a.Option)
				.FirstOrDefault(ta => ta.AttemptId == id && ta.UserId == userId);

			if (attempt == null)
				return NotFound();

			return View(attempt);
		}

		// =================== HỖ TRỢ LẤY USER ID TỪ SESSION ===================
		private int? GetCurrentUserId()
		{
			return HttpContext.Session.GetInt32("UserId");
		}

	}
}
