document.addEventListener('DOMContentLoaded', function() {
    // Удаляем все стандартные теги иконок Swagger
    const existingIcons = document.querySelectorAll("link[rel*='icon']");
    existingIcons.forEach(icon => icon.remove());

    // Создаем и добавляем вашу новую иконку
    const link = document.createElement('link');
    link.type = 'image/x-icon';
    link.rel = 'icon';
    link.href = '/favicon.ico';
    document.head.appendChild(link);
});
