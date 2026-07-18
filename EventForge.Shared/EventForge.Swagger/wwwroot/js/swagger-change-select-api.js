document.addEventListener('DOMContentLoaded', function() {
    // Создаем наблюдатель за изменениями на странице
    const observer = new MutationObserver((mutations, obs) => {
        // Ищем лейбл выпадающего списка
        const label = document.querySelector('.swagger-ui .topbar .download-url-wrapper label span');

        if (label)
        {
            // Заменяем текст на ваш собственный
            label.textContent = 'Выберите версию API:'; // ВПИШИТЕ СЮДА ВАШ ТЕКСТ
            obs.disconnect(); // Отключаем наблюдатель, когда текст изменен
        }
    });

    // Начинаем следить за всем документом
    observer.observe(document.body, {
    childList: true,
                    subtree: true
                });
});
