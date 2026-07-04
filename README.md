<br />
<div align="center">
  <a href="https://github.com/Skilllab/EventBookingService">
    <img src="logo.png" alt="Logo" width="80" height="80">
  </a>

  <h1 align="center">EventBookingService</h1>
  <p align="center">
    Набор микросервисов для управления пользователями, событиями и бронированием
  </p>
</div>

## О проекте

`EventBookingService` — это solution с несколькими .NET 10 микросервисами:

- `EventForge.Users` — регистрация, логин и JWT-аутентификация
- `EventForge.Events` — управление событиями и доступными местами
- `EventForge.Booking` — создание, обработка и отмена бронирований

Решение использует слои `Domain`, `Application`, `Infrastructure`, `Presentation` для каждого сервиса.

## Архитектура решения

Схема зависимостей внутри каждого сервиса:

```text
Presentation -> Application -> Domain
                  ^
            Infrastructure
```

Общие проекты:

- `EventForge.Shared/EventForge.Contract` — контракты Kafka-сообщений
- `EventForge.Shared/EventForge.Entities` — общие перечисления и shared entities
- `EventForge.Shared/EventForge.ExceptionMiddleware` — middleware для обработки ошибок
- `EventForge.Shared/EventForge.LoggingDBInterceptor` — DB interceptor и инфраструктурные расширения

## Сервисы

### EventForge.Users

Основные компоненты:

- `AuthService`
- `PasswordHasher`
- `JwtTokenGenerator`
- `UserRepository`

### EventForge.Events

Основные компоненты:

- `EventService`
- `EventRepository`
- `ProcessedMessageRepository`
- `BookingConfirmedConsumer`
- `BookingRejectedConsumer`
- `BookingCancelledConsumer`

### EventForge.Booking

Основные компоненты:

- `BookingService`
- `BookingRepository`
- `OutboxRepository`
- `BookingBackgroundService`
- `OutboxPublisherBackgroundService`
- `KafkaBookingConfirmedPublisher`

## Технологии

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core 10
- PostgreSQL
- Confluent.Kafka
- xUnit 3
- FluentAssertions
- Moq
- Testcontainers for .NET

## Аутентификация и роли

Система использует JWT Bearer authentication с ролями `User` и `Admin`.

- `User` может просматривать события, создавать и отменять свои бронирования
- `Admin` дополнительно может создавать, изменять и удалять события, а также отменять любые бронирования

JWT создаётся сервисом `EventForge.Users`.

## Kafka и асинхронные процессы

Kafka используется для межсервисного обмена событиями. Основной сценарий:

1. `Booking` создаёт бронь со статусом `Pending`
2. `BookingBackgroundService` обрабатывает pending-брони
3. `Booking` сохраняет событие в outbox
4. `OutboxPublisherBackgroundService` публикует его в Kafka
5. `Events` consumer уменьшает или освобождает места
6. `Events` хранит обработанные сообщения в `ProcessedMessages` для идемпотентности

Контракты Kafka лежат в `EventForge.Shared/EventForge.Contract/Brokers`.

## Запуск проекта

### Требования

- .NET 10 SDK
- Docker Desktop
- PostgreSQL 16+ либо контейнер PostgreSQL
- Kafka broker для полного межсервисного сценария

### Локальный запуск сервисов

```bash
dotnet run --project EventForge.Users/EventForge.Users.Presentation
dotnet run --project EventForge.Events/EventForge.Events.Presentation
dotnet run --project EventForge.Booking/EventForge.Booking.Presentation
```

### Настройка подключения к БД

Для каждого сервиса можно использовать `appsettings.Development.json`, user secrets или переменные окружения.

Пример для Users:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=eventforge_users;Username=postgres;Password=postgres" --project EventForge.Users/EventForge.Users.Presentation
```

Аналогично настраиваются `EventForge.Events.Presentation` и `EventForge.Booking.Presentation`.

### Настройка Kafka

В `appsettings*.json` сервисов `Booking` и `Events` используется секция `KafkaOptions`.

## Миграции

Миграции создаются отдельно для каждого сервиса.

### Users

```bash
dotnet ef migrations add <MigrationName> --project EventForge.Users/EventForge.Users.Infrastructure --startup-project EventForge.Users/EventForge.Users.Presentation
```

### Events

```bash
dotnet ef migrations add <MigrationName> --project EventForge.Events/EventForge.Events.Infrastructure --startup-project EventForge.Events/EventForge.Events.Presentation
```

### Booking

```bash
dotnet ef migrations add <MigrationName> --project EventForge.Booking/EventForge.Booking.Infrastructure --startup-project EventForge.Booking/EventForge.Booking.Presentation
```

## Тестирование

В репозитории есть старые монолитные тесты в `tests/` и новые тестовые проекты для микросервисов в `EventForge.*/Tests`.

### Users

```bash
dotnet test EventForge.Users/Tests/EventForge.Users.UnitTests/EventForge.Users.UnitTests.csproj
dotnet test EventForge.Users/Tests/EventForge.Users.IntegrationTests/EventForge.Users.IntegrationTests.csproj
```

### Events

```bash
dotnet test EventForge.Events/Tests/EventForge.Events.UnitTests/EventForge.Events.UnitTests.csproj
dotnet test EventForge.Events/Tests/EventForge.Events.IntegrationTests/EventForge.Events.IntegrationTests.csproj
```

### Booking

```bash
dotnet test EventForge.Booking/Tests/EventForge.Booking.UnitTests/EventForge.Booking.UnitTests.csproj
dotnet test EventForge.Booking/Tests/EventForge.Booking.IntegrationTests/EventForge.Booking.IntegrationTests.csproj
```

Integration-тесты используют `Testcontainers.PostgreSql`, поэтому для них нужен запущенный Docker Desktop.

### Что покрыто тестами

- `Users`: `AuthService`, `PasswordHasher`, `JwtTokenGenerator`, `UserRepository`, миграции
- `Events`: `EventService`, Kafka consumers, `EventRepository`, `ProcessedMessageRepository`, миграции
- `Booking`: `BookingService`, Kafka/outbox background logic, `BookingRepository`, `OutboxRepository`, миграции

## Структура репозитория

```text
EventForge.Booking/
  EventForge.Booking.Application/
  EventForge.Booking.Domain/
  EventForge.Booking.Infrastructure/
  EventForge.Booking.Presentation/
  Tests/

EventForge.Events/
  EventForge.Events.Application/
  EventForge.Events.Domain/
  EventForge.Events.Infrastructure/
  EventForge.Events.Presentation/
  Tests/

EventForge.Users/
  EventForge.Users.Application/
  EventForge.Users.Domain/
  EventForge.Users.Infrastructure/
  EventForge.Users.Presentation/
  Tests/

EventForge.Shared/
  EventForge.Contract/
  EventForge.Entities/
  EventForge.ExceptionMiddleware/
  EventForge.LoggingDBInterceptor/

tests/
  EventBookingService.UnitTests/
  EventBookingService.IntegrationTests/
```

## Примечания

- Старые проекты в папке `tests/` относятся к предыдущей структуре решения
- Актуальное покрытие и развитие сейчас ведётся в `EventForge.Users`, `EventForge.Events` и `EventForge.Booking`
- Для полного end-to-end сценария между сервисами, кроме PostgreSQL, потребуется Kafka broker
