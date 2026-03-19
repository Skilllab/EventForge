<br />
<div align="center">
  <a href="https://github.com/Skilllab/EventBookingService">
    <img src="logo.png" alt="Logo" width="80" height="80">
  </a>

  <h1 align="center">EventBookingService</h3>
  <p align="center">
    Сервис для бронирования билетов на мероприятия   
  </p>
</div>

<!-- ABOUT THE PROJECT -->
## О проекте

Данный проект является по сути PET проектом при прохождении курса "Продвинутая разработка на C# и .NET
"

<!-- GETTING STARTED -->
## Getting Started

Ниже описана инструкция по первоначальной настройке проекта.

### Необходимые компоненты

Проект разрабатывается на VS 2022 с использованием ASP .NET Core 10


### Установка


1. Клонируйте репозиторий
   ```sh
   git clone https://github.com/Skilllab/EventBookingService.git
   ```
2. Откройте папку с решением и выполните сборку проекта WebAPI
   ```sh
   dotnet build EventBookingService.WebAPI
   ```
3. Запустите сборку проекта с тестами
   ```sh
   dotnet build EventBookingService.Tests
   ```
4. Запустите выполнение тестов
   ```sh
    dotnet test EventBookingService.Tests
   ```
5. Запустите проект
   ```sh
   dotnet run --project EventBookingService.WebAPI
   ```


## Использование API

При запущенном проекте в браузере введите адрес на swagger [https://localhost:5001/swagger/index.html](https://localhost:5001/swagger/index.html) 

Доступные запросы

Вид запроса | Метод             | Параметр |Возвращаемое значение| Описание
----------- | ------------------| -------- |---------------------|---------
GET         | /api/Events       |          | Массив JSON         | Получить список всех событий (с фильтрацией и пагинацией)
GET         | /api/Events/\{id} | GUID     | JSON                | Получить событие по id
POST        | /api/Events       | JSON     |                     | Создать новое событие
PUT         | /api/Events       | JSON     |                     | Обновить событие целиком
DELETE      | /api/Events/\{id} | GUID     |                     | Удалить событие

### Виды фильтрации при получении всех событий
```title``` - отфильтруются события, которые содержат в названии  указанный текст

```from``` - отфильтруются события, начало которых раньше или равны указанной дате

```to``` - отфильтруются события, окончание которых позже или равны указанной дате

```page``` - (пагинация) вернется список, состав которого отфильтрован и попал на вторую страницу

```pageSize``` - (пагинация) вернется список, количество которого тут указано для одной страницы

### Формат ошибок при ответе основан на [ProblemDetails (RFC 7807)](https://datatracker.ietf.org/doc/html/rfc7807)  

<details>
<summary>Пример ответа при ненайденном событии</summary>

```json
{
  "type": "Event",
  "status": 404,
  "detail": "Элемент Event c ID: 'b25175cf-298e-4440-a852-fa423a2ba55e' не найден.",
  "instance": "b25175cf-298e-4440-a852-fa423a2ba55e"
}
```
</details>


<details>
<summary>Пример ответа при ошибках в валидации при создании события</summary>

```json
{
  "type": "Event",
  "status": 400,
  "detail": "Дата окончания события не может быть раньше даты начала",
  "instance": "00000000-0000-0000-0000-000000000000"
}
```
</details>

### Пример запроса с фильтрацией

````
https://localhost:5001/api/events?title=6&from=2026-03-19T22:13:23.372Z&page=1&pageSize=2
````
где надо найти все события, содержащие в названии текст **2** с датой начала меньше или равной **2026-03-19T22:13:23.372Z** с показом страницы **1** и с выводом по **2** элемента на странице


