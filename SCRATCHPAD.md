# Brief Regeneration, Email & Loading Indicator Implementation

## Goal / Scope

**What we ARE doing:**
- Add `force` parameter to regenerate briefs for a specific date (replacing existing ones)
- Show loading indicator in UI during the full generation process (not just API call)
- Ensure email is sent when brief is generated (already implemented, just verify config)

**What we are NOT doing:**
- Changing email implementation (already works)
- Modifying PDF generation logic
- Changing the brief content/format

## Current Status

**COMPLETED:**

1. **Backend: Force Regeneration**
   - [x] `IBriefGenerationService.cs` - Added `force` parameter to interface
   - [x] `BriefGenerationService.cs` - Handles force regeneration:
     - Deletes existing BriefSections
     - Deletes existing NewsStoryClusters
     - Deletes the brief itself
     - Then continues with fresh generation
   - [x] `GenerationEndpoints.cs` - Passes `forceRegenerate` to service (using local variable for Hangfire serialization)
   - [x] `Program.cs` - Updated scheduled job call with `force=false`

2. **Frontend: Loading Indicator**
   - [x] `GenerationTrigger.tsx` - Updated to:
     - Import `useGenerationStatus` hook
     - Combined loading state: `isPending || isRunning`
     - Shows "Starting..." then "Generating..."
     - Disables date input and button during full generation
     - Always sends `force: true` for manual generation

3. **Email Notification**
   - [x] Already implemented at `BriefGenerationService.cs:174-180`
   - Sends automatically when `Email:Enabled=true` and SMTP configured

## Decisions Made

- Manual generation from UI always uses `force: true` (replaces existing briefs)
- Scheduled jobs use `force: false` (won't regenerate if already exists)
- Used local variable `forceRegenerate` instead of `request.Force` in Hangfire expression to avoid serialization issues

## Bug Fix Applied

- Fixed Hangfire expression serialization error by capturing `request.Force` into local variable before lambda

## Next Steps

- [x] User needs to restart API (stop and `dotnet run`) to pick up changes
- [x] Test force regeneration works
- [x] Test loading indicator shows during full generation
- [ ] Verify email is sent (check `Email:Enabled: true` in appsettings.json)

## Verification

- **Fix verified** - Generation working with force regeneration and loading indicator

## Files Modified

| File | Change |
|------|--------|
| `src/MarketBrief.Api/Services/IBriefGenerationService.cs` | Added `force` parameter |
| `src/MarketBrief.Api/Services/BriefGenerationService.cs` | Handle force regeneration, delete existing brief |
| `src/MarketBrief.Api/Endpoints/GenerationEndpoints.cs` | Pass `forceRegenerate` from request to service |
| `src/MarketBrief.Api/Program.cs` | Updated scheduled job with `force=false` |
| `src/MarketBrief.Web/src/components/generation/GenerationTrigger.tsx` | Show loading during full generation |

---

# Future Feature Ideas

## Content & Data

- [ ] **Historical comparison** - Show week-over-week and month-over-month changes in briefs
- [ ] **Custom watchlists** - Let users define their own symbols/sectors to track
- [ ] **Earnings calendar integration** - Highlight upcoming earnings for tracked companies
- [ ] **Economic calendar** - Include Fed meetings, jobs reports, CPI releases
- [ ] **International markets** - Add European/Asian indices and overnight moves
- [ ] **Options data** - VIX term structure, put/call ratios, unusual activity

## News & Analysis

- [ ] **AI-generated summaries** - Use LLM to summarize news clusters instead of just headlines
- [ ] **Sentiment scoring** - Add sentiment analysis to news stories
- [ ] **Source quality weighting** - Prioritize Reuters/Bloomberg over lesser sources
- [ ] **Custom news keywords** - Let users add their own GDELT query buckets

## Delivery & Notifications

- [ ] **Slack/Teams integration** - Post briefs to channels
- [ ] **SMS alerts** - Text when market moves exceed thresholds
- [ ] **Multiple delivery times** - Pre-market (6am), midday, after-close options
- [ ] **Webhook support** - POST brief data to custom endpoints

## User Experience

- [ ] **Brief archive/search** - Search historical briefs by date or content
- [ ] **Mobile-responsive PDF** - Optimize layout for phone viewing
- [ ] **Dark mode PDF** - Alternative color scheme
- [ ] **Interactive web brief** - View brief in browser with expandable sections
- [ ] **Diff view** - Compare two briefs side-by-side

## Infrastructure

- [ ] **Multi-tenant support** - Different configurations per user/team
- [ ] **API rate limiting** - Protect external API quotas
- [ ] **Brief caching** - CDN for PDF delivery
- [ ] **Health checks endpoint** - Monitor GDELT, market data API status
- [ ] **Retry with backoff** - More resilient external API calls

---

# Security Audit (2026-01-28)

## Completed

- [x] Added `appsettings.Development.json` to `.gitignore`
- [x] Removed `appsettings.Development.json` from git tracking
- [x] Verified `appsettings.json` already in `.gitignore`

## Remaining Actions

| Priority | Action | Status |
|----------|--------|--------|
| **CRITICAL** | Revoke Gmail app password (`anki jpwc pxhi oayc`) - it was exposed | Pending |
| **HIGH** | Reset `appsettings.json` to remove real credentials: `git checkout src/MarketBrief.Api/appsettings.json` | Pending |
| **HIGH** | Use `dotnet user-secrets` for local dev credentials | Pending |
| **MEDIUM** | Use environment variables in production | Pending |
| **MEDIUM** | Consider Azure Key Vault or similar for production secrets | Pending |

## Exposed Data Found (in uncommitted appsettings.json)

- Gmail App Password: `anki jpwc pxhi oayc`
- Email addresses: `duckfanaz14@gmail.com`, `peytonmccutcheon4@gmail.com`, `Batessambates@gmail.com`
- DB credentials: `postgres:postgres` (hardcoded default)

## How to Use dotnet user-secrets

```bash
cd src/MarketBrief.Api
dotnet user-secrets init
dotnet user-secrets set "Email:SmtpPassword" "your-new-app-password"
dotnet user-secrets set "Email:SmtpUsername" "your-email@gmail.com"
dotnet user-secrets set "Email:FromAddress" "your-email@gmail.com"
dotnet user-secrets set "Email:Recipients:0" "recipient1@example.com"
dotnet user-secrets set "Email:Recipients:1" "recipient2@example.com"
```

## Files Status

| File | Git Status | Contains Secrets |
|------|------------|------------------|
| `appsettings.json` | Ignored | Yes (local only) |
| `appsettings.Development.json` | Ignored | Yes (local only) |
| `appsettings.Email.example.json` | Tracked | No (placeholders only) |
| `bin/` artifacts | Ignored | N/A |
| `.env` files | Ignored | N/A |

---

## Commands Run

```bash
# Build API (successful after fixes)
dotnet build src/MarketBrief.Api/MarketBrief.Api.csproj --no-restore

# Build Web (successful)
cd src/MarketBrief.Web && npm run build
```
