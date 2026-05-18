using GLMS.Enterprise.Core.Entities;

namespace GLMS.Enterprise.Web.Models;

public class DashboardViewModel
{
    public int TotalClients { get; set; }
    public int TotalContracts { get; set; }
    public int ActiveContracts { get; set; }
    public int TotalServiceRequests { get; set; }
    public int PendingRequests { get; set; }
    public List<Contract> RecentContracts { get; set; } = new();
}
