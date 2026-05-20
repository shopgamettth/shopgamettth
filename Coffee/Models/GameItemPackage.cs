using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Coffee.Models;

public partial class GameItemPackage
{
    public int Id { get; set; }
    
    public int GameId { get; set; }
    public virtual Game Game { get; set; } = null!;

    public string PackageName { get; set; } = null!;
    
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }
    
    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }

    public virtual ICollection<GameItemOrder> GameItemOrders { get; set; } = new List<GameItemOrder>();
}
