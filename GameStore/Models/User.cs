using System;
using System.Collections.Generic;

namespace GameStore.Models;

public partial class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string Password { get; set; } = null!;
    public string? Address { get; set; }
    public DateTime? CreatedAt { get; set; }

    public virtual Cart? Cart { get; set; }
    public virtual ICollection<OrderStatusHistory> OrderStatusHistories { get; set; } = new List<OrderStatusHistory>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    public virtual PasswordResetToken? PasswordResetToken { get; set; }
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    // Thuộc tính đã được sửa để khớp với bảng trung gian UserRoles
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}