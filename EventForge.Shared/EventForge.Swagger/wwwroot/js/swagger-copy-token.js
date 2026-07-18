(function () {
    function initTokenObserver() {
        // Мягкое ожидание загрузки body страницы
        if (!document.body) {
            setTimeout(initTokenObserver, 10);
            return;
        }

        const observer = new MutationObserver(() => {
            // Находим все блоки pre (тела ответов Swagger UI) tanpa повторной обработки
            const preBlocks = document.querySelectorAll('pre:not(.has-copy-button)');

            preBlocks.forEach(block => {
                const textContent = block.textContent;

                // Быстрая проверка на ключевое слово token без привязки к кавычкам разметки
                if (textContent.toLowerCase().includes('token')) {

                    // Регулярное выражение ищет ключ token/access_token и забирает значение в первую группу
                    const tokenMatch = textContent.match(/"(?:access_)?token"\s*:\s*"([^"]+)"/i);

                    if (tokenMatch && tokenMatch[1]) {
                        const cleanToken = tokenMatch[1].trim();

                        // Исключаем дефолтные схемы Swagger (Example Value), где значение равно "string"
                        if (cleanToken !== 'string' && cleanToken.length > 20) {
                            block.classList.add('has-copy-button');

                            // Создаем кнопку в фирменном стиле Swagger
                            const copyBtn = document.createElement('button');
                            copyBtn.innerText = '📋 Копировать токен';

                            // Стили позиционирования по правому краю
                            copyBtn.style.display = 'block';
                            copyBtn.style.marginLeft = 'auto';
                            copyBtn.style.marginRight = '0';
                            copyBtn.style.marginBottom = '10px';

                            // Визуальное оформление
                            copyBtn.style.backgroundColor = '#49cc90';
                            copyBtn.style.color = '#fff';
                            copyBtn.style.border = 'none';
                            copyBtn.style.padding = '6px 12px';
                            copyBtn.style.borderRadius = '4px';
                            copyBtn.style.fontFamily = 'sans-serif';
                            copyBtn.style.fontSize = '12px';
                            copyBtn.style.fontWeight = 'bold';
                            copyBtn.style.cursor = 'pointer';
                            copyBtn.style.transition = 'background-color 0.2s';

                            // Логика копирования
                            copyBtn.onclick = function (e) {
                                e.preventDefault();
                                navigator.clipboard.writeText(cleanToken).then(() => {
                                    copyBtn.innerText = '✅ Токен скопирован!';
                                    copyBtn.style.backgroundColor = '#2e7d32';
                                    setTimeout(() => {
                                        copyBtn.innerText = '📋 Копировать токен';
                                        copyBtn.style.backgroundColor = '#49cc90';
                                    }, 2000);
                                }).catch(err => {
                                    console.error('Copy failed: ', err);
                                });
                            };

                            // Вставляем кнопку ровно над черным полем ответа
                            block.parentNode.insertBefore(copyBtn, block);
                        }
                    }
                }
            });
        });

        // Начинаем следить за изменениями в DOM
        observer.observe(document.body, { childList: true, subtree: true });
    }

    initTokenObserver();
})();
