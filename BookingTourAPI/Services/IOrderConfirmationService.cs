using System.Threading.Tasks;

namespace BookingTourAPI.Services
{
    // Interface này cho phép cả Admin (thủ công) và MoMo (tự động)
    // cùng sử dụng một logic để xác nhận đơn hàng.
    public interface IOrderConfirmationService
    {
        Task<bool> ConfirmOrderAsync(string orderType, string orderId);
    }
}