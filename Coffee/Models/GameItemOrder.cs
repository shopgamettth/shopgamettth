using System;

namespace Coffee.Models;

public partial class GameItemOrder
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public int GameItemPackageId { get; set; }
    public virtual GameItemPackage GameItemPackage { get; set; } = null!;

    public string PlayerId { get; set; } = null!;
    
    public int Status { get; set; } // 0: Pending, 1: Completed, 2: Cancelled

    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
