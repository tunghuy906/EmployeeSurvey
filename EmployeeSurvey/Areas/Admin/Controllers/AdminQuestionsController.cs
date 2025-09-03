using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EmployeeSurvey.Models;
using ClosedXML.Excel;

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
		// GET: Admin/AdminQuestions/Create
		[HttpGet]
		public IActionResult Create()
		{
			ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName");
			ViewData["DifficultyId"] = new SelectList(_context.Difficulties, "DifficultyId", "LevelName");
			ViewData["SkillId"] = new SelectList(_context.Skills, "SkillId", "SkillName");

			// Tạo sẵn 4 option mặc định cho form MCQ
			var vm = new QuestionCreateViewModel
			{
				QuestionType = "MCQ",
				Options = new List<QuestionOptionViewModel>
		{
			new QuestionOptionViewModel(),
			new QuestionOptionViewModel(),
			new QuestionOptionViewModel(),
			new QuestionOptionViewModel()
		}
			};

			return View(vm);
		}


		// POST: Admin/AdminQuestions/Create
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(QuestionCreateViewModel model)
		{
			if (!ModelState.IsValid)
			{
				ViewData["DifficultyId"] = new SelectList(_context.Difficulties, "DifficultyId", "LevelName", model.DifficultyID);
				ViewData["SkillId"] = new SelectList(_context.Skills, "SkillId", "SkillName", model.SkillID);
				return View(model);
			}

			// Lấy userId từ Session hoặc Claims
			var userId = HttpContext.Session.GetInt32("UserId");
			if (userId == null)
			{
				return RedirectToAction("Login", "Home");
			}

			var question = new Question
			{
				Content = model.Content,
				QuestionType = model.QuestionType,
				SkillId = model.SkillID,
				DifficultyId = model.DifficultyID,
				CreatedBy = userId.Value, // set trực tiếp từ session
				CreatedDate = DateTime.Now,
				QuestionOptions = new List<QuestionOption>()
			};

			if (model.QuestionType == "TrueFalse")
			{
				question.QuestionOptions = new List<QuestionOption>
		{
			new QuestionOption { Content = "True", IsCorrect = model.Options.Any(o => o.Content == "True" && o.IsCorrect) },
			new QuestionOption { Content = "False", IsCorrect = model.Options.Any(o => o.Content == "False" && o.IsCorrect) }
		};
			}
			else if (model.QuestionType == "MCQ" || model.QuestionType == "MultipleResponse")
			{
				question.QuestionOptions = model.Options
					.Where(o => !string.IsNullOrWhiteSpace(o.Content))
					.Select(o => new QuestionOption
					{
						Content = o.Content,
						IsCorrect = o.IsCorrect
					}).ToList();
			}
			else if (model.QuestionType == "TextInput")
			{
				if (!string.IsNullOrWhiteSpace(model.Content))
				{
					question.QuestionOptions = new List<QuestionOption>
			{
				new QuestionOption { Content = model.Content, IsCorrect = true }
			};
				}
			}

			_context.Questions.Add(question);
			await _context.SaveChangesAsync();

			return RedirectToAction(nameof(Index));
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
			ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName", question.CreatedBy);
			ViewData["DifficultyId"] = new SelectList(_context.Difficulties, "DifficultyId", "LevelName", question.DifficultyId);
			ViewData["SkillId"] = new SelectList(_context.Skills, "SkillId", "SkillName", question.SkillId);
			return View(question);
		}

		// POST: Admin/AdminQuestions/Edit/5
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
			ViewData["CreatedBy"] = new SelectList(_context.Users, "UserId", "FullName", question.CreatedBy);
			ViewData["DifficultyId"] = new SelectList(_context.Difficulties, "DifficultyId", "LevelName", question.DifficultyId);
			ViewData["SkillId"] = new SelectList(_context.Skills, "SkillId", "SkillName", question.SkillId);
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
			// Lấy question kèm các collection
			var question = await _context.Questions
				.Include(q => q.QuestionOptions)
				.Include(q => q.Answers)
				.Include(q => q.TestQuestions)
				.FirstOrDefaultAsync(q => q.QuestionId == id);

			if (question != null)
			{
				// Xóa các bản ghi con trước
				if (question.QuestionOptions.Any())
					_context.QuestionOptions.RemoveRange(question.QuestionOptions);

				if (question.Answers.Any())
					_context.Answers.RemoveRange(question.Answers);

				if (question.TestQuestions.Any())
					_context.TestQuestions.RemoveRange(question.TestQuestions);

				// Xóa question
				_context.Questions.Remove(question);

				await _context.SaveChangesAsync();
			}

			return RedirectToAction(nameof(Index));
		}

		private bool QuestionExists(int id)
		{
			return _context.Questions.Any(e => e.QuestionId == id);
		}

		[HttpPost]
		public async Task<IActionResult> ImportExcel(IFormFile file)
		{
			if (file == null || file.Length == 0)
			{
				ModelState.AddModelError("", "Please select an Excel file.");
				return RedirectToAction(nameof(Index));
			}

			using (var stream = new MemoryStream())
			{
				await file.CopyToAsync(stream);
				using (var workbook = new XLWorkbook(stream))
				{
					var worksheet = workbook.Worksheet(1); // sheet 1
					var rows = worksheet.RangeUsed().RowsUsed().Skip(1); // bỏ row header

					foreach (var row in rows)
					{
						var questionType = row.Cell(2).GetString();

						var question = new Question
						{
							Content = row.Cell(1).GetString(),
							QuestionType = questionType,
							SkillId = row.Cell(3).GetValue<int>(),
							DifficultyId = row.Cell(4).GetValue<int>(),
							CreatedBy = row.Cell(5).GetValue<int>(),
							CreatedDate = DateTime.Now,
							QuestionOptions = new List<QuestionOption>()
						};

						// Đọc CorrectOptionIndex (cột 10) -> VD: "1"
						var correctIndexesStr = row.Cell(10).GetString();
						var correctIndexes = new List<int>();
						if (!string.IsNullOrWhiteSpace(correctIndexesStr))
						{
							correctIndexes = correctIndexesStr
								.Split(',', StringSplitOptions.RemoveEmptyEntries)
								.Select(x => int.Parse(x.Trim()))
								.ToList();
						}

						if (questionType == "TrueFalse")
						{
							// Luôn ép tạo 2 option True/False
							question.QuestionOptions = new List<QuestionOption>
							{
								new QuestionOption { Content = "True",  IsCorrect = correctIndexes.Contains(1) },
								new QuestionOption { Content = "False", IsCorrect = correctIndexes.Contains(2) }
							};
						}
						else if (questionType == "MCQ")
						{
							// Option nằm ở cột 6–9
							for (int i = 6; i <= 9; i++)
							{
								if (!string.IsNullOrWhiteSpace(row.Cell(i).GetString()))
								{
									question.QuestionOptions.Add(new QuestionOption
									{
										Content = row.Cell(i).GetString(),
										IsCorrect = correctIndexes.Contains(i - 5)
									});
								}
							}
						}
						else if (questionType == "Text")
						{
							// Text question -> không có option
							question.QuestionOptions = new List<QuestionOption>();
						}

						_context.Questions.Add(question);
					}

					await _context.SaveChangesAsync();
				}
			}

			TempData["Message"] = "Import successfully!";
			return RedirectToAction(nameof(Index));
		}
	}
}
