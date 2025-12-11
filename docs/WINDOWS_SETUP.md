# Power Realms — Инструкция по запуску на Windows

[English](#english) | [Русский](#russian)

---

<a name="russian"></a>
## Русский

### Описание
Power Realms — **децентрализованная P2P система**, где каждый экземпляр приложения является:
- **Сервером** (REST API на порту 5000)
- **Клиентом** (может подключаться к другим узлам)
- **Базой данных** (встроенная SQLite)

Чем больше экземпляров запущено на разных ПК — тем обширнее возможности сети!

### Требования
- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Шаг 1: Установка .NET 8.0

1. Скачайте установщик .NET 8.0 SDK с официального сайта
2. Запустите установщик и следуйте инструкциям
3. Проверьте установку:
   ```powershell
   dotnet --version
   ```
   Должна отобразиться версия 8.0.x

### Шаг 2: Клонирование проекта

```powershell
git clone <URL_репозитория>
cd PowerRealms
cd src\PowerRealms.Api
```

### Шаг 3: Настройка конфигурации

Отредактируйте файл `appsettings.json`:

```json
{
  "Database": {
    "Path": "powerrealms.db"
  },
  "P2P": {
    "Port": 5001,
    "BootstrapPeers": []
  },
  "Jwt": {
    "Key": "YourSecretKeyAtLeast32Characters!",
    "Issuer": "PowerRealms",
    "Audience": "PowerRealmsUsers"
  },
  "SeedAdmin": {
    "Username": "admin",
    "Password": "your-password",
    "IsGlobalAdmin": true
  }
}
```

### Шаг 4: Запуск приложения

```powershell
dotnet restore
dotnet run
```

Приложение будет доступно по адресу: http://localhost:5000

### Шаг 5: Проверка работы

Откройте в браузере:
- Swagger UI: http://localhost:5000/swagger/index.html
- Информация об узле: http://localhost:5000/api/p2p/info

---

## Подключение нескольких ПК (P2P сеть)

### Вариант 1: Через конфигурацию

Добавьте адреса других узлов в `BootstrapPeers`:

**ПК 1 (192.168.1.100)** — первый узел:
```json
{
  "P2P": {
    "Port": 5001,
    "BootstrapPeers": []
  }
}
```

**ПК 2 (192.168.1.101)** — подключается к первому:
```json
{
  "P2P": {
    "Port": 5001,
    "BootstrapPeers": ["192.168.1.100:5001"]
  }
}
```

**ПК 3 (192.168.1.102)** — подключается ко всем:
```json
{
  "P2P": {
    "Port": 5001,
    "BootstrapPeers": ["192.168.1.100:5001", "192.168.1.101:5001"]
  }
}
```

### Вариант 2: Динамическое подключение через API

```bash
POST http://localhost:5000/api/p2p/connect
Content-Type: application/json

{
  "endpoint": "192.168.1.100:5001"
}
```

---

## Порты

| Порт | Назначение |
|------|------------|
| 5000 | REST API (Swagger UI) |
| 5001 | P2P связь между узлами (TCP) |

### Настройка файрвола

Разрешите входящие подключения на порты 5000 и 5001:

```powershell
New-NetFirewallRule -DisplayName "PowerRealms API" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "PowerRealms P2P" -Direction Inbound -LocalPort 5001 -Protocol TCP -Action Allow
```

---

## API Endpoints

### P2P
- `GET /api/P2P/info` — Информация об узле и подключённых пирах
- `POST /api/P2P/connect` — Подключиться к узлу
- `GET /api/P2P/peers` — Список подключённых узлов
- `POST /api/P2P/sync` — Запросить синхронизацию данных

### Аутентификация
- `POST /api/Auth/register` — Регистрация
- `POST /api/Auth/login` — Вход
- `GET /api/Auth/languages` — Список языков

### Пулы и балансы
- `GET /api/Pools` — Список пулов
- `POST /api/Pools` — Создать пул
- `GET /api/Balance/pool/{poolId}` — Баланс в пуле

### Ноды
- `POST /api/Nodes` — Зарегистрировать ноду
- `GET /api/Nodes/my` — Мои ноды
- `GET /api/Nodes/available` — Доступные ноды

### Маркетплейс
- `GET /api/Marketplace` — Список предложений
- `POST /api/Marketplace` — Создать предложение

### Споры
- `POST /api/Disputes` — Открыть спор
- `POST /api/Disputes/{id}/resolve` — Разрешить спор

### Вывод средств
- `POST /api/Withdrawals/request` — Запросить вывод
- `POST /api/Withdrawals/{id}/approve` — Одобрить вывод

---

## Синхронизация данных

Между узлами автоматически синхронизируются:
- Пользователи
- Пулы и участники
- Транзакции
- Предложения маркетплейса
- Ноды

---

## Учётные данные по умолчанию

- **Логин:** admin
- **Пароль:** admin123!
- **Роль:** GlobalAdmin

---

## Устранение неполадок

| Проблема | Решение |
|----------|---------|
| Порт 5000 занят | Измените порт в Program.cs |
| Не подключается к другому узлу | Проверьте файрвол и сетевое соединение |
| Нет доступа к API | Убедитесь что узел запущен |

---

<a name="english"></a>
## English

### Description
Power Realms is a **decentralized P2P system** where each application instance is:
- **Server** (REST API on port 5000)
- **Client** (can connect to other nodes)
- **Database** (embedded SQLite)

The more instances running on different PCs — the more powerful the network!

### Requirements
- Windows 10/11
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Step 1: Install .NET 8.0

1. Download .NET 8.0 SDK installer from the official website
2. Run the installer and follow the instructions
3. Verify installation:
   ```powershell
   dotnet --version
   ```
   Should display version 8.0.x

### Step 2: Clone the Project

```powershell
git clone <repository_URL>
cd PowerRealms
cd src\PowerRealms.Api
```

### Step 3: Configure

Edit `appsettings.json`:

```json
{
  "Database": {
    "Path": "powerrealms.db"
  },
  "P2P": {
    "Port": 5001,
    "BootstrapPeers": []
  },
  "Jwt": {
    "Key": "YourSecretKeyAtLeast32Characters!",
    "Issuer": "PowerRealms",
    "Audience": "PowerRealmsUsers"
  },
  "SeedAdmin": {
    "Username": "admin",
    "Password": "your-password",
    "IsGlobalAdmin": true
  }
}
```

### Step 4: Run the Application

```powershell
dotnet restore
dotnet run
```

The application will be available at: http://localhost:5000

### Step 5: Verify It Works

Open in browser:
- Swagger UI: http://localhost:5000/swagger/index.html
- Node info: http://localhost:5000/api/p2p/info

---

## Connecting Multiple PCs (P2P Network)

### Option 1: Via Configuration

Add other nodes' addresses to `BootstrapPeers`:

**PC 1 (192.168.1.100)** — first node:
```json
{
  "P2P": {
    "Port": 5001,
    "BootstrapPeers": []
  }
}
```

**PC 2 (192.168.1.101)** — connects to first:
```json
{
  "P2P": {
    "Port": 5001,
    "BootstrapPeers": ["192.168.1.100:5001"]
  }
}
```

### Option 2: Dynamic Connection via API

```bash
POST http://localhost:5000/api/p2p/connect
Content-Type: application/json

{
  "endpoint": "192.168.1.100:5001"
}
```

---

## Ports

| Port | Purpose |
|------|---------|
| 5000 | REST API (Swagger UI) |
| 5001 | P2P communication (TCP) |

### Firewall Configuration

Allow incoming connections on ports 5000 and 5001:

```powershell
New-NetFirewallRule -DisplayName "PowerRealms API" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
New-NetFirewallRule -DisplayName "PowerRealms P2P" -Direction Inbound -LocalPort 5001 -Protocol TCP -Action Allow
```

---

## Default Credentials

- **Username:** admin
- **Password:** admin123!
- **Role:** GlobalAdmin

---

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Port 5000 is busy | Change the port in Program.cs |
| Can't connect to another node | Check firewall and network connection |
| No API access | Make sure the node is running |
