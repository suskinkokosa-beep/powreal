# Power Realms

## Обзор / Overview
Power Realms — это **децентрализованная P2P платформа** для объединения вычислительных ресурсов. Каждый экземпляр приложения является одновременно сервером, клиентом и базой данных.

Power Realms is a **decentralized P2P platform** for pooling computing resources. Each application instance is simultaneously a server, client, and database.

## Два режима работы / Two Operating Modes

### 1. Desktop GUI (Avalonia)
Полноценное десктопное приложение с графическим интерфейсом:
```
src/PowerRealms.Desktop/
```
- Кроссплатформенный GUI (Windows/Linux/macOS)
- Встроенная SQLite база данных
- P2P сетевой слой
- Не требует браузера

### 2. REST API (Headless)
Серверный режим без GUI:
```
src/PowerRealms.Api/
```
- Swagger UI для тестирования
- Можно использовать с любым фронтендом
- Подходит для серверов

## Архитектура / Architecture
- **Встроенная БД** — SQLite (каждый узел имеет свою локальную БД)
- **REST API** — Порт 5000
- **P2P слой** — Порт 5001 (TCP для связи между узлами)
- **Синхронизация** — Автоматическая синхронизация данных между узлами

## Структура проекта / Project Structure
```
src/
  PowerRealms.Api/           - REST API (headless mode)
    Controllers/             - API контроллеры
    Data/                    - Entity Framework DbContext (SQLite)
    Models/                  - Модели и перечисления
    P2P/                     - P2P сетевой слой
    Repositories/            - Доступ к данным
    Services/                - Бизнес-логика
    Resources/               - Файлы локализации
    
  PowerRealms.Desktop/       - Desktop GUI (Avalonia)
    Views/                   - XAML представления
    ViewModels/              - MVVM ViewModels
    Models/                  - Модели данных
    Data/                    - SQLite контекст
    P2P/                     - P2P сетевой слой
    
docs/
  WINDOWS_SETUP.md           - Инструкция по запуску на Windows
  
CONCEPT.md                   - Концепция проекта
```

## Стек технологий / Tech Stack
- .NET 8.0
- **Avalonia UI 11** — Desktop GUI
- Entity Framework Core 8.0 + **SQLite**
- JWT Bearer Authentication
- Swagger/OpenAPI
- BCrypt.Net
- TCP Sockets для P2P

## Локализация / Localization
Поддерживаемые языки / Supported languages:
- Русский (ru)
- English (en)

## API Endpoints

### P2P
- `GET /api/p2p/info` - Информация об узле
- `POST /api/p2p/connect` - Подключиться к узлу
- `GET /api/p2p/peers` - Список пиров
- `POST /api/p2p/sync` - Запросить синхронизацию

### Auth / Аутентификация
- `POST /api/auth/register` - Регистрация
- `POST /api/auth/login` - Вход
- `GET /api/auth/languages` - Доступные языки

### Pools / Пулы
- `POST /api/pools` - Создать пул
- `GET /api/pools` - Список пулов
- `POST /api/pools/{id}/join` - Присоединиться к пулу

### Nodes / Ноды
- `POST /api/nodes` - Зарегистрировать ноду
- `GET /api/nodes/my` - Мои ноды
- `GET /api/nodes/available` - Доступные ноды

### Balance / Балансы
- `GET /api/balance/pool/{poolId}` - Баланс в пуле
- `POST /api/balance/deposit` - Пополнить баланс
- `POST /api/balance/transfer` - Перевести средства

### Disputes / Споры
- `POST /api/disputes` - Открыть спор
- `POST /api/disputes/{id}/resolve` - Разрешить спор

### Marketplace / Маркетплейс
- `POST /api/marketplace/offer` - Создать предложение
- `GET /api/marketplace/pool/{poolId}` - Предложения пула

### Withdrawals / Выводы
- `POST /api/withdrawals/request` - Запрос на вывод
- `POST /api/withdrawals/confirm/{id}` - Подтвердить вывод

## Запуск / Running

### Desktop GUI (рекомендуется)
```bash
cd src/PowerRealms.Desktop
dotnet run
```

### API Mode (Replit)
Workflow автоматически запускает API на порту 5000.
Swagger UI: http://localhost:5000/swagger

### Windows
См. подробную инструкцию: `docs/WINDOWS_SETUP.md`

## Конфигурация / Configuration

### appsettings.json
```json
{
  "Database": {
    "Path": "powerrealms.db"
  },
  "P2P": {
    "Port": 5001,
    "BootstrapPeers": ["192.168.1.100:5001"]
  }
}
```

## Учётные данные по умолчанию / Default Credentials
- Username: admin
- Password: admin123!
- Role: GlobalAdmin

## Последние изменения / Recent Changes
- 2025-12-11: **Добавлен Desktop GUI** — Avalonia UI для Windows/Linux/macOS
- 2025-12-11: **Переход на SQLite** — теперь БД встроена в приложение
- 2025-12-11: **Добавлен P2P слой** — узлы могут связываться через TCP
- 2025-12-11: Добавлены контроллеры: Disputes, Nodes, Balance, P2P
- 2025-12-11: Создана концепция проекта (CONCEPT.md)
