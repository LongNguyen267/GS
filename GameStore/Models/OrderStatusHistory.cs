using System;
using System.Collections.Generic;

namespace GameStore.Models;

public partial class OrderStatusHistory
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public string? OldStatus { get; set; }

    public string NewStatus { get; set; } = null!;

    public DateTime? ChangedAt { get; set; }

    public int? ChangedByUserId { get; set; }

    public virtual User? ChangedByUser { get; set; }

    public virtual Order Order { get; set; } = null!;
}
