using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EmployeeSurvey.Models
{
	// ViewModel cho việc tạo mới câu hỏi và các đáp án
	public class QuestionCreateViewModel
	{
		// Thông tin câu hỏi
		public int QuestionID { get; set; }

		[Required(ErrorMessage = "Nội dung câu hỏi không được để trống")]
		public string Content { get; set; }

		[Required(ErrorMessage = "Loại câu hỏi không được để trống")]
		public string QuestionType { get; set; } // VD: MCQ, Essay, True/False

		public int? SkillID { get; set; }
		public int? DifficultyID { get; set; }

		public int? CreatedBy { get; set; }
		public DateTime CreatedDate { get; set; } = DateTime.Now;

		// Danh sách phương án trả lời (Options)
		public List<QuestionOptionViewModel> Options { get; set; } = new List<QuestionOptionViewModel>();
	}

	// ViewModel cho phương án trả lời
	public class QuestionOptionViewModel
	{
		public int OptionID { get; set; }
		public int QuestionID { get; set; }

		[Required(ErrorMessage = "Nội dung đáp án không được để trống")]
		public string Content { get; set; }

		public bool IsCorrect { get; set; } // true nếu là đáp án đúng
	}
}
