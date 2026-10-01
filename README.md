# MessageGuard

Sistema de mensajería con clasificación inteligente de mensajes. Un usuario envía un mensaje de ayuda desde el frontend; el sistema lo clasifica automáticamente con IA (Jev de TypeSafe AI) y decide su destino:

| Clasificación | Estado final | Acción |
|---|---|---|
| `NORMAL` | `Sent` | Se envía, el usuario recibe un correo de confirmación |
| `URGENT` | `SentUrgent` | Se envía con prioridad, correo urgente |
| `SPAM` | `RejectedSpam` | Se rechaza, no se envía |
| `THREAT` | `RejectedThreat` | Se rechaza, no se envía |
| *(fallo de IA)* | `RejectedReview` | Queda en revisión manual |

---

## Stack tecnológico

| Capa | Tecnología |
|---|---|
| Backend | C# / .NET 10, ASP.NET Core Web API |
| Workers | BackgroundService (.NET hosted services) |
| Frontend | Next.js 15 (React, TypeScript, Tailwind CSS 4) |
| Base de datos | PostgreSQL 17 |
| Broker de mensajes | RabbitMQ 4 |
| Clasificador IA | Jev — TypeSafe AI (`api.typesafe.ai`) |
| Correo (local) | Mailpit (SMTP falso para desarrollo) |
| Contenedores | Docker / Docker Compose |

---

## Arquitectura

El proyecto sigue **arquitectura hexagonal (Ports & Adapters)**:

```
Domain          → entidades, enums, eventos de dominio (sin dependencias externas)
Application     → casos de uso y puertos (interfaces). Solo referencia a Domain.
Infrastructure  → adaptadores: Postgres, RabbitMQ, SMTP, Jev. Referencia a Application.
Api             → endpoints HTTP y composición de dependencias (DI).
```

### Flujo completo de un mensaje

```
[Frontend]
    │  POST /api/messages
    ▼
[API] SubmitMessage
    │  Guarda mensaje (PENDING) + evento en outbox
    │  (misma transacción SQL → atomico)
    ▼
[OutboxRelayWorker]  ← cada 5 segundos
    │  Lee outbox_messages WHERE published = false
    │  Publica a RabbitMQ (exchange: messageguard.events)
    ▼
[RabbitMQ] cola: messageguard.classify
    ▼
[ClassifyMessageConsumer]
    │  Llama a Jev (TypeSafe AI) → spam / threat / urgent / normal
    │  Actualiza estado en BD + guarda evento MessageApproved/MessageRejected
    ▼
[RabbitMQ] cola: messageguard.notify  (solo si fue aprobado)
    ▼
[SendApprovedMessageConsumer]
    │  Envía correo via SMTP (Mailpit en local)
    ▼
[Frontend] polling cada 3s → muestra estado final + confianza de la IA
```

### Robustez (Dead-Letter Queue)

```
Fallo en consumer
    │  nack(requeue: false)
    ▼
[messageguard.dlx]  ← fanout exchange
    ▼
[messageguard.dead]  ← cola de cuarentena
    ▼
[DeadLetterConsumer]
    ├── intentos < 3 → republica al exchange original (retry)
    └── intentos >= 3 → marca mensaje como RejectedReview en BD
```

---

## Prerequisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [dotnet-ef tools](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)

```bash
dotnet tool install --global dotnet-ef
```

---

## Instalación y configuración

### 1. Clonar el repositorio

```bash
git clone <url-del-repo>
cd ChatMessageJEV
```

### 2. Configurar secretos del backend

Copia el archivo de ejemplo y agrega tu API key de Jev:

```bash
cp Backend/MessageGuard.Api/appsettings.Development.example.json \
   Backend/MessageGuard.Api/appsettings.Development.json
```

Edita `appsettings.Development.json` y reemplaza `TU_API_KEY_DE_TYPESAFE_AI_AQUI` con tu key real.

> Obtén tu API key en [typesafe.ai](https://typesafe.ai). Sin ella, el sistema usa un clasificador de palabras clave como fallback (no necesita key).

### 3. Configurar el frontend

```bash
cp Frontend/.env.example Frontend/.env.local
```

El valor por defecto (`http://localhost:5055`) funciona para desarrollo local sin cambios.

### 4. Levantar la infraestructura (Docker)

```bash
docker compose up -d
```

Esto inicia:
- **PostgreSQL** en `localhost:5432`
- **RabbitMQ** en `localhost:5672` (management: `localhost:15672`)
- **Mailpit** en `localhost:8025` (SMTP: `localhost:1025`)

### 5. Crear la base de datos

```bash
cd Backend
dotnet ef database update \
  --project MessageGuard.Infrastructure \
  --startup-project MessageGuard.Api
```

### 6. Instalar dependencias del frontend

```bash
cd Frontend
npm install
```

---

## Ejecución

Abre **tres terminales**:

**Terminal 1 — Backend (API + Workers)**
```bash
cd Backend
dotnet run --project MessageGuard.Api
# Disponible en: http://localhost:5055
```

**Terminal 2 — Frontend**
```bash
cd Frontend
npm run dev
# Disponible en: http://localhost:3000
```

**Terminal 3 — Infraestructura** (si no está corriendo)
```bash
docker compose up -d
```

---

## Tests

```bash
cd Backend
dotnet test
```

27 tests unitarios cubriendo Domain y Application (sin dependencias externas).

---

## Paneles de monitoreo

| Panel | URL | Credenciales |
|---|---|---|
| RabbitMQ Management | http://localhost:15672 | guest / guest |
| Mailpit (bandeja de correos) | http://localhost:8025 | — |

### Consultar la BD directamente

```bash
docker exec -it <nombre-contenedor-postgres> psql -U messageguard -d messageguard
```

```sql
-- Ver todos los mensajes y su estado
SELECT id, sender_email, status, confidence, created_at FROM messages ORDER BY created_at DESC;

-- Ver el outbox (eventos publicados/pendientes)
SELECT event_type, published, created_at FROM outbox_messages ORDER BY created_at DESC LIMIT 20;

-- Ver eventos procesados (tabla de idempotencia)
SELECT COUNT(*) FROM processed_events;
```

---

## Estructura del proyecto

```
ChatMessageJEV/
├── docker-compose.yml
├── Backend/
│   ├── MessageGuard.Domain/          # Entidades, enums, eventos de dominio
│   │   ├── Entities/                 # Message, OutboxMessage, ProcessedEvent
│   │   ├── Enums/                    # MessageStatus, Classification
│   │   └── Events/                   # MessageSubmitted, MessageApproved, MessageRejected
│   ├── MessageGuard.Application/     # Casos de uso y puertos (interfaces)
│   │   ├── Ports/                    # IMessageRepository, IMessageClassifier, IEmailSender...
│   │   └── UseCases/                 # SubmitMessage, ClassifyMessage, NotifyApprovedMessage...
│   ├── MessageGuard.Infrastructure/  # Adaptadores externos
│   │   ├── Classifiers/              # JevClassifier, KeywordClassifier
│   │   ├── Mail/                     # MailpitEmailSender (MailKit)
│   │   ├── Messaging/                # RabbitMqEventPublisher
│   │   ├── Persistence/              # AppDbContext, migraciones EF Core
│   │   ├── Repositories/             # PostgresMessageRepository
│   │   └── Workers/                  # OutboxRelayWorker, ClassifyMessageConsumer,
│   │                                 # SendApprovedMessageConsumer, DeadLetterConsumer
│   ├── MessageGuard.Api/             # Endpoints HTTP, DI, configuración
│   └── MessageGuard.Tests/           # Tests unitarios (xUnit)
│       ├── Domain/                   # Tests de entidades
│       └── Application/              # Tests de casos de uso con fakes
└── Frontend/
    └── src/
        ├── domain/                   # Tipos TypeScript (Message, MessageStatus)
        ├── application/
        │   ├── ports/                # IMessageRepository (interfaz)
        │   └── useCases/             # submitMessage, getMessageStatus
        ├── infrastructure/           # HttpMessageRepository (fetch a la API)
        ├── container.ts              # Composición de dependencias del frontend
        ├── components/               # StatusBadge, ConfidenceBar
        └── app/                      # Next.js App Router (page.tsx, layout.tsx)
```

---

## Variables de entorno y configuración

### Backend — `appsettings.json` (comprometido, sin secretos)

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=messageguard;Username=messageguard;Password=messageguard"
  },
  "RabbitMq": { "Host": "localhost" },
  "Mail": { "Host": "localhost", "Port": "1025", "From": "noreply@messageguard.local" },
  "Jev": { "ApiKey": "" }
}
```

### Backend — `appsettings.Development.json` (ignorado por git, secretos locales)

```json
{
  "Jev": {
    "ApiKey": "tu-api-key-real"
  }
}
```

### Frontend — `.env.local` (ignorado por git)

```env
NEXT_PUBLIC_API_URL=http://localhost:5055
```

> En producción estas variables se pasan como variables de entorno del sistema, nunca como archivos.

---

## Seguridad

- **API keys fuera del repo**: `appsettings.Development.json` y `.env.local` están en `.gitignore`. El repositorio solo contiene valores vacíos.
- **Fallback seguro ante fallo de IA**: si Jev no responde, el mensaje queda en `RejectedReview` en vez de pasar sin clasificar.
- **Idempotencia**: la tabla `processed_events` previene que un mismo evento se procese dos veces aunque RabbitMQ reentregue el mensaje.
- **Outbox pattern**: el mensaje y su evento se guardan en la misma transacción SQL — imposible guardar un mensaje sin que su evento quede registrado para publicar.
- **Dead-Letter Queue**: mensajes que fallan repetidamente van a cuarentena; después de 3 intentos se marcan para revisión manual.
- **HTTPS redirect** desactivado en desarrollo, activo en producción.

---

## Conceptos aprendidos en este proyecto

- Arquitectura hexagonal (Ports & Adapters)
- Outbox Pattern para mensajería confiable
- RabbitMQ: exchanges, queues, routing keys, DLX
- BackgroundService en .NET (workers en segundo plano)
- Idempotencia con tabla `processed_events`
- EF Core Code First con migraciones y snake_case naming convention
- TypeSafe AI / Jev para clasificación de texto con IA
- MailKit para envío de correos SMTP
- Arquitectura hexagonal también en el frontend (Next.js)
