using System.ComponentModel.DataAnnotations;

namespace EmployeeSurvey.Models
{
	public class AssignGroupViewModel
	{
		[Required]
		[Display(Name = "Bài test")]
		public int TestId { get; set; }

		[Display(Name = "Role")]
		public int? RoleId { get; set; }

		[Display(Name = "Level")]
		public string? Level { get; set; }

		[Display(Name = "Phòng ban")]
		public int? DepartmentId { get; set; }

		[Display(Name = "Deadline")]
		[DataType(DataType.Date)]
		public DateTime? Deadline { get; set; }
	}
}
