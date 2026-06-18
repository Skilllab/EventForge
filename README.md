<br />
<div align="center">
  <a href="https://github.com/Skilllab/EventBookingService">
    <img src="logo.png" alt="Logo" width="80" height="80">
  </a>

  <h1 align="center">EventBookingService</h1>
  <p align="center">
    Сервис для бронирования билетов на мероприятия   
  </p>
</div>

## Содержание

- [О проекте](#о-проекте)
- [Архитектура проекта](#архитектура-проекта)
- [Ролевая модель и разграничение прав](#ролевая-модель-и-разграничение-прав)
- [Аутентификация и JWT](#аутентификация-и-jwt)
  - [Получение JWT-токена через Swagger](#получение-jwt-токена-через-swagger)
  - [Настройка JWT в конфигурации](#настройка-jwt-в-конфигурации)
- [Getting Started](#getting-started)
  - [Установка и запуск](#установка-и-запуск)
  - [Запуск через Docker Compose](#запуск-через-docker-compose)
- [Использование API](#использование-api)
- [Модели данных](#модели-данных)
- [Операции с Событиями](#операции-с-событиями)
- [Операции с Бронированием](#операции-с-бронированием)
- [Миграции БД](#миграции-бд)
- [Тестирование](#тестирование)
- [Безопасность](#безопасность)

---

## О проекте

EventBookingService — это RESTful API для управления событиями и бронированием билетов, построенный на принципах Clean Architecture с использованием ASP.NET Core 10, Entity Framework Core и PostgreSQL.

**Ключевые особенности:**
- **JWT-аутентификация** с ролевой моделью доступа (User/Admin)
- **Защита от овербукинга** через транзакционные блокировки БД (`FOR UPDATE`)
- **Фоновая обработка броней** с автоматическим изменением статусов
- **Автоматические миграции БД** при старте приложения
- **Полное покрытие unit и integration тестами**

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
- **Контроллеры/эндпоинты:**
  - `AuthController` — регистрация (`POST /auth/register`) и аутентификация (`POST /auth/login`) пользователей, возврат JWT-токена
  - `EventsController` — CRUD событий (создание/изменение/удаление требуют роли Admin, просмотр публичный), создание брони (требует JWT)
  - `BookingsController` — получение информации о бронировании (требует JWT), отмена брони (владелец или Admin)
- **Глобальный обработчик исключений** — `GlobalExceptionHandlingMiddleware`. Перехватывает все необработанные исключения и маппит доменные исключения в HTTP-статусы: `NotFoundException` → 404, `ValidationCustomException` или `BookingPastEventException` → 400, `NoAvailableSeatsException` или `BookingLimitExceededException` → 409, `UnauthorizedAccessException` → 401, `InsufficientPermissionsException` → 403, всё остальное → 500. Ответ формируется в формате `ProblemDetails (RFC 7807)`.
- **JWT-аутентификация и авторизация:**
  - JWT Bearer authentication настроена в `DependencyInjection.cs` с валидацией по `Issuer`, `Audience`, `Lifetime` и `Secret`
  - Middleware `UseAuthentication()` и `UseAuthorization()` зарегистрированы в `Program.cs`
  - Защищённые endpoints используют `[Authorize(Policy = "CustomJwtPolicy")]` или `[Authorize(Roles = "Admin")]`
- **Composition Root** — `Program.cs`. Компактная регистрация всех зависимостей через три extension-метода: `AddInfrastructure()`, `AddApplication()`, `AddPresentation()`. Здесь же выполняется автоматическая миграция БД при старте и подключается Swagger с поддержкой JWT Bearer авторизации.

```
Presentation ──→ Application ──→ Domain
                      ↑
               Infrastructure
```

---

## Ролевая модель и разграничение прав

Сервис использует **JWT-аутентификацию** с **двухуровневой ролевой моделью**:

### Роли пользователей

| Роль | Описание | Права доступа |
|------|----------|---------------|
| `User` | Обычный пользователь | • Просмотр событий<br>• Создание собственных броней<br>• Просмотр и отмена **только своих** броней<br>• Лимит активных броней: 10 (настраивается в `BookingOptions.MaxBookingCount`) |
| `Admin` | Администратор | • **Все права User**<br>• Создание, изменение и удаление событий<br>• Отмена **любых** броней |

### Разграничение доступа к API endpoints

#### Публичные endpoints (без аутентификации)

```http
GET  /Events              # Список всех событий (с фильтрацией и пагинацией)
GET  /Events/{eventId}    # Информация о конкретном событии
POST /auth/register       # Регистрация нового пользователя
POST /auth/login          # Аутентификация и получение JWT-токена
```

#### Endpoints, требующие аутентификации (роль User или Admin)

```http
POST   /Events/{eventId}/book        # Создание брони на событие
GET    /Bookings/{bookingId}         # Получение информации о бронировании
DELETE /Bookings/{bookingId}         # Отмена брони (только владелец или Admin)
```

**Бизнес-правила для User:**
- Может создать бронь на будущее событие (если `Event.StartAt > now`)
- Не может забронировать прошедшее событие → `400 BookingPastEventException`
- Не может превысить лимит активных броней (по умолчанию 10) → `409 BookingLimitExceededException`
- Не может отменить чужую бронь → `403 InsufficientPermissionsException`

#### Admin-only endpoints (требуют роль Admin)

```http
POST   /Events                # Создание нового события
PUT    /Events/{eventId}      # Изменение существующего события
DELETE /Events/{eventId}      # Удаление/отмена события
```

**Дополнительные права Admin:**
- Может отменять **любые** брони (в том числе чужие)
- Не ограничен лимитом активных броней

### Реализация в коде

**Защита endpoints:**

```csharp
// EventsController.cs
[Authorize(Policy = StringConstants.CustomJwtPolicy)] // Базовая защита JWT для всего контроллера
[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    [AllowAnonymous] // Переопределяем — публичный доступ
    [HttpGet]
    public async Task<IActionResult> GetAllEvents(...) { }

    [Authorize(Roles = nameof(RoleType.Admin))] // Только Admin
    [HttpPost]
    public async Task<IActionResult> CreateEvent(...) { }

    [HttpPost("{eventId}/book")] // User или Admin (наследуется от контроллера)
    public async Task<IActionResult> CreateBook(...) { }
}
```

**Проверка прав владения брони:**

```csharp
// BookingService.cs
public async Task<bool> CancelBooking(Guid bookingId, string userLogin, CancellationToken ct)
{
    var user = await userRepository.GetByLoginAsync(userLogin);
    var booking = await bookingRepository.GetByIdAsync(bookingId, ct);

    // Проверка: владелец брони ИЛИ Admin
    if (booking.UserId != user.Id && user.Role != RoleType.Admin)
        throw new InsufficientPermissionsException(...);

    // Отмена брони...
}
```

---

## Аутентификация и JWT

### Получение JWT-токена через Swagger

1. **Регистрация пользователя**

   Откройте Swagger UI: [https://localhost:5001/swagger/index.html](https://localhost:5001/swagger/index.html)

   Найдите endpoint `POST /auth/register` и выполните запрос:

   ```json
   {
     "login": "testuser",
     "password": "SecurePassword123!",
     "role": "User"
   }
   ```

   **Доступные роли:** `"User"` или `"Admin"`

   **Успешный ответ:** `204 No Content`

2. **Получение JWT-токена**

   Найдите endpoint `POST /auth/login` и выполните запрос:

   ```json
   {
     "login": "testuser",
     "password": "SecurePassword123!"
   }
   ```

   **Успешный ответ:**
   ```json
   {
     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJ0ZXN0dXNlciIsInJvbGUiOiJVc2VyIiwianRpIjoiMTIzZTQ1NjctZTg5Yi0xMmQzLWE0NTYtNDI2NjE0MTc0MDAwIiwiaWF0IjoxNzE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"
   }
   ```

   **Скопируйте значение `token`.**

3. **Авторизация в Swagger**

   - Нажмите кнопку **Authorize** в правом верхнем углу Swagger UI
   - В поле **Value** введите: `Bearer <ваш_токен>`
     ```
     Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
     ```
   - Нажмите **Authorize**, затем **Close**

4. **Использование защищённых endpoints**

   Теперь все запросы к защищённым endpoints (`POST /Events/{eventId}/book`, `GET /Bookings/{bookingId}` и т.д.) будут автоматически включать JWT-токен в заголовке `Authorization: Bearer <токен>`.

   **Пример с curl:**
   ```bash
   curl -X 'POST' \
     'https://localhost:5001/Events/ea482456-8da9-4026-9c8f-278bd1206b13/book' \
     -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...' \
     -H 'Content-Type: application/json'
   ```

### Структура JWT-токена

Токен содержит следующие claims:

| Claim | Значение | Описание |
|-------|----------|----------|
| `sub` (`Name`) | Логин пользователя | Используется для идентификации пользователя в `BookingService.CancelBooking` |
| `role` | `User` или `Admin` | Проверяется атрибутом `[Authorize(Roles = "Admin")]` |
| `jti` | GUID | Уникальный идентификатор токена |
| `iat` | Unix timestamp | Время выдачи токена |

### Настройка JWT в конфигурации

#### Конфигурация для разработки (`appsettings.Development.json`)

```json
{
  "JwtSettings": {
    "SchemeName": "EventBookingScheme",
    "Secret": "SuperSecretKeyWithMoreThan32CharactersLength!!",
    "Issuer": "EventBookingService",
    "Audience": "EventBookingAPI",
    "Lifetime": 2
  }
}
```

| Параметр | Тип | Описание |
|----------|-----|----------|
| `SchemeName` | string | Название схемы аутентификации (используется в `AddAuthentication()`) |
| `Secret` | string | **Секретный ключ для подписи JWT** (минимум 32 символа для HMAC SHA-256) |
| `Issuer` | string | Издатель токена (проверяется при валидации) |
| `Audience` | string | Целевая аудитория токена (проверяется при валидации) |
| `Lifetime` | int | Время жизни токена в **часах** |

#### Безопасность JWT Secret в продакшне

**⚠️ ВАЖНО:** Значение `JwtSettings:Secret` из `appsettings.Development.json` предназначено **только для разработки** и не должно использоваться в продакшне!

**Рекомендации для production:**

1. **Используйте User Secrets (для локального тестирования):**
   ```bash
   dotnet user-secrets set "JwtSettings:Secret" "YourProductionSecretKeyWith64+Characters!!!"
   ```

2. **Используйте переменные окружения (для контейнеров/серверов):**
   ```bash
   export JwtSettings__Secret="YourProductionSecretKeyWith64+Characters!!!"
   ```

   Для Docker Compose:
   ```yaml
   # docker-compose.yml
   services:
     api:
       environment:
         - JwtSettings__Secret=${JWT_SECRET}
   ```

   ```env
   # .env (не коммитить в Git!)
   JWT_SECRET=YourProductionSecretKeyWith64+Characters!!!
   ```

3. **Требования к секрету:**
   - Минимум **32 символа** (для HMAC SHA-256)
   - Рекомендуется **64+ символов**
   - Используйте криптографически стойкий генератор случайных данных
   - Разные секреты для dev/staging/production окружений
   - **Никогда** не коммитьте секрет в Git
   - **Никогда** не публикуйте секрет в логах или ответах API

**Проверка конфигурации при старте:**

Приложение автоматически валидирует наличие всех обязательных параметров JWT при старте в `DependencyInjection.cs`:

```csharp
var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
if (string.IsNullOrEmpty(jwtSettings?.Secret) || jwtSettings.Secret.Length < 32)
    throw new InvalidOperationException("JwtSettings:Secret must be at least 32 characters long!");
```
    
---
## Getting Started

Ниже описана инструкция по первоначальной настройке проекта.

### Необходимые компоненты

Проект разрабатывается на VS 2026 с использованием ASP .NET Core 10

### Требования

- **Docker Desktop** — для запуска интеграционных тестов и контейнеризации
- **PostgreSQL 16+** — для хранения данных (может быть запущен локально или через Docker)
- **.NET 10 SDK** — для сборки и запуска приложения

### Подключение к БД

Для настройки строки подключения к PostgreSQL используйте **User Secrets** (рекомендуется для разработки):

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=YOUR_PASSWORD" --project EventBookingService.Presentation
```

**Альтернатива:** переменная окружения (для Docker/production):
```bash
export ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=eventapi;Username=postgres;Password=YOUR_PASSWORD"
```

### Настройка JWT для разработки

Для локальной разработки JWT уже настроен в `appsettings.Development.json`. Для production используйте User Secrets:

```bash
dotnet user-secrets set "JwtSettings:Secret" "YourSecureProductionSecretWith64+Characters!!!" --project EventBookingService.Presentation
```

**Важно:** Секрет должен содержать минимум 32 символа. См. раздел [Настройка JWT в конфигурации](#настройка-jwt-в-конфигурации) для деталей.

### Установка и запуск

1. **Клонируйте репозиторий**
   ```bash
   git clone https://github.com/Skilllab/EventBookingService.git
   cd EventBookingService
   ```

2. **Настройте подключение к БД** (см. раздел [Подключение к БД](#подключение-к-бд))

3. **Соберите проект Presentation**
   ```bash
   dotnet build EventBookingService.Presentation
   ```

4. **Запустите Unit-тесты**
   ```bash
   dotnet test tests/EventBookingService.UnitTests
   ```

5. **Запустите Integration-тесты** (требуется Docker Desktop)
   ```bash
   # Запустите Docker Desktop, затем:
   dotnet test tests/EventBookingService.IntegrationTests
   ```

6. **Запустите приложение**
   ```bash
   dotnet run --project EventBookingService.Presentation
   ```

   База данных будет создана автоматически при первом запуске (миграции применяются в `Program.cs`).

7. **Откройте Swagger UI**

   Перейдите по адресу: [https://localhost:5001/swagger/index.html](https://localhost:5001/swagger/index.html)
 
 ### Запуск через Docker Compose

 Для быстрого запуска всего окружения (PostgreSQL + приложение) выполните:

 1. **Создайте файл `.env`** в корне проекта:

    ```env
    # Базовые настройки БД
    DB_USER=postgres
    DB_PASSWORD=postgres
    DB_NAME=eventapi
    DB_HOST=postgres_db
    DB_PORT=5438

    # Строка подключения (использует переменные выше)
    CONNECTION_STRING=Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}

    # Настройки pgAdmin
    PGADMIN_EMAIL=admin@admin.com
    PGADMIN_PASSWORD=admin_password_987
    ```

    **Важно:** Не коммитьте `.env` файл в Git! Он уже добавлен в `.gitignore`.

 2. **Запустите контейнеры:**

    ```bash
    docker-compose up
    ```

    Эта команда:
    - Поднимет PostgreSQL на порту `5438`
    - Автоматически применит миграции БД
    - Запустит приложение на порту `5001`
    - Поднимет pgAdmin на порту `5050` (доступ через `http://localhost:5050`)

 3. **Откройте Swagger UI:**

    [https://localhost:5001/swagger/index.html](https://localhost:5001/swagger/index.html)

 4. **Остановка контейнеров:**

    ```bash
    docker-compose down
    ```

---

## Использование API

### Быстрый старт с аутентификацией

1. **Зарегистрируйте пользователя** через Swagger UI или curl:
   ```bash
   curl -X 'POST' 'https://localhost:5001/auth/register' \
     -H 'Content-Type: application/json' \
     -d '{"login": "testuser", "password": "SecurePass123!", "role": "User"}'
   ```

2. **Получите JWT-токен:**
   ```bash
   curl -X 'POST' 'https://localhost:5001/auth/login' \
     -H 'Content-Type: application/json' \
     -d '{"login": "testuser", "password": "SecurePass123!"}'
   ```

   **Ответ:**
   ```json
   {
     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
   }
   ```

3. **Используйте токен в запросах:**
   ```bash
   curl -X 'POST' 'https://localhost:5001/Events/EVENT_ID/book' \
     -H 'Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...'
   ```

**Подробная инструкция:** см. раздел [Получение JWT-токена через Swagger](#получение-jwt-токена-через-swagger)

### Swagger UI

При запущенном приложении откройте Swagger UI для интерактивной работы с API:

[https://localhost:5001/swagger/index.html](https://localhost:5001/swagger/index.html)

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

Миграции находятся в проекте **Infrastructure** (`EventBookingService.Infrastructure`), так как именно этот слой отвечает за доступ к данным и содержит `AppDbContext`, конфигурации маппинга сущностей (`EventConfiguration`, `BookingConfiguration`) и всё, что связано с технологией хранения (Entity Framework Core + Npgsql).

При запуске приложения миграции применяются автоматически — в `Program.cs` (Presentation) вызывается `db.Database.Migrate()`, поэтому ручное обновление БД требуется только при отладке или ручном развёртывании.

### Создание новой миграции

   Для создания новой миграции выполните команду из **корня решения**:

   ```bash
   dotnet ef migrations add <ИмяМиграции> --project EventBookingService.Infrastructure --startup-project EventBookingService.Presentation
   ```

   Где:

   | Параметр | Значение | Пояснение |
   |----------|----------|------------|
   | `<ИмяМиграции>` | Например `AddEventCapacity`, `AddBookingIndex` | Осмысленное имя на PascalCase, отражающее суть изменений |
   | `--project` | `EventBookingService.Infrastructure` | Проект, где находится `AppDbContext` и папка `Migrations/` |
   | `--startup-project` | `EventBookingService.Presentation` | Проект с `Program.cs`, где сконфигурирована строка подключения и DI |

   **Важно:** Поскольку `AppDbContext` использует `IDbContextFactory<AppDbContext>`, миграции должны создаваться с флагом `--startup-project`, указывающим на Presentation — именно там зарегистрирована фабрика контекстов и строка подключения.

   ### Ручное обновление базы данных

   Для явного применения миграций к базе данных (без запуска приложения) выполните:

   ```bash
   dotnet ef database update --project EventBookingService.Infrastructure --startup-project EventBookingService.Presentation
   ```

   ### Удаление последней миграции

   Если миграция была создана ошибочно и ещё не применена к БД:

   ```bash
   dotnet ef migrations remove --project EventBookingService.Infrastructure --startup-project EventBookingService.Presentation
   ```

   ### Принцип работы в рамках Clean Architecture

   1. **Domain** определяет сущности (`Event`, `Booking`) без привязки к БД
   2. **Application** описывает интерфейсы репозиториев (`IEventRepository`, `IBookingRepository`)
   3. **Infrastructure** реализует репозитории, содержит `AppDbContext`, конфигурации маппинга и миграции — это единственный слой, который «знает» о том, как сущности хранятся в PostgreSQL
   4. **Presentation** при старте вызывает `Migrate()` и передаёт строку подключения через `appsettings.json` / user-secrets

   Такое разделение позволяет заменить БД или ORM, не затрагивая Domain и Application — достаточно изменить только Infrastructure.

---

## Тестирование

Проект содержит два уровня тестов:

### Unit-тесты (`EventBookingService.UnitTests`)

**Технологии:** xUnit, Moq, FluentAssertions

**Запуск:**
```bash
dotnet test tests/EventBookingService.UnitTests
```

### Integration-тесты (`EventBookingService.IntegrationTests`)

**Технологии:** xUnit, Testcontainers, WebApplicationFactory

**Особенности:**
- Используют **реальную PostgreSQL в Docker-контейнере** (через Testcontainers)
- Каждый тест работает с изолированной БД
- Проверяют **полный end-to-end flow** от HTTP-запроса до БД

**Запуск:** (требуется запущенный Docker Desktop)
```bash
dotnet test tests/EventBookingService.IntegrationTests
```

---

## Безопасность

### JWT-аутентификация

- Все защищённые endpoints требуют валидный JWT-токен
- Токены подписаны HMAC SHA-256 с валидацией `Issuer`, `Audience`, `Lifetime`
- Поддержка ролевой авторизации (`User`/`Admin`)

### Защита от овербукинга

- Транзакционные блокировки `FOR UPDATE` на уровне БД
- Атомарность операций резервирования места и создания брони

### Обработка ошибок

- Глобальный обработчик исключений `GlobalExceptionHandlingMiddleware`
- Стандартизированный формат ошибок `ProblemDetails (RFC 7807)`
- Логирование всех необработанных исключений с `RequestId`
