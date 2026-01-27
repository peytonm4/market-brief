# Market Brief

A .NET 8 Minimal API + PostgreSQL + React application that generates daily market briefs with Markdown and PDF output.

## Tech Stack

- **Backend**: .NET 8 Minimal API, EF Core 8, Hangfire, QuestPDF
- **Frontend**: React 18, Vite, TypeScript, TanStack Query, Tailwind CSS
- **Database**: PostgreSQL 16+

## Prerequisites

- .NET 8 SDK
- Node.js 18+
- PostgreSQL 16+
- Docker (optional)

## Getting Started

### Using Docker

```bash
docker-compose -f docker/docker-compose.yml up -d
```

### Manual Setup

1. **Database**: Create a PostgreSQL database named `marketbrief`

2. **Backend**:
```bash
cd src/MarketBrief.Api
dotnet restore
dotnet ef database update --project ../MarketBrief.Infrastructure
dotnet run
```

3. **Frontend**:
```bash
cd src/MarketBrief.Web
npm install
npm run dev
```

## API Endpoints

### Briefs
- `GET /api/v1/briefs` - List briefs (paginated)
- `GET /api/v1/briefs/latest` - Get most recent brief
- `GET /api/v1/briefs/{id}` - Get brief by ID
- `GET /api/v1/briefs/date/{date}` - Get brief by date
- `POST /api/v1/briefs` - Create brief
- `PUT /api/v1/briefs/{id}` - Update brief
- `DELETE /api/v1/briefs/{id}` - Delete brief

### Formats
- `GET /api/v1/briefs/{id}/json` - Get as structured JSON
- `GET /api/v1/briefs/{id}/markdown` - Get as Markdown
- `GET /api/v1/briefs/{id}/pdf` - Download as PDF
- `POST /api/v1/briefs/{id}/pdf/regenerate` - Regenerate PDF

### Generation
- `POST /api/v1/generation/trigger` - Trigger manual generation
- `GET /api/v1/generation/status` - Get current status
- `GET /api/v1/generation/history` - Get history

## Project Structure

```
├── src/
│   ├── MarketBrief.Api/          # API endpoints and services
│   ├── MarketBrief.Core/         # Entities and enums
│   ├── MarketBrief.Infrastructure/# EF Core and external services
│   └── MarketBrief.Web/          # React frontend
├── docker/                        # Docker configuration
└── README.md
```

## License

MIT
