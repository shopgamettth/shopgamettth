namespace Coffee.ViewModel
{
    public class AdminDashboardViewModel
    {
        public int TotalProducts { get; set; }

        public int TotalCategories { get; set; }

        public int TotalOrders { get; set; }

        public int TotalCustomers { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal AverageOrderValue { get; set; }

        public int PendingOrders { get; set; }

        public int PendingCodOrders { get; set; }

        public int PaidMomoOrders { get; set; }

        public List<AdminChartItemViewModel> ProductsByCategory { get; set; } = new();

        public List<AdminMonthlyStatViewModel> MonthlyStats { get; set; } = new();

        public List<AdminChartItemViewModel> OrderStatusStats { get; set; } = new();

        public List<AdminTopProductViewModel> TopProducts { get; set; } = new();

        public List<AdminRecentOrderViewModel> RecentOrders { get; set; } = new();

        public List<AdminPendingCodOrderViewModel> PendingCodOrderList { get; set; } = new();
    }

    public class AdminChartItemViewModel
    {
        public string Label { get; set; } = string.Empty;

        public int Value { get; set; }
    }

    public class AdminMonthlyStatViewModel
    {
        public string Label { get; set; } = string.Empty;

        public int OrderCount { get; set; }

        public decimal Revenue { get; set; }
    }

    public class AdminTopProductViewModel
    {
        public string ProductName { get; set; } = string.Empty;

        public int QuantitySold { get; set; }

        public decimal Revenue { get; set; }
    }

    public class AdminRecentOrderViewModel
    {
        public int OrderId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTimeOffset OrderDate { get; set; }
    }

    public class AdminPendingCodOrderViewModel
    {
        public int OrderId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string ReceiverPhone { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTimeOffset OrderDate { get; set; }
    }
}
