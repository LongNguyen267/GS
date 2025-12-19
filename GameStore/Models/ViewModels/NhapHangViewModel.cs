using System.Collections.Generic;
using GameStore.Models;

namespace GameStore.ViewModels
{
    public class NhapHangViewModel
    {
        public List<Brand> Brands { get; set; } = new List<Brand>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Product> Products { get; set; } = new List<Product>();
    }
}