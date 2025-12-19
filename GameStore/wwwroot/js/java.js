document.addEventListener('DOMContentLoaded', () => {
    // --- Khai báo các biến cần thiết ---
    const slides = document.querySelectorAll('.slide');
    const dotsContainer = document.querySelector('.slider-dots');
    const prevBtn = document.querySelector('.prev-btn');
    const nextBtn = document.querySelector('.next-btn');

    let currentSlide = 0;
    let slideInterval; // Biến để chứa bộ đếm thời gian tự động chuyển slide

    const slideCount = slides.length;
    if (slideCount === 0) return; // Nếu không có slide nào thì dừng

    // --- Tạo các dấu chấm điều khiển ---
    for (let i = 0; i < slideCount; i++) {
        const dot = document.createElement('span');
        dot.classList.add('dot');
        dot.dataset.slide = i;
        dotsContainer.appendChild(dot);
    }
    const dots = document.querySelectorAll('.dot');
    dots[0].classList.add('active'); // Kích hoạt dấu chấm đầu tiên

    // --- Hàm chính để hiển thị slide ---
    function showSlide(slideIndex) {
        // Xử lý vòng lặp: nếu đến slide cuối thì quay lại slide đầu
        if (slideIndex >= slideCount) {
            slideIndex = 0;
        } else if (slideIndex < 0) {
            slideIndex = slideCount - 1;
        }

        // Ẩn slide hiện tại và bỏ active ở dot
        slides[currentSlide].classList.remove('active');
        dots[currentSlide].classList.remove('active');

        // Hiện slide mới và active dot mới
        slides[slideIndex].classList.add('active');
        dots[slideIndex].classList.add('active');

        // Cập nhật lại vị trí slide hiện tại
        currentSlide = slideIndex;
    }

    // --- CÁC HÀM MỚI ĐỂ TỰ ĐỘNG CHUYỂN SLIDE ---

    // Hàm để dừng slideshow (khi người dùng tương tác)
    function stopSlideShow() {
        clearInterval(slideInterval);
    }

    // Hàm để bắt đầu hoặc reset lại slideshow
    function startSlideShow() {
        stopSlideShow(); // Dừng slideshow cũ trước khi bắt đầu cái mới

        // Hàm setInterval sẽ lặp lại hành động bên trong nó
        // sau mỗi 10000 mili giây (tức là 10 giây)
        slideInterval = setInterval(() => {
            showSlide(currentSlide + 1);
        }, 10000);
    }

    // --- Gán sự kiện cho các nút ---
    nextBtn.addEventListener('click', () => {
        showSlide(currentSlide + 1);
        startSlideShow(); // Reset lại bộ đếm 10 giây khi người dùng tự bấm nút
    });
    

    prevBtn.addEventListener('click', () => {
        showSlide(currentSlide - 1);
        startSlideShow(); // Reset lại bộ đếm 10 giây
    });

    // Gán sự kiện cho các dấu chấm
    dots.forEach(dot => {
        dot.addEventListener('click', () => {
            const slideIndex = parseInt(dot.dataset.slide);
            showSlide(slideIndex);
            startSlideShow(); // Reset lại bộ đếm 10 giây
        });
    });

    // --- Tự động bắt đầu slideshow khi tải trang ---
    startSlideShow();
});
// File: wwwroot/js/site.js (hoặc file js tương ứng)

// Đảm bảo code chạy sau khi trang đã tải xong
document.addEventListener("DOMContentLoaded", function () {

    const slider = document.querySelector(".product-slider");
    const prevButton = document.querySelector(".prev-arrow");
    const nextButton = document.querySelector(".next-arrow");

    // Nếu không tìm thấy slider trên trang thì không làm gì cả
    if (!slider) {
        return;
    }

    const cards = document.querySelectorAll(".product-card");
    // Nếu không có sản phẩm nào thì dừng lại
    if (cards.length === 0) {
        return;
    }

    const sliderWrapper = document.querySelector(".product-slider-wrapper");
    let currentIndex = 0;

    function slideToCurrent() {
        const cardWidth = cards[0].offsetWidth + 20; // Lấy độ rộng card + margin (10px mỗi bên)
        slider.style.transform = `translateX(-${currentIndex * cardWidth}px)`;
    }

    nextButton.addEventListener("click", () => {
        const itemsVisible = Math.floor(sliderWrapper.offsetWidth / (cards[0].offsetWidth + 20));
        const maxIndex = cards.length - itemsVisible;

        // Nếu đang ở cuối, quay về đầu. Nếu không thì tiến tới.
        if (currentIndex >= maxIndex) {
            currentIndex = 0;
        } else {
            currentIndex++;
        }
        slideToCurrent();
    });

    prevButton.addEventListener("click", () => {
        const itemsVisible = Math.floor(sliderWrapper.offsetWidth / (cards[0].offsetWidth + 20));
        const maxIndex = cards.length - itemsVisible;

        // Nếu đang ở đầu, đi tới cuối. Nếu không thì lùi lại.
        if (currentIndex <= 0) {
            currentIndex = maxIndex;
        } else {
            currentIndex--;
        }
        slideToCurrent();
    });

    // Cập nhật lại vị trí khi thay đổi kích thước cửa sổ để không bị lỗi
    window.addEventListener('resize', () => {
        // Reset về vị trí đầu tiên để tính toán lại cho đúng
        currentIndex = 0;
        slideToCurrent();
    });
});