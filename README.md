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

## Содержание

- [О проекте](#о-проекте)
- [Quick Start](#quick-start)
- [Архитектура решения](#архитектура-решения)
- [Сервисы](#сервисы)
- [Технологии](#технологии)
- [Аутентификация и роли](#аутентификация-и-роли)
- [Kafka и асинхронные процессы](#kafka-и-асинхронные-процессы)
- [Локальная dev-схема запуска](#локальная-dev-схема-запуска)
- [Запуск проекта](#запуск-проекта)
- [Миграции](#миграции)
- [Тестирование](#тестирование)
- [API примеры](#api-примеры)

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

Назначение:
- регистрация пользователей
- логин
- генерация JWT

Основные компоненты:

- `AuthService`
- `PasswordHasher`
- `JwtTokenGenerator`
- `UserRepository`

### EventForge.Events

Назначение:
- CRUD для событий
- управление доступными местами
- реакция на Kafka-события бронирования

Основные компоненты:

- `EventService`
- `EventRepository`
- `ProcessedMessageRepository`
- `BookingConfirmedConsumer`
- `BookingRejectedConsumer`
- `BookingCancelledConsumer`

### EventForge.Booking

Назначение:
- создание бронирований
- фоновая обработка pending-бронирований
- публикация интеграционных событий в Kafka через outbox
- отмена бронирований

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

JWT содержит как минимум:

- `sub` — GUID пользователя
- `role` — `User` или `Admin`

## Kafka и асинхронные процессы

Kafka используется для межсервисного обмена событиями. Основной сценарий:

1. `Booking` создаёт бронь со статусом `Pending`
2. `BookingBackgroundService` обрабатывает pending-брони
3. `Booking` сохраняет событие в outbox
4. `OutboxPublisherBackgroundService` публикует его в Kafka
5. `Events` consumer уменьшает или освобождает места
6. `Events` хранит обработанные сообщения в `ProcessedMessages` для идемпотентности

Контракты Kafka лежат в `EventForge.Shared/EventForge.Contract/Brokers`.

Используемые типы сообщений:

- `BookingConfirmed`
- `BookingRejected`
- `BookingCancelled`

Для `Booking` важно, что публикация идёт через outbox, а для `Events` — что обработка идемпотентна.

## Запуск проекта

### Требования

- .NET 10 SDK
- Docker Desktop
- PostgreSQL 16+ либо контейнер PostgreSQL
- Kafka broker для межсервисного сценария

Предлагается 2 сценария запуска:

1. Локально через Visual Studio или Rider (только сервисы)
2. Через Docker Compose (все сервисы + инфраструктура)

#### Сценарий 1: 

- Запуск окружения

```bash
docker compose up -d zookeeper kafka akhq postgres_users postgres_events postgres_booking pgadmin
```

- Запуск через IDE (Visual Studio, Rider) с конфигурацией `Только сервисы` (три Web API проекта)
или Локальный запуск сервисов

```bash
dotnet run --project EventForge.Users/EventForge.Users.Presentation
dotnet run --project EventForge.Events/EventForge.Events.Presentation
dotnet run --project EventForge.Booking/EventForge.Booking.Presentation
```

#### Сценарий 2: 
- Запуск через Docker Compose (все сервисы + инфраструктура)
```bash 
docker compose up -d
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

Пример:

```json
{
  "KafkaOptions": {
    "BootstrapServers": "localhost:9092",
    "ConsumerGroup": "eventforge-events"
  }
}
```

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

Применение миграций:

```bash
dotnet ef database update --project <InfrastructureProject> --startup-project <PresentationProject>
```

## Тестирование

В репозитории есть тестовые проекты для микросервисов в `EventForge.*/Tests`.

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

## API примеры

### Users API

Регистрация пользователя:

```bash
curl -X POST 'http://localhost:5008/auth/register' \
  -H 'Content-Type: application/json' \
  -d '{
    "login": "admin-user",
    "password": "SecurePassword123!",
    "role": "Admin"
  }'
```

Логин:

```bash
curl -X POST 'http://localhost:5008/auth/login' \
  -H 'Content-Type: application/json' \
  -d '{
    "login": "admin-user",
    "password": "SecurePassword123!"
  }'
```

### Events API

Создание события под `Admin`:

```bash
curl -X POST 'http://localhost:5009/Events' \
  -H 'Authorization: Bearer <JWT>' \
  -H 'Content-Type: application/json' \
  -d '{
    "title": "DotNet Meetup",
    "description": "Community event",
    "startAt": "2026-08-15T10:00:00Z",
    "endAt": "2026-08-15T13:00:00Z",
    "totalSeats": 50
  }'
```

Получение списка событий:

```bash
curl 'http://localhost:5009/Events?title=dotnet&page=1&pageSize=10'
```

### Booking API

Создание бронирования:

```bash
curl -X POST 'http://localhost:5010/Bookings/<EVENT_ID>' \
  -H 'Authorization: Bearer <JWT>'
```

Получение бронирования:

```bash
curl 'http://localhost:5010/Bookings/<BOOKING_ID>' \
  -H 'Authorization: Bearer <JWT>'
```

Отмена бронирования:

```bash
curl -X DELETE 'http://localhost:5010/Bookings/<BOOKING_ID>' \
  -H 'Authorization: Bearer <JWT>'
```

## Примечания

- Для полного end-to-end сценария между сервисами, кроме PostgreSQL, потребуется Kafka broker
