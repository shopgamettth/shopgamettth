using System;

namespace Coffee.Helper
{
    public static class OrderStatusHelper
    {
        public const string UnpaidStatus = "Chua thanh toan";
        public const string PaidStatus = "Da thanh toan";
        public const string CancelledStatus = "Da huy";
        public const string CodPaymentMethod = "COD";
        public const string MomoPaymentMethod = "MoMo";

        public static string NormalizeOrderStatus(string? orderStatus, string? paymentStatus = null)
        {
            if (MatchesCancelled(orderStatus) || MatchesCancelled(paymentStatus))
            {
                return CancelledStatus;
            }

            if (MatchesPaid(orderStatus) || MatchesPaid(paymentStatus))
            {
                return PaidStatus;
            }

            return UnpaidStatus;
        }

        public static string NormalizePaymentStatus(string? paymentStatus, string? orderStatus = null)
        {
            if (MatchesCancelled(paymentStatus) || MatchesCancelled(orderStatus))
            {
                return CancelledStatus;
            }

            if (MatchesPaid(paymentStatus) || MatchesPaid(orderStatus))
            {
                return PaidStatus;
            }

            return UnpaidStatus;
        }

        public static bool CanCustomerCancelOrder(string? orderStatus, string? paymentStatus = null)
        {
            return IsUnpaid(NormalizeOrderStatus(orderStatus, paymentStatus))
                && IsUnpaid(NormalizePaymentStatus(paymentStatus, orderStatus));
        }

        public static string GetCustomerCancellationMessage(string? orderStatus, string? paymentStatus = null)
        {
            var normalizedOrderStatus = NormalizeOrderStatus(orderStatus, paymentStatus);

            if (IsCancelled(normalizedOrderStatus))
            {
                return "Don nay da duoc huy truoc do.";
            }

            if (CanCustomerCancelOrder(orderStatus, paymentStatus))
            {
                return "Ban co the huy don nay neu muon thay doi thong tin hoac dat lai tu dau.";
            }

            return "Don nay da duoc cap nhat sang da thanh toan, ban khong the huy truc tiep tren website nua.";
        }

        public static bool IsUnpaid(string? status)
        {
            return string.Equals(status, UnpaidStatus, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPaid(string? status)
        {
            return string.Equals(status, PaidStatus, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCancelled(string? status)
        {
            return string.Equals(status, CancelledStatus, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCodPaymentMethod(string? paymentMethod)
        {
            return string.Equals(paymentMethod, CodPaymentMethod, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsMomoPaymentMethod(string? paymentMethod)
        {
            return string.Equals(paymentMethod, MomoPaymentMethod, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsCompletedSale(string? orderStatus, string? paymentStatus = null)
        {
            if (MatchesCancelled(orderStatus) || MatchesCancelled(paymentStatus))
            {
                return false;
            }

            return MatchesCompletedSale(orderStatus) || MatchesCompletedSale(paymentStatus);
        }

        private static bool MatchesCancelled(string? status)
        {
            return IsAnyStatus(status,
                CancelledStatus,
                "Da huy don COD",
                "Da huy thanh toan MoMo");
        }

        private static bool MatchesPaid(string? status)
        {
            return IsAnyStatus(status,
                PaidStatus,
                "Da duyet COD",
                "Da thanh toan MoMo",
                "Da gui yeu cau xac nhan",
                "Cho shop xac nhan thanh toan");
        }

        private static bool MatchesCompletedSale(string? status)
        {
            return IsAnyStatus(status,
                PaidStatus,
                "Da duyet COD",
                "Da thanh toan MoMo");
        }

        private static bool IsAnyStatus(string? value, params string[] candidates)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (value.StartsWith("Thanh toan that bai", StringComparison.OrdinalIgnoreCase))
            {
                return Array.Exists(candidates, candidate =>
                    string.Equals(candidate, UnpaidStatus, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var candidate in candidates)
            {
                if (string.Equals(value, candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
