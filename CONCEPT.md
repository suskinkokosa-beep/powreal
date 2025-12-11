# Power Realms - Концепция / Concept

[English](#english) | [Русский](#russian)

---

<a name="russian"></a>
## Русский

### Описание проекта

**Power Realms** — децентрализованная P2P платформа для объединения вычислительных ресурсов. Каждый экземпляр приложения является одновременно:

- **Сервером** — предоставляет REST API и P2P интерфейс
- **Клиентом** — может подключаться к другим узлам
- **Базой данных** — встроенная SQLite для хранения данных

### Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                    Power Realms Desktop                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │   Avalonia  │  │  REST API   │  │   SQLite    │          │
│  │     GUI     │  │  (порт 5000)│  │    (БД)     │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│                          │                                    │
│  ┌───────────────────────┴────────────────────────┐          │
│  │              P2P Network Layer                  │          │
│  │              (TCP порт 5001)                    │          │
│  └─────────────────────────────────────────────────┘         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
         ┌──────────────────────────────────────┐
         │         Другие узлы сети             │
         │  (на разных ПК по всему миру)        │
         └──────────────────────────────────────┘
```

### Ключевые функции

#### 1. P2P Сеть
- Автоматическое обнаружение узлов
- Синхронизация данных между узлами
- Устойчивость к отключению отдельных узлов

#### 2. Пулы ресурсов
- **Публичные пулы** — открыты для всех
- **Приватные пулы** — доступ по паролю
- **Реферальные пулы** — доступ по приглашению

#### 3. Ноды (вычислительные узлы)
- Регистрация вычислительных ресурсов (CPU/GPU)
- Рейтинговая система
- Мониторинг производительности

#### 4. Маркетплейс
- Создание предложений на продажу ресурсов
- Покупка вычислительного времени
- Внутренняя валюта пула

#### 5. Игровые сессии (GameBoost)
- Аренда вычислительных ресурсов для игр
- Метрики качества (latency, FPS, uptime)
- Автоматические расчёты и выплаты

#### 6. Система споров
- Открытие споров при проблемах
- Разрешение офицерами пула
- Автоматические возвраты

#### 7. Вывод средств
- Запрос на вывод с холдом
- Подтверждение и выполнение

### Роли пользователей

| Роль | Права |
|------|-------|
| Member | Базовые операции |
| Officer | Управление пулом, разрешение споров |
| Owner | Полный контроль над пулом |
| GlobalAdmin | Администрирование системы |

### Технологии

- **.NET 8.0** — основная платформа
- **Avalonia UI** — кроссплатформенный GUI
- **SQLite** — встроенная база данных
- **TCP Sockets** — P2P связь
- **JWT** — аутентификация

### Порты

| Порт | Назначение |
|------|------------|
| 5000 | REST API (Swagger UI) |
| 5001 | P2P Network (TCP) |

### Как это работает

1. **Запуск приложения**
   - Создаётся локальная SQLite база данных
   - Запускается REST API на порту 5000
   - Запускается P2P слой на порту 5001

2. **Подключение к сети**
   - Узел подключается к bootstrap peers
   - Обменивается списком известных узлов
   - Начинается синхронизация данных

3. **Работа в пуле**
   - Пользователь регистрируется/входит
   - Создаёт или присоединяется к пулу
   - Регистрирует свои вычислительные ноды
   - Предоставляет или потребляет ресурсы

4. **Синхронизация**
   - Данные автоматически реплицируются
   - Изменения распространяются по сети
   - Конфликты разрешаются по timestamp

---

<a name="english"></a>
## English

### Project Description

**Power Realms** is a decentralized P2P platform for pooling computing resources. Each application instance is simultaneously:

- **Server** — provides REST API and P2P interface
- **Client** — can connect to other nodes
- **Database** — embedded SQLite for data storage

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Power Realms Desktop                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐          │
│  │   Avalonia  │  │  REST API   │  │   SQLite    │          │
│  │     GUI     │  │  (port 5000)│  │    (DB)     │          │
│  └─────────────┘  └─────────────┘  └─────────────┘          │
│                          │                                    │
│  ┌───────────────────────┴────────────────────────┐          │
│  │              P2P Network Layer                  │          │
│  │              (TCP port 5001)                    │          │
│  └─────────────────────────────────────────────────┘         │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
         ┌──────────────────────────────────────┐
         │           Other network nodes        │
         │     (on different PCs worldwide)     │
         └──────────────────────────────────────┘
```

### Key Features

#### 1. P2P Network
- Automatic node discovery
- Data synchronization between nodes
- Resilience to individual node disconnections

#### 2. Resource Pools
- **Public pools** — open to everyone
- **Private pools** — password protected
- **Referral pools** — invitation access

#### 3. Nodes (Computing Nodes)
- Registration of computing resources (CPU/GPU)
- Rating system
- Performance monitoring

#### 4. Marketplace
- Creating offers to sell resources
- Buying computing time
- Internal pool currency

#### 5. Gaming Sessions (GameBoost)
- Renting computing resources for games
- Quality metrics (latency, FPS, uptime)
- Automatic calculations and payouts

#### 6. Dispute System
- Opening disputes for issues
- Resolution by pool officers
- Automatic refunds

#### 7. Withdrawals
- Withdrawal request with hold
- Confirmation and execution

### User Roles

| Role | Permissions |
|------|-------------|
| Member | Basic operations |
| Officer | Pool management, dispute resolution |
| Owner | Full control over pool |
| GlobalAdmin | System administration |

### Technologies

- **.NET 8.0** — main platform
- **Avalonia UI** — cross-platform GUI
- **SQLite** — embedded database
- **TCP Sockets** — P2P communication
- **JWT** — authentication

### Ports

| Port | Purpose |
|------|---------|
| 5000 | REST API (Swagger UI) |
| 5001 | P2P Network (TCP) |

### How It Works

1. **Application Startup**
   - Local SQLite database is created
   - REST API starts on port 5000
   - P2P layer starts on port 5001

2. **Network Connection**
   - Node connects to bootstrap peers
   - Exchanges list of known nodes
   - Data synchronization begins

3. **Working in a Pool**
   - User registers/logs in
   - Creates or joins a pool
   - Registers computing nodes
   - Provides or consumes resources

4. **Synchronization**
   - Data is automatically replicated
   - Changes propagate across the network
   - Conflicts resolved by timestamp
