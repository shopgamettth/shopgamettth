using System;
using System.Collections.Generic;

namespace Coffee.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string? ProductName { get; set; }

    public decimal Price { get; set; }

    public string? Description { get; set; }

    public string? ImageUrl { get; set; }

    public int? CategoryId { get; set; }

    public string? ImagePublicId { get; set; }

    public string? GameUsername { get; set; }

    public string? GamePassword { get; set; }

    public bool IsSold { get; set; } = false;

    public string? ExtraImages { get; set; }

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual Category? Category { get; set; }

    public virtual ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
}
