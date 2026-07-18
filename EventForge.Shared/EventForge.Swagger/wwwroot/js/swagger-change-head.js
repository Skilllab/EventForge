document.addEventListener('DOMContentLoaded', function() {
    // Ждем появления элемента ссылки в шапке
    const checkTopbar = setInterval(() => {
        const topbarLink = document.querySelector('.swagger-ui .topbar .link');

        if (topbarLink)
        {
            clearInterval(checkTopbar); // Останавливаем проверку

            // Создаем контейнер для нашего текста
            const brandText = document.createElement('span');
            brandText.className = 'custom-topbar-text';
            brandText.innerText = 'EventForge - лучшая микросервисная платформа';

            // Добавляем текст внутрь ссылки в шапке
            topbarLink.appendChild(brandText);
        }
    }, 50); // Проверяем каждые 50мс
});
