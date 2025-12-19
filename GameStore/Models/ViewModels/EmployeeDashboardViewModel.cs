using System.Collections.Generic;
using GameStore.Models;

namespace GameStore.ViewModels
{
    public class EmployeeDashboardViewModel
    {
        public int TotalProductsInStock { get; set; }
        public List<Order> Orders { get; set; } = new List<Order>();
        public List<Product> SearchResults { get; set; } = new List<Product>();
        public List<Product> AllProductsInStock { get; set; } = new List<Product>();
    }
}