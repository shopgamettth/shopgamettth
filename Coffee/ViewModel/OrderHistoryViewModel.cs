using System.Linq;

namespace Coffee.ViewModel
{
    public class OrderHistoryViewModel
    {
        public List<OrderHistoryItemViewModel> Orders { get; set; } = new();

        public List<OrderHistoryItemViewModel> CodOrders =>
            Orders.Where(x => x.IsCodPayment).ToList();

        public List<OrderHistoryItemViewModel> MomoOrders =>
            Orders.Where(x => x.IsMomoPayment).ToList();
    }
}
