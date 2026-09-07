# Firewall Packet Analyzer
**REST API для управления правилами Linux-файрволла** с интеграцией iptables, приоритетами, метриками и готовым Docker-деплоем.
Проект написан на **C# (.NET 10)** с использованием **ASP.NET Core**, **Entity Framework Core** и **PostgreSQL**. Правила хранятся в БД и синхронизируются с iptables в реальном времени.

## 🚀 Ключевые возможности
- ✅ **Полное управление правилами** через REST API (CRUD + `allow` / `block`)
- ⚡ **Автоматическая синхронизация с iptables** (очистка цепочки INPUT + приоритеты)
- 🔢 **Приоритеты правил** (`Priority`) — можно задать любой порядок
- 📊 **Мониторинг в реальном времени**: количество правил, соотношение ALLOW/DENY, uptime, CPU, RAM
- 🐳 **Docker-деплоймент** с правами `NET_ADMIN`

## 📂 Структура проекта
```
firewall-packetanalyzer/
├── FirewallApi/                     # Web API
│   ├── Controllers/                 # RulesController
│   ├── Services/                    # IptablesService, MetricsService
│   ├── Middlewares/                 # ApiKeyMiddleware
│   ├── Program.cs
│   └── appsettings.json
├── FirewallDb/                      # EF Core слой
│   ├── Models/Rule.cs
│   ├── Data/FirewallDbContext.cs
│   └── Migrations/
├── Dockerfile
├── docker-compose.yml
├── README.md
└── .gitignore
```

## 🧠 Логика приоритетов

Правила применяются в iptables **в порядке возрастания `Priority`** (чем меньше число, тем выше приоритет).

При добавлении/обновлении/удалении правила происходит **полная синхронизация**:

1. Очищается цепочка `INPUT` (`iptables -F INPUT`)
2. Все правила из БД сортируются по `Priority`
3. Правила добавляются в iptables (`-A INPUT ...`) в отсортированном порядке

## API Эндпоинты
Все эндпоинты доступны через **Swagger** (http://localhost:5087/swagger).

| Метод  | Эндпоинт                    | Описание                                      |
|--------|-----------------------------|-----------------------------------------------|
| GET    | `/api/Rules`                | Получить все правила                          |
| GET    | `/api/Rules/{id}`           | Получить правило по ID                        |
| POST   | `/api/Rules`                | Создать правило (с Action = ALLOW)            |
| POST   | `/api/Rules/allow`          | Создать ALLOW правило                         |
| POST   | `/api/Rules/block`          | Создать DENY правило                          |
| PUT    | `/api/Rules/{id}`           | Обновить правило                              |
| DELETE | `/api/Rules/{id}`           | Удалить правило                               |
| GET    | `/api/Rules/metrics`        | Метрики (кол-во правил, ALLOW/DENY, uptime, CPU, RAM) |
**Важно**: при создании/обновлении/удалении правила автоматически пересортировываются по `Priority`.

## 🐳 Запуск в Docker
# Сборка образа
docker build -t firewall-api .
# Запуск с правами NET_ADMIN
docker run -d \
  --name firewall-api \
  -p 5087:80 \
  --cap-add NET_ADMIN \
  -e ASPNETCORE_ENVIRONMENT=Development \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5433;Database=firewalldb;Username=postgres;Password=postgres" \
  firewall-api

## 🚀 Дорожная карта
- [ ] JWT / OAuth2 авторизация (вместо API Key)
- [ ] Полный анализ пакетов через NFQueue (pcap, deep packet inspection)
- [ ] Load testing (NUnit + Testcontainers)
- [ ] Графический интерфейс (Blazor или React)
- [ ] IPv6 support (ip6tables + dual stack)
- [ ] Prometheus + Grafana мониторинг
- [ ] Kubernetes (Helm)
