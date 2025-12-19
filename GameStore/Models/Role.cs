using System.Collections.Generic;

namespace GameStore.Models;

public partial class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;

    // Thuộc tính đã được thêm
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}