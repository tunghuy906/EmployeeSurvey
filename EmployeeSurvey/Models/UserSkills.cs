using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace EmployeeSurvey.Models;

[Table("UserSkills")]
public partial class UserSkills
{
	[Key]
	[Column("Id")]  // sửa lại cho trùng tên cột DB
	public int Id { get; set; }

	[Column("UserId")]
	public int UserId { get; set; }

	[Column("SkillId")]
	public int SkillId { get; set; }

	[Range(0, 100)]
	public int Score { get; set; } // điểm kỹ năng (0 - 100)

	[ForeignKey("UserId")]
	[InverseProperty("UserSkills")]
	public virtual User User { get; set; } = null!;

	[ForeignKey("SkillId")]
	[InverseProperty("UserSkills")]
	public virtual Skill Skill { get; set; } = null!;
}
