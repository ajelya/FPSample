namespace FPSample.Models.Entities // Ensure this matches your folder path
{
    public class AdminDashboardVM
    {
        public int TotalUsers { get; set; }
        public int TotalRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int ReadyToClaim { get; set; }

        // This property holds the data for your table
        public IEnumerable<dynamic> ActiveRequests { get; set; }
    }
}