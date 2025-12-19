using GameStore.Models;
using GameStore.ViewModels;

namespace GameStore.Services
{
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _config;

        public VnPayService(IConfiguration config)
        {
            _config = config;
        }

        public string CreatePaymentUrl(HttpContext context, VnPayRequestModel model)
        {
            var vnpay = new VnPayLibrary();

            // =========================================================================
            // CẤU HÌNH CỦA BẠN (Đã cập nhật theo email VNPAY gửi)
            // =========================================================================
            string vnp_TmnCode = "7ET0HOP8";
            string vnp_HashSecret = "YHFYNI77R15T6W9P77TADOSBUK4F5VJ5";
            string vnp_BaseUrl = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";

            // QUAN TRỌNG: Kiểm tra lại số cổng (Port) 7253 xem có đúng web bạn đang chạy không
            string vnp_ReturnUrl = "https://localhost:7253/Checkout/PaymentCallBack";
            // =========================================================================

            vnpay.AddRequestData("vnp_Version", "2.1.0");
            vnpay.AddRequestData("vnp_Command", "pay");
            vnpay.AddRequestData("vnp_TmnCode", vnp_TmnCode);

            // Số tiền phải nhân 100 và ép kiểu long
            vnpay.AddRequestData("vnp_Amount", ((long)(model.Amount * 100)).ToString());

            vnpay.AddRequestData("vnp_CreateDate", model.CreatedDate.ToString("yyyyMMddHHmmss"));
            vnpay.AddRequestData("vnp_CurrCode", "VND");
            vnpay.AddRequestData("vnp_IpAddr", Utils.GetIpAddress(context));
            vnpay.AddRequestData("vnp_Locale", "vn");

            vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang:" + model.OrderId);
            vnpay.AddRequestData("vnp_OrderType", "other");
            vnpay.AddRequestData("vnp_ReturnUrl", vnp_ReturnUrl);

            // --- [ĐOẠN ĐÃ SỬA] ---
            // Thay vì gửi tick (số quá lớn gây lỗi), ta gửi đúng mã đơn hàng (số nhỏ)
            vnpay.AddRequestData("vnp_TxnRef", model.OrderId.ToString());
            // ---------------------

            var paymentUrl = vnpay.CreateRequestUrl(vnp_BaseUrl, vnp_HashSecret);

            return paymentUrl;
        }

        public VnPayResponseModel PaymentExecute(IQueryCollection collections)
        {
            var vnpay = new VnPayLibrary();
            foreach (var (key, value) in collections)
            {
                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    vnpay.AddResponseData(key, value.ToString());
                }
            }

            // Cập nhật lại mã HashSecret ở đây để kiểm tra chữ ký khi VNPAY trả về
            string vnp_HashSecret = "YHFYNI77R15T6W9P77TADOSBUK4F5VJ5";

            var vnp_orderId = Convert.ToInt64(vnpay.GetResponseData("vnp_TxnRef"));
            var vnp_TransactionId = Convert.ToInt64(vnpay.GetResponseData("vnp_TransactionNo"));
            var vnp_SecureHash = collections.FirstOrDefault(p => p.Key == "vnp_SecureHash").Value;
            var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
            var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

            bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash, vnp_HashSecret);

            if (!checkSignature)
            {
                return new VnPayResponseModel
                {
                    Success = false
                };
            }

            return new VnPayResponseModel
            {
                Success = true,
                PaymentMethod = "VnPay",
                OrderDescription = vnp_OrderInfo,
                OrderId = vnp_orderId.ToString(),
                TransactionId = vnp_TransactionId.ToString(),
                Token = vnp_SecureHash,
                VnPayResponseCode = vnp_ResponseCode
            };
        }
    }
}