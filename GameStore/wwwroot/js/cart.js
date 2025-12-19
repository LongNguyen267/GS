// wwwroot/js/cart.js

$(document).ready(function () {
    // Cấu hình Toastr để hiển thị thông báo
    toastr.options = {
        "closeButton": true,
        "progressBar": true,
        "positionClass": "toast-top-right", // Vị trí hiển thị
        "timeOut": "3000", // Tự động tắt sau 3 giây
    };

    // Bắt sự kiện click cho tất cả các nút có class 'add-cart-btn' trên toàn trang
    $(document).on('click', '.add-cart-btn', function (e) {
        e.preventDefault(); // Ngăn hành vi mặc định của nút (tránh chuyển trang)

        var productId = $(this).data('product-id');
        var quantity = 1; // Số lượng mặc định là 1

        // Nếu có ô nhập số lượng (trên trang chi tiết), lấy giá trị từ đó
        if ($('#quantity-input').length > 0) {
            quantity = parseInt($('#quantity-input').val());
        }

        // Gửi yêu cầu AJAX đến CartController
        $.ajax({
            url: '/Cart/AddToCart',
            type: 'POST',
            data: {
                productId: productId,
                quantity: quantity
            },
            success: function (response) {
                if (response.success) {
                    // Nếu server trả về thành công, hiển thị thông báo thành công
                    toastr.success(response.message);
                } else {
                    // Nếu server trả về lỗi, hiển thị thông báo lỗi
                    toastr.error(response.message);
                }
            },
            error: function () {
                // Nếu có lỗi kết nối đến server
                toastr.error('Có lỗi xảy ra, vui lòng thử lại sau.');
            }
        });
    });
});