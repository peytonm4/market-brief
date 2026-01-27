# Market Brief

A full-stack application that generates daily market briefs with real-time data from Yahoo Finance. Outputs briefs in Markdown, JSON, and PDF formats with optional email notifications.

![.NET](https://img.shields.io/badge/.NET-7.0-512BD4)
![React](https://img.shields.io/badge/React-18-61DAFB)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-16-336791)
![License](https://img.shields.io/badge/License-MIT-green)

## Features

- **Automated Daily Briefs** - Scheduled generation at 6 AM ET (Mon-Fri) via Hangfire
- **Real-Time Market Data** - Fetches live data from Yahoo Finance (indices, sectors, commodities, currencies)
- **Multiple Output Formats** - View as Markdown, JSON, or download as PDF
- **Email Notifications** - Optionally send briefs to a distribution list with PDF attachment
- **Modern React UI** - Clean dashboard with real-time generation status updates

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Backend** | .NET 7 Minimal API |
| **Database** | PostgreSQL 16+ with EF Core 7 |
| **Background Jobs** | Hangfire with PostgreSQL storage |
| **PDF Generation** | QuestPDF |
| **Market Data** | Yahoo Finance API |
| **Frontend** | React 18 + TypeScript + Vite |
| **State Management** | TanStack Query v5 |
| **Styling** | Tailwind CSS |

## Prerequisites

- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0)
- [Node.js 18+](https://nodejs.org/)
- [PostgreSQL 16+](https://www.postgresql.org/download/) (or Docker)
- [Docker](https://www.docker.com/) (optional, for running PostgreSQL)

## Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/peytonm4/market-brief.git
cd market-brief
```

### 2. Start PostgreSQL

**Option A: Using Docker (recommended)**
```bash
docker run -d \
  --name marketbrief-db \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -e POSTGRES_DB=marketbrief \
  -p 5433:5432 \
  postgres:16
```

**Option B: Using existing PostgreSQL**
Create a database named `marketbrief` and update the connection string in `appsettings.json`.

### 3. Initialize the Database

Connect to PostgreSQL and run the following SQL to create the required tables:

```sql
-- Connect to the marketbrief database first

CREATE TABLE IF NOT EXISTS market_briefs (
    id UUID PRIMARY KEY,
    brief_date DATE NOT NULL UNIQUE,
    title VARCHAR(500),
    summary TEXT,
    content_markdown TEXT,
    content_json JSONB,
    status INTEGER NOT NULL DEFAULT 0,
    pdf_storage_path VARCHAR(1000),
    pdf_generated_at TIMESTAMP WITH TIME ZONE,
    generation_started_at TIMESTAMP WITH TIME ZONE,
    generation_completed_at TIMESTAMP WITH TIME ZONE,
    generation_duration_ms INTEGER,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE,
    published_at TIMESTAMP WITH TIME ZONE,
    version INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS brief_sections (
    id UUID PRIMARY KEY,
    brief_id UUID NOT NULL REFERENCES market_briefs(id) ON DELETE CASCADE,
    section_type INTEGER NOT NULL,
    title VARCHAR(200),
    content_markdown TEXT,
    content_json JSONB,
    display_order INTEGER NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS market_data_snapshots (
    id UUID PRIMARY KEY,
    snapshot_date DATE NOT NULL,
    data_type INTEGER NOT NULL,
    symbol VARCHAR(50) NOT NULL,
    name VARCHAR(200),
    open_price DECIMAL(18,4),
    close_price DECIMAL(18,4) NOT NULL,
    high_price DECIMAL(18,4),
    low_price DECIMAL(18,4),
    volume BIGINT,
    change_amount DECIMAL(18,4),
    change_percent DECIMAL(18,4),
    additional_data JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    UNIQUE(snapshot_date, data_type, symbol)
);

CREATE TABLE IF NOT EXISTS generation_logs (
    id UUID PRIMARY KEY,
    brief_id UUID REFERENCES market_briefs(id) ON DELETE SET NULL,
    job_id VARCHAR(100),
    trigger_type INTEGER NOT NULL,
    status INTEGER NOT NULL,
    started_at TIMESTAMP WITH TIME ZONE NOT NULL,
    completed_at TIMESTAMP WITH TIME ZONE,
    error_message TEXT,
    error_stack_trace TEXT,
    metadata JSONB,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_market_briefs_date ON market_briefs(brief_date DESC);
CREATE INDEX IF NOT EXISTS idx_brief_sections_brief_id ON brief_sections(brief_id);
CREATE INDEX IF NOT EXISTS idx_market_data_snapshots_date ON market_data_snapshots(snapshot_date DESC);
CREATE INDEX IF NOT EXISTS idx_generation_logs_brief_id ON generation_logs(brief_id);
```

### 4. Start the Backend

```bash
cd src/MarketBrief.Api
dotnet restore
dotnet run
```

The API will start at `http://localhost:5000`. You can access:
- **Swagger UI**: http://localhost:5000/swagger
- **Hangfire Dashboard**: http://localhost:5000/hangfire

### 5. Start the Frontend

```bash
cd src/MarketBrief.Web
npm install
npm run dev
```

The UI will start at `http://localhost:5173`.

### 6. Generate Your First Brief

1. Open http://localhost:5173
2. Click the **"Generate Brief"** button
3. Wait for generation to complete (fetches live market data)
4. View the brief in Markdown format or download as PDF

## Configuration

### Database Connection

Edit `src/MarketBrief.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=marketbrief;Username=postgres;Password=postgres"
  }
}
```

### Email Notifications (Optional)

To enable email notifications when briefs are generated, configure the Email section in `appsettings.json`:

```json
{
  "Email": {
    "Enabled": true,
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "UseSsl": true,
    "FromAddress": "your-email@gmail.com",
    "FromName": "Market Brief",
    "Recipients": [
      "recipient1@example.com",
      "recipient2@example.com"
    ]
  }
}
```

**For Gmail**: You need to create an [App Password](https://support.google.com/accounts/answer/185833) (requires 2-Step Verification enabled).

See `appsettings.Email.example.json` for examples with other providers (SendGrid, AWS SES, Outlook, Mailgun).

### Scheduled Generation

By default, briefs are automatically generated at **6:00 AM Eastern Time, Monday through Friday**. This is configured in `Program.cs` using Hangfire:

```csharp
RecurringJob.AddOrUpdate<IBriefGenerationService>(
    "daily-market-brief",
    service => service.GenerateBriefAsync(DateOnly.FromDateTime(DateTime.UtcNow), TriggerType.Scheduled, CancellationToken.None),
    "0 6 * * 1-5",  // Cron: 6 AM, Mon-Fri
    new RecurringJobOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York") }
);
```

## API Endpoints

### Briefs
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/briefs` | List briefs (paginated) |
| `GET` | `/api/v1/briefs/latest` | Get most recent brief |
| `GET` | `/api/v1/briefs/{id}` | Get brief by ID |
| `GET` | `/api/v1/briefs/date/{date}` | Get brief by date (YYYY-MM-DD) |
| `POST` | `/api/v1/briefs` | Create brief manually |
| `PUT` | `/api/v1/briefs/{id}` | Update brief |
| `DELETE` | `/api/v1/briefs/{id}` | Delete brief |

### Output Formats
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/v1/briefs/{id}/json` | Get structured JSON |
| `GET` | `/api/v1/briefs/{id}/markdown` | Get Markdown content |
| `GET` | `/api/v1/briefs/{id}/pdf` | Download PDF file |
| `POST` | `/api/v1/briefs/{id}/pdf/regenerate` | Regenerate PDF |

### Generation
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/generation/trigger` | Trigger manual generation |
| `GET` | `/api/v1/generation/status` | Get current generation status |
| `GET` | `/api/v1/generation/history` | Get generation history |

## Project Structure

```
market-brief/
├── src/
│   ├── MarketBrief.Api/           # .NET Minimal API
│   │   ├── Endpoints/             # API endpoint definitions
│   │   ├── Services/              # Business logic (generation, PDF, email)
│   │   └── Models/                # Request/response DTOs
│   │
│   ├── MarketBrief.Core/          # Domain layer
│   │   ├── Entities/              # Database entities
│   │   └── Enums/                 # Status and type enums
│   │
│   ├── MarketBrief.Infrastructure/# Data access layer
│   │   ├── Data/                  # EF Core DbContext and configurations
│   │   └── External/              # Yahoo Finance API client
│   │
│   └── MarketBrief.Web/           # React frontend
│       ├── src/
│       │   ├── components/        # React components
│       │   ├── pages/             # Page components
│       │   ├── hooks/             # TanStack Query hooks
│       │   ├── services/          # API client services
│       │   └── types/             # TypeScript types
│       └── ...
│
├── docker/                        # Docker configuration
│   ├── docker-compose.yml         # Full stack compose
│   ├── Dockerfile.api             # API container
│   └── Dockerfile.web             # Frontend container
│
└── README.md
```

## Market Data

The application fetches real-time data from Yahoo Finance for:

**Indices**
- S&P 500 (SPX)
- Dow Jones Industrial Average (DJI)
- NASDAQ Composite (IXIC)
- Russell 2000 (RUT)

**Sector ETFs**
- Technology (XLK), Financials (XLF), Healthcare (XLV)
- Energy (XLE), Industrials (XLI), Consumer Staples (XLP)
- Consumer Discretionary (XLY), Utilities (XLU)

**Commodities**
- Gold, Silver, Crude Oil WTI, Natural Gas

**Currencies**
- US Dollar Index (DXY)
- EUR/USD, USD/JPY, GBP/USD

## Docker Deployment

Build and run the full stack with Docker Compose:

```bash
cd docker
docker-compose up -d
```

This starts:
- PostgreSQL database on port 5433
- .NET API on port 5000
- React frontend on port 80 (via nginx)

## Troubleshooting

### "Connection refused" errors
- Ensure PostgreSQL is running: `docker ps` or check your local PostgreSQL service
- Verify the port in `appsettings.json` matches your PostgreSQL port

### Brief generation fails
- Check the API logs for detailed error messages
- Ensure you have internet connectivity (Yahoo Finance API requires it)
- View generation history at `/api/v1/generation/history`

### PDF not generating
- PDFs are saved to the `pdfs/` directory (configurable in `appsettings.json`)
- Check that the directory exists and is writable

### Frontend not connecting to API
- Verify the API is running on port 5000
- Check CORS settings in `appsettings.json` include your frontend URL

## License

MIT License - see [LICENSE](LICENSE) for details.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request.
