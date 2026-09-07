# Firewall Packet Analyzer

## 🚀 Ключевые возможности
- Полное управление правилами через REST API (CRUD + `allow` / `block`)
- Автоматическая синхронизация с `iptables` (очистка цепочки `INPUT` + приоритеты)
- Приоритеты правил (Priority) — можно задать любой порядок
- Мониторинг в реальном времени: количество правил, соотношение ALLOW/DENY, uptime, CPU, RAM
- Docker-деплоймент с правами `NET_ADMIN`

## 📂 Структура проекта
```bash
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
```bash

Запуск в Docker
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

## 🚀 Дорожная карта
- [ ] JWT / OAuth2 авторизация (вместо API Key)
- [ ] Полный анализ пакетов через NFQueue (pcap, deep packet inspection)
- [ ] Load testing (NUnit + Testcontainers)
- [ ] Графический интерфейс (Blazor или React)
- [ ] IPv6 support (ip6tables + dual stack)
- [ ] Prometheus + Grafana мониторинг
- [ ] Kubernetes (Helm)
