using System;
using System.Collections.Generic;

namespace GameStore.Models;

public partial class Discount
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public string Type { get; set; } = null!;

    public decimal Value { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
