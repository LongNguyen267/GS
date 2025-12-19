using GameStore.Models;
using GameStore.ViewModels;
namespace GameStore.Services
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context, VnPayRequestModel model);
        VnPayResponseModel PaymentExecute(IQueryCollection collections);
    }
}