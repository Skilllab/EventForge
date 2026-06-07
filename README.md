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

## О проекте

Данный проект является по сути PET проектом при прохождении курса "Продвинутая разработка на C# и .NET"

## Архитектура проекта

Проект построен по принципам **Clean Architecture** и разделён на четыре слоя, каждый из которых отвечает за свою зону ответственности.
    
### Domain (`EventBookingService.Domain`)
    
Слой, описывающий предметную область. **Не зависит от технологий и внешних фреймворков.**
    
**Содержит:**
- **Доменные сущности** — `Event` (событие), `Booking` (бронь), `BookingStatus` (перечисление статусов брони), `PagedResult<T>` (обобщённый результат с пагинацией). Сущности используют статические фабричные методы (`Event.Create(...)`, `Booking.Create(...)`) и инкапсулируют бизнес-правила (например, `Event.TryReserveSeats()`, `Booking.Confirm()`).
- **Доменные исключения** — `NotFoundException` (ресурс не найден → 404), `NoAvailableSeatsException` (нет доступных мест → 409 Conflict), `ValidationCustomException` (нарушение бизнес-правил → 400). Все исключения наследуются от базового `ApplicationBaseException`, хранящего `EntityName` и `EntityId` для формирования `ProblemDetails`.
    
### Application (`EventBookingService.Application`)
   
Слой, описывающий **бизнес-логику и абстракции**. Зависит только от Domain.
   
**Содержит:**
- **Интерфейсы сервисов (Use Cases)** — `IEventService` (создание/изменение/отмена/получение событий), `IBookingService` (создание/обработка/получение броней). Реализации: `EventService`, `BookingService`.
- **Интерфейсы портов (абстракции для доступа к данным)** — `IEventRepository`, `IBookingRepository` (репозитории), `ITransactionService`, `ITransactionContext` (управление транзакциями). Application определяет, что ему нужно от Infrastructure, через эти интерфейсы.
- **DTO (объекты передачи данных)** — `EventDto`, `CreateEventDto`, `UpdateEventDto`, `BookingInfoDto`, `EventsFilterDto`, `PaginatedResultDto`. Используются для обмена данными между слоями Presentation и Application, а также для маппинга в доменные модели.
- **Фоновые сервисы** — логика фоновой обработки броней описана в `BookingService.UpdateBookingAsync` (вызывается из Infrastructure-хоста `BookingBackgroundService`).
- **Extension-метод для DI** — `AddApplication()` в `DependencyInjection.cs` регистрирует сервисы (`IEventService`, `IBookingService`) и `TimeProvider.System`.
    
### Infrastructure (`EventBookingService.Infrastructure`)
    
Слой, содержащий **реализации, зависящие от внешних технологий**. Зависит от Application (реализует его интерфейсы).
    
**Содержит:**
- **Реализации репозиториев** — `EventRepository`, `BookingRepository`. Используют `IDbContextFactory<AppDbContext>` для создания короткоживущих контекстов БД. Включают методы для работы внутри транзакции (`GetByIdWithLockInContextAsync` с SQL `FOR UPDATE`, `AddInContextAsync`, `UpdateInContextAsync`).
- **DbContext и маппинг сущностей** — `AppDbContext` (Npgsql, схема `EventBooking`). Конфигурации сущностей: `EventConfiguration`, `BookingConfiguration` (имена таблиц, столбцов в snake_case, индексы, каскадное удаление).
- **Миграции** — авто-применение при старте приложения (`db.Database.Migrate()`), ручное создание через `dotnet ef migrations add`.
- **Адаптеры к внешним системам** — `BookingBackgroundService` (`BackgroundService`, опрашивает pending-брони каждые 5 секунд), `LoggingInterceptor` (перехватчик команд EF Core, логирует медленные запросы >500 мс).
- **Extension-метод для DI** — `AddInfrastructure(IConfiguration)` в `DependencyInjection.cs` регистрирует `AppDbContext`, репозитории, `TransactionService`, `BookingBackgroundService` и `LoggingInterceptor`.
    
### Presentation (`EventBookingService.Presentation`)
    
Слой, отвечающий за **взаимодействие с внешним миром через HTTP API**.
   
**Содержит:**
- **Контроллеры/эндпоинты** — `EventsController` (CRUD событий + создание брони), `BookingsController` (получение информации о брони). Получают HTTP-запрос, вызывают нужный сервис из Application, возвращают ответ. Используют DTO запросов/ответов (`CreateEventRequest`, `EventResponse` и др.) с атрибутами валидации (`[DateGreater]`, `[NotMinDateTime]`, `[Required]`).
- **Глобальный обработчик исключений** — `GlobalExceptionHandlingMiddleware`. Перехватывает все необработанные исключения и маппит доменные исключения в HTTP-статусы: `NotFoundException` → 404, `ValidationCustomException` → 400, `NoAvailableSeatsException` → 409, `UnauthorizedAccessException` → 401, всё остальное → 500. Ответ формируется в формате `ProblemDetails (RFC 7807)`.
- **Composition Root** — `Program.cs`. Компактная регистрация всех зависимостей через три extension-метода: `AddInfrastructure()`, `AddApplication()`, `AddPresentation()`. Здесь же выполняется автоматическая миграция БД при старте и подключается Swagger.
    
```
Presentation ──→ Application ──→ Domain
                      ↑
               Infrastructure
```
    
<!-- GETTING STARTED -->
## Getting Started

Ниже описана инструкция по первоначальной настройке проекта.

### Необходимые компоненты

Проект разрабатывается на VS 2022 с использованием ASP .NET Core 10

### Требования

- В системе должен быть установлен Docker Desktop
- В системе должен быть установлен PostgreSQL (не ниже 16) для хранения данных в БД

### Подключение
для установки подключения к БД настройте секреты
```sh
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=ИМЯ_ХОСТА_ИЛИ_localhost;Port=5432;Database=ИМЯ_БД;Username=ЛОГИН;Password=ПАРОЛЬ"
```

### Установка

1. Клонируйте репозиторий
   ```sh
   git clone https://github.com/Skilllab/EventBookingService.git
   ```
2. Откройте папку с решением и выполните сборку проекта Presentation
   ```sh
   dotnet build EventBookingService.Presentation
   ```
3. Запустите сборку проекта с Unit тестами
   ```sh
   dotnet build tests\EventBookingService.UnitTests
   ```
4. Запустите выполнение Unit тестов
   ```sh
    dotnet test --project tests\EventBookingService.UnitTests
   ```
5. Запустите сборку проекта с Интеграционными тестами
   ```sh
   dotnet build tests\EventBookingService.IntegrationTests
   ```
6. Запустите Docker Desktop

7. Запустите выполнение Интеграционных тестов
   ```sh
    dotnet test --project tests\EventBookingService.IntegrationTests
   ```
8. Запустите проект
   ```sh
   dotnet run --project EventBookingService.Presentation
   ```
     База данных будет создана исходя из строки подключения и при выполнении миграции при старте программы.
 
 ### Запуск через Docker Compose
 
 Для быстрого запуска всего окружения (БД + приложение) выполните:
 ```sh
 docker-compose up
 ``` 
 Эта команда поднимет контейнеры с PostgreSQL и приложением, автоматически применит миграции и запустит сервис.

 Перед запуском создайте файл .env с описанием секретов

  ```sh
 # Базовые секреты
DB_USER=postgres
DB_PASSWORD=postgres
DB_NAME=eventapi
DB_HOST=postgres_db
DB_PORT=5438

# Строка подключения в формате .NET Npgsql
# Используем переменные, объявленные выше
CONNECTION_STRING=Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}

# Секреты для pgAdmin
PGADMIN_EMAIL=admin@admin.com
PGADMIN_PASSWORD=admin_password_987
 ``` 

---

## Использование API

При запущенном проекте в браузере введите адрес на swagger [https://localhost:5001/swagger/index.html](https://localhost:5001/swagger/index.html) 

## Модели данных

### Event

Модель представляет сущность события. Используется как внутренняя доменная модель и может быть преобразована в DTO для передачи через API.

**Общая информация:**

- **Назначение:** хранение данных о событии.
- **Создание:** через статический метод `string title, DateTime startDate, DateTime endDate, int totalSeats, string? description = null`.

**Поля модели:**

| Поле             | Тип        | Обязательное | Описание           |
|------------------|------------|--------------|--------------------
| `Id`             | `Guid`     | Да           | Уникальный идентификатор события
| `Title`          | `string`   | Да           | Название события
| `Description`    | `string`   | Нет          | Описание события
| `StartAt`        | `DateTime` | Да           | Дата начала события
| `EndAt`          | `DateTime` | Да           | Дата завершения события
| `TotalSeats`     | `int`      | Да           | Общее количество мест на событии
| `AvailableSeats` | `int`      | Да           | Текущее количество свободных мест



**Детализация полей:**

#### `id` (`Guid`, обязательное, только для чтения после создания)
- **Описание:** уникальный идентификатор события.
- **Генерация:** автоматически при вызове метода `Create`.
- **Пример:** `"98aa6f8e-91c8-4485-9779-bb244b8a67bb"`.

#### `Title` (`string`, обязательное)
- **Описание:** Текстовое название события.
- **Пример:** `"Новое красивое событие"`.

#### `Description` (`string`, не обязательное)
- **Описание:** Текстовое описание события.
- **Пример:** `"Это событие должно положить начало новой эры событий"`.

#### `StartAt` (`DateTime`, обязательное)
- **Описание:** дата и время начала события.
- **Особенности:** может учитывать временной пояс региона.
- **Формат:** ISO 8601 (`YYYY-MM-DDTHH:MM:SSZ`).
- **Пример:** `"2024-05-20T10:15:00Z"`.

#### `EndAt` (`DateTime`, обязательное)
- **Описание:** дата и время окончания события.
- **Особенности:** может учитывать временной пояс региона.
- **Формат:** ISO 8601 (`YYYY-MM-DDTHH:MM:SSZ`).
- **Пример:** `"2024-06-20T10:15:00Z"`.

#### `TotalSeats` (`int`, обязательное)
- **Описание:** Общее количество мест на событии.
- **Особенности:** не может быть меньше нуля или равно нулю.
- **Пример:** `2`.

#### `AvailableSeats` (`int`, обязательное)
- **Описание:** Количество доступных мест на событии.
- **Особенности:** не может быть меньше нуля. Изначально равно общему количеству метс.
- **Пример:** `2`.

### Booking

Модель представляет сущность брони события. Используется как внутренняя доменная модель и может быть преобразована в DTO для передачи через API.

**Общая информация:**

- **Назначение:** хранение данных о бронировании события.
- **Создание:** через статический метод `Create(Guid eventId, DateTime createdAt)`.
- **Начальный статус:** `BookingStatus.Pending`.
- **ID брони:** генерируется автоматически при создании (`Guid.NewGuid()`).

**Поля модели:**

| Поле | Тип | Обязательное |
|------|---------|-------------|
| `id` | `Guid` | Да |
| `eventId` | `Guid` | Да |
| `status` | `BookingStatus` | Да |
| `createdAt` | `DateTime` | Да |
| `processedAt` | `DateTime | null` | Нет |

**Детализация полей:**

#### `id` (`Guid`, обязательное, только для чтения после создания)
- **Описание:** уникальный идентификатор брони.
- **Генерация:** автоматически при вызове метода `Create`.
- **Пример:** `"123e4567-e89b-12d3-a456-426614174000"`.

#### `eventId` (`Guid`, обязательное)
- **Описание:** идентификатор события, к которому относится бронь.
- **Назначение:** связь брони с конкретным событием.
- **Пример:** `"a1b2c3d4-e5f6-7890-abcd-ef1234567890"`.

#### `status` (`BookingStatus`, обязательное)
- **Описание:** текущий статус брони.
- **Возможные значения:**
  - `Pending` — ожидание обработки;
  - `Confirmed` — подтверждена;
  - `Cancelled` — отменена;
  - `Failed` — ошибка при обработке.
- **Начальное значение:** `Pending`.

#### `createdAt` (`DateTime`, обязательное, только для чтения после создания)
- **Описание:** дата и время создания брони.
- **Особенности:** может учитывать временной пояс региона.
- **Формат:** ISO 8601 (`YYYY-MM-DDTHH:MM:SSZ`).
- **Пример:** `"2024-05-20T10:15:00Z"`.

#### `processedAt` (`DateTime | null`, необязательное)
- **Описание:** дата и время обработки брони (когда статус был изменён с `Pending`).
- **Значение по умолчанию:** `null` (пока бронь не обработана).
- **Устанавливается:** при изменении статуса брони.
- **Формат:** ISO 8601.
- **Пример:** `"2024-05-20T11:30:00Z"`.

## Операции с Событиями

События, это запланированные с датой и описанием мероприятия.

### Endpoints

<details>
<summary>Получить список всех событий</summary>

* **Метод:** `GET`
* **URL:** `/Events`
* **Параметры запроса:**
  * `title` (опционально) - фильтрация событий по содержанию (вхождение строки)
  * `from` (опционально) - фильтрация событий по дате начала (дата начала больше или равна заданной)
  * `to` (опционально) - фильтрация событий по дате окончания (дата окончания меньше или равна заданной)
  * `page` (опционально, по умолчанию `1`) — номер страницы
  * `pageSize` (опционально, по умолчанию `10`) — количество записей на странице
* **Пример запроса:**
```bash
  curl -X 'GET' \
  'https://localhost:5001/Events?title=11&from=2026-04-03&to=2026-04-04&page=2&pageSize=4' \
  -H 'accept: */*'
```

* **Пример ответа**
```json
{
  "eventsTotalCount": 3,
  "events": [
    {
      "id": "f50d1bf8-a6a4-4f31-9e97-0878d97c6fd9",
      "title": "Событие 1",
      "description": "",
      "startAt": "2026-04-03T07:54:33.751Z",
      "endAt": "2026-04-03T07:54:33.751Z"
      "totalSeats": 2,
      "availableSeats": 2
    },
    {
      "id": "ff8da171-3154-463c-bc89-c776b270baee",
      "title": "Событие 2",
      "description": "",
      "startAt": "2026-04-03T07:54:33.751Z",
      "endAt": "2026-04-03T07:54:33.751Z"
      "totalSeats": 2,
      "availableSeats": 2

    },
    {
      "id": "789dc5c9-2bd8-40b8-ad81-18d53e0d3bcd",
      "title": "Событие 3",
      "description": "",
      "startAt": "2026-04-03T07:54:33.751Z",
      "endAt": "2026-04-03T07:54:33.751Z"
      "totalSeats": 2,
      "availableSeats": 2
    }
  ],
  "currentPageNumber": 1,
  "eventsCountOnCurrentPage": 10
}
```

</details>

<details>
<summary>Получить событие по идентификатору</summary>

* **Метод:** `GET`
* **URL:** `/Events/{eventId}`
* **Параметры запроса:**
  * `eventId` (обязательно) - идентификатор события
* **Пример запроса:**
```bash
  curl -X 'GET' \
  'https://localhost:5001/Events/f50d1bf8-a6a4-4f31-9e97-0878d97c6fd9' \
  -H 'accept: */*'
```

* **Пример ответа**

```json
{
  "id": "f50d1bf8-a6a4-4f31-9e97-0878d97c6fd9",
  "title": "Событие 23",
  "description": "string",
  "startAt": "2026-04-03T07:54:33.751Z",
  "endAt": "2026-04-03T07:54:33.751Z"
  "totalSeats": 2,
  "availableSeats": 2
}
```
</details>

<details>
<summary>Создать новое событие</summary>

* **Метод:** `POST`
* **URL:** `/Events/`
* **Параметры запроса:**
  * `title` (обязательно) - Наименование события
  * `description` (необязательное) - Описание события
  * `startAt` (обязательно) - Дата начала события
  * `endAt` (обязательно) - Дата окончания события 
  * `totalSeats` (обязательно) - Общее количество мест  
* **Пример запроса:**
 
```bash
curl -X 'POST' \
  'https://localhost:5001/Events' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "title": "Новое событие",
  "description": "",
  "startAt": "2026-04-03T07:54:33.751Z",
  "endAt": "2026-04-03T07:54:33.751Z",
  "totalSeats": 2
}'
```

* **Пример ответа**

```json
{
  "id": "f50d1bf8-a6a4-4f31-9e97-0878d97c6fd9",
  "title": "Новое событие",
  "description": "",
  "startAt": "2026-04-03T07:54:33.751Z",
  "endAt": "2026-04-03T07:54:33.751Z"
  "totalSeats": 2,
  "availableSeats": 2
}
```

</details>


<details>
<summary>Обновить событие целиком</summary>

* **Метод:** `POST`
* **URL:** `/Events/{eventId}`
* **Параметры запроса:**
  * `title` (необязательное) - Наименование события
  * `description` (необязательное) - Описание события
  * `startAt` (необязательное) - Дата начала события
  * `endAt` (необязательное) - Дата окончания события  
* **Пример запроса:**
 
```bash
  curl -X 'POST' \
  'https://localhost:5001/Events' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "title": "Событие из базы",
  "description": "",
  "startAt": "2026-04-03T07:54:33.751Z",
  "endAt": "2026-04-03T07:54:33.751Z"
}'
```
</details>

<details>
<summary>Удалить/отменить событие</summary>

* **Метод:** `DELETE`
* **URL:** `/Events/{eventId}`
* **Параметры запроса:**
  * `eventId` (обязательно) - идентификатор события
* **Пример запроса:**
 
```bash
curl -X 'DELETE' \
  'https://localhost:5001/Events/f05c1bab-70f1-4c76-a321-758e84509672' \
  -H 'accept: */*'
```
</details>


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

---

## Операции с Бронированием

Бронирование событий основано на фоновом сервисе, который периодически опрашивает репозиторий с созданныйи бронированиями, и переводит их (бронирования) в соответствующий статус.

Пример Happy Path для бронирования

* Создаем событие
```bash
curl -X 'POST' \
  'https://localhost:5001/Events' \
  -H 'accept: */*' \
  -H 'Content-Type: application/json' \
  -d '{
  "title": "Новое событие",
  "description": "",
  "startAt": "2026-04-03T07:54:33.751Z",
  "endAt": "2026-04-03T07:54:33.751Z",
  "totalSeats": 1
}'
```
из ответа забираем id

```json
{
  "id": "ea482456-8da9-4026-9c8f-278bd1206b13",
  "title": "string",
  "description": "string",
  "startAt": "2026-04-03T08:27:17.998Z",
  "endAt": "2026-04-03T08:27:17.998Z"
  "totalSeats": 1,
  "availableSeats": 1
}
```

* Создаем бронирование
```bash
curl -X 'POST' \
  'https://localhost:5001/Events/ea482456-8da9-4026-9c8f-278bd1206b13/book' \
  -H 'accept: */*' \
  -d ''
```

из ответа забираем id

```json
{
  "id": "07c5ba9a-900a-43ba-931f-ee5f9ec58d79",
  "eventID": "ea482456-8da9-4026-9c8f-278bd1206b13",
  "status": "Pending"
}
```

* Через 5 секунд проверяем статус бронирования
```bash
curl -X 'GET' \
  'https://localhost:5001/Bookings/07c5ba9a-900a-43ba-931f-ee5f9ec58d79' \
  -H 'accept: */*'
```

### Endpoints

<details>
<summary>Создать новое бронирование</summary>

* **Метод:** `POST`
* **URL:** `/Events/{eventId}/book`
* **Параметры запроса:**
  * `eventId` (обязательно) - идентификатор события
* **Пример запроса:**
 
```bash
curl -X 'POST' \
  'https://localhost:5001/Events/f1981d77-e952-420f-b9a1-13a8e2efcf8f/book' \
  -H 'accept: */*' \
  -d ''
``` 
* **Пример ответа**
  * `id` - идентификатор бронирования
  * `eventID` - идентификатор события
  * `status` - статус брнонирования
```json
{
  "id": "d1a37063-0700-4c60-ae98-095be738c682",
  "eventID": "f1981d77-e952-420f-b9a1-13a8e2efcf8f",
  "status": "Pending"
}
```

</details>

### Блокировка от овербукинга
 
 Метод `CreateBookingAsync` в `BookingService` использует **блокировку на уровне транзакции БД** для предотвращения овербукинга:
 
 1. Операция выполняется внутри транзакции (`TransactionService.ExecuteAsync`)
 2. При получении события используется **блокировка строки `FOR UPDATE`** (`GetByIdWithLockInContextAsync`) — это гарантирует, что никто другой не изменит `AvailableSeats` до завершения транзакции
 3. Проверка и резервирование места (`TryReserveSeats`) и сохранение брони происходят атомарно в одной транзакции
 
 Таким образом, даже при параллельных запросах на бронирование последнего места только один запрос будет успешен, а остальные получат ошибку `409 Conflict`.


<details>
<summary>Получить информацию по бронированию</summary>

* **Метод:** `GET`
* **URL:** `/Bookings/{bookingId}`
* **Параметры запроса:**
  * `bookingId` (обязательно) - идентификатор бронирования
* **Пример запроса:**
 
```bash
curl -X 'GET' \
  'https://localhost:5001/Bookings/d1a37063-0700-4c60-ae98-095be738c682' \
  -H 'accept: */*'
```

* **Пример ответа**
  * `id` - идентификатор бронирования
  * `eventID` - идентификатор события
  * `status` - статус брнонирования
```json
{
  "id": "d1a37063-0700-4c60-ae98-095be738c682",
  "eventID": "f1981d77-e952-420f-b9a1-13a8e2efcf8f",
  "status": "Confirmed"
}
```

</details>

### Формат ошибок при ответе основан на [ProblemDetails (RFC 7807)](https://datatracker.ietf.org/doc/html/rfc7807)  

<details>
<summary>Пример ответа при ненайденном событии</summary>

```json
{
    "type": "Event",
    "status": 404,
    "detail": "Элемент Event c ID: 'b4d85802-6c58-4413-82b2-1e7fdbbf8fd5' не найден.",
    "instance": "b4d85802-6c58-4413-82b2-1e7fdbbf8fd5"
}
```
</details>


<details>
<summary>Пример ответа при невозможности бронирования из-за недостаточного количества мест</summary>

```json
{
    "type": "Event",
    "status": 409,
    "detail": "Для элемента Event c ID: a0949a65-e080-4d51-9fc4-247054eedc71 нет доступных мест для бронирования",
    "instance": "a0949a65-e080-4d51-9fc4-247054eedc71"
}
```
</details>

---

## Миграции БД

-
-
-
-
   Миграции находятся в проекте **Infrastructure** (`EventBookingService.Infrastructure`), так как именно этот слой отвечает за доступ к данным и содержит `AppDbContext`, конфигурации маппинга сущностей (`EventConfiguration`, `BookingConfiguration`) и всё, что связано с технологией хранения (Entity Framework Core + Npgsql).
+
   При запуске приложения миграции применяются автоматически — в `Program.cs` (Presentation) вызывается `db.Database.Migrate()`, поэтому ручное обновление БД требуется только при отладке или ручном развёртывании.
+
   ### Структура миграций в проекте
+
   ```
   EventBookingService.Infrastructure/
   └── Migrations/
       ├── {timestamp}_InitialCreate.cs
       ├── {timestamp}_InitialCreate.Designer.cs
       ├── {timestamp}_NextMigration.cs
       └── AppDbContextModelSnapshot.cs
   ```
+
   ### Создание новой миграции
+
   Для создания новой миграции выполните команду из **корня решения**:
+
   ```bash
   dotnet ef migrations add <ИмяМиграции> --project EventBookingService.Infrastructure --startup-project EventBookingService.Presentation
   ```
+
   Где:
+
   | Параметр | Значение | Пояснение |
   |----------|----------|------------|
   | `<ИмяМиграции>` | Например `AddEventCapacity`, `AddBookingIndex` | Осмысленное имя на PascalCase, отражающее суть изменений |
   | `--project` | `EventBookingService.Infrastructure` | Проект, где находится `AppDbContext` и папка `Migrations/` |
   | `--startup-project` | `EventBookingService.Presentation` | Проект с `Program.cs`, где сконфигурирована строка подключения и DI |
+
   **Важно:** Поскольку `AppDbContext` использует `IDbContextFactory<AppDbContext>`, миграции должны создаваться с флагом `--startup-project`, указывающим на Presentation — именно там зарегистрирована фабрика контекстов и строка подключения.
+
   ### Ручное обновление базы данных
+
   Для явного применения миграций к базе данных (без запуска приложения) выполните:
+
   ```bash
   dotnet ef database update --project EventBookingService.Infrastructure --startup-project EventBookingService.Presentation
   ```
+
   ### Удаление последней миграции
+
   Если миграция была создана ошибочно и ещё не применена к БД:
+
   ```bash
   dotnet ef migrations remove --project EventBookingService.Infrastructure --startup-project EventBookingService.Presentation
   ```
+
   ### Принцип работы в рамках Clean Architecture
+
   1. **Domain** определяет сущности (`Event`, `Booking`) без привязки к БД
   2. **Application** описывает интерфейсы репозиториев (`IEventRepository`, `IBookingRepository`)
   3. **Infrastructure** реализует репозитории, содержит `AppDbContext`, конфигурации маппинга и миграции — это единственный слой, который «знает» о том, как сущности хранятся в PostgreSQL
   4. **Presentation** при старте вызывает `Migrate()` и передаёт строку подключения через `appsettings.json` / user-secrets
+
   Такое разделение позволяет заменить БД или ORM, не затрагивая Domain и Application — достаточно изменить только Infrastructure.
