using System.Collections.Generic;
using GameStore.Models;

namespace GameStore.ViewModels
{
    public class QuanLySanPhamViewModel
    {
        public int TotalProductsInStock { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Brand> Brands { get; set; } = new List<Brand>();
    }
}