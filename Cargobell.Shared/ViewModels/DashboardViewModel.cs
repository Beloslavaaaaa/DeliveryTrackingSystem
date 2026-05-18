using System.Collections.Generic;
using Cargobell.Shared.Models;
namespace Cargobell.Shared.ViewModels
{
    public class DashboardViewModel
    {
        public string UserName { get; set; }
        public int ActiveShipmentsCount { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalEarned { get; set; }
        public List<RecentAction> RecentActions { get; set; }
    }

    public class RecentAction
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public string Amount { get; set; }
        public string Date { get; set; }
        public string Status { get; set; }
    }
}