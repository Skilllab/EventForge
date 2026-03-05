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
2. Откройте папку с проектом и выполните сборку
   ```sh
   dotnet build -p WebAPI
   ```
4. Запустите проект
   ```sh
   dotnet run -p WebAPI
   ```

## Использование API

При запущенном проекте в браузере введите адрес на swagger [https://localhost:5001/swagger/index.html](https://localhost:5001/swagger/index.html) 

Доступные запросы

Вид запроса | Метод             | Параметр |Возвращаемое значение| Описание
----------- | ------------------| -------- |---------------------|---------
GET         | /api/Events       |          | Массив JSON         | Получить список всех событий
GET         | /api/Events/\{id} | GUID     | JSON                | Получить событие по id
POST        | /api/Events       | JSON     |                     | Создать новое событие
PUT         | /api/Events       | JSON     |                     | Обновить событие целиком
DELETE      | /api/Events/\{id} | GUID     |                     | Удалить событие
