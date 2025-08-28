using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeSurvey.Models
{
	public partial class QuestionOption
	{
		[Key]
		[Column("OptionID")]
		public int OptionId { get; set; }  // PK

		[Column("QuestionID")]
		public int QuestionId { get; set; }  // FK

		[StringLength(500)]
		public string Content { get; set; } = null!;

		public bool? IsCorrect { get; set; }

		// ---------------- Relations ----------------
		[InverseProperty("Option")]
		public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

		[ForeignKey("QuestionId")]
		[InverseProperty("QuestionOptions")]
		public virtual Question? Question { get; set; } = null!;
	}
}
