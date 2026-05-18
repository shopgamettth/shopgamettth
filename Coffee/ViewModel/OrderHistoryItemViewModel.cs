using Coffee.Helper;
using System;

namespace Coffee.ViewModel
{
    public class OrderHistoryItemViewModel
    {
        public int OrderId { get; set; }

        public DateTimeOffset OrderDate { get; set; }

        public string ReceiverName { get; set; } = string.Empty;

        public string ReceiverPhone { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public int TotalItems { get; set; }

        public bool IsCodPayment =>
            string.Equals(PaymentMethod, "COD", StringComparison.OrdinalIgnoreCase);

        public bool IsMomoPayment => 
            string.Equals(PaymentMethod, "MoMo", StringComparison.OrdinalIgnoreCase);

        public bool CanCancel =>
            OrderStatusHelper.CanCustomerCancelOrder(Status, PaymentStatus);

        public bool IsCancelled =>
            OrderStatusHelper.IsCancelled(Status);
    }
}
