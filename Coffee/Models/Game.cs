using System;
using System.Collections.Generic;

namespace Coffee.Models;

public partial class Game
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string? ImagePublicId { get; set; }
    public string? Description { get; set; }

    public virtual ICollection<GameItemPackage> GameItemPackages { get; set; } = new List<GameItemPackage>();
}
