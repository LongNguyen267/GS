using GameStore.Models;
using System.Collections.Generic;

namespace GameStore.ViewModels
{
    public class HomeViewModel
    {
        public List<Product> FeaturedProducts { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Discount> Discounts { get; set; } = new List<Discount>();
        public string? SearchQuery { get; set; }
        public string? CurrentCategory { get; set; }
    }
}