namespace EmployeeSurvey.Models
{
	public class DashboardViewModel
	{
		public int TotalEmployees { get; set; }   // Người dùng
		public int TotalTests { get; set; }       // Bài test
		public int TotalReports { get; set; }     // Báo cáo (nếu có bảng Report)
		public int TotalAdmins { get; set; }
		public int TotalHR { get; set; }
		public int TotalManagers { get; set; }

		public string? FullName { get; set; }   // dùng để hiển thị lời chào
		public List<int> NewUsersPerDay { get; set; } = new List<int>(); // dữ liệu biểu đồ
	}

}
