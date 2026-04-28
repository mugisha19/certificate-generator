# AUCA Certify — Certificate of Good Standing

A modern ASP.NET Core MVC web application that produces the **Adventist University of
Central Africa (AUCA) Certificate of Good Standing** for students. Records are stored in
the `CertificateOfAttendance` table; the same data drives both an on-screen preview and a
print-ready A4 PDF whose layout mirrors the official AUCA document.

---

## Highlights

- **Same-day preview, A4-true PDF.** The on-screen certificate matches the generated PDF
  byte-for-byte in layout. The PDF is `PageSizes.A4` (210 × 297 mm) with proper 20 mm
  margins; the preview enforces an A4 aspect ratio (`width: min(100%, 794px); aspect-ratio: 210/297`).
- **Dual database provider.** A single `DatabaseProvider` setting in `appsettings.json`
  picks **PostgreSQL** *or* **SQL Server**. Two named connection strings are kept side by
  side so switching is a one-line config change.
- **Real datepickers everywhere a date is captured.** `Date of birth`, `Studied from`,
  and `Studied to` use native `<input type="date">` controls. A "Currently studying"
  toggle disables the end-date and prints the literal word **"Date"** on the certificate
  for ongoing students.
- **Year / semester dropdown** with three options (`One (Semester 1)`, `Two (Semester 2)`,
  `Three (Semester 3)`) — no more free-form text drift.
- **Modern UI.** Charcoal sidebar + light content area; emerald-on-charcoal palette; Plus
  Jakarta Sans (UI) + JetBrains Mono (IDs); outlined "notched-label" inputs; card-grid
  dashboard; PDF-viewer-framed details preview. Hooks for browser print produce a clean
  A4 page.
- **DOCX-faithful certificate.** Header letterhead → contact strip (Mobile Phone + Email)
  → "Kigali, …" date → centered, underlined `CERTIFICATE OF GOOD STANDING` → director
  attestation → student name → period → year/faculty/major/academic-year/validity lines
  → closing paragraph → signature (left) and verification QR (right).

---

## Tech stack

| Layer    | Choice                                                |
|----------|-------------------------------------------------------|
| Runtime  | .NET 8 (ASP.NET Core MVC, Razor runtime compilation)  |
| ORM      | Entity Framework Core 8                               |
| Database | PostgreSQL 14+ **or** SQL Server 2019+ (configurable) |
| PDF      | QuestPDF (Community licence)                          |
| Type     | Plus Jakarta Sans (UI), JetBrains Mono (IDs)          |

---

## Repository layout

```text
Controllers/            Home (CRUD + PDF) controller
Data/                   AppDbContext (EF Core)
Models/                 CertificateOfAttendance domain model + display helpers
Services/               CertificatePdfService (QuestPDF document)
Migrations/             EF Core migrations (PostgreSQL provider)
Views/
  Home/                 Index, Create, Edit, Details
  Shared/               _Layout (sidebar shell), _CertificateForm, _ValidationScriptsPartial
wwwroot/
  css/site.css          Theme + components + print rules
  js/site.js            Sidebar collapse, alert auto-dismiss, "currently studying" toggle
  images/certificate/   letterhead.jpg, qr.png, signature.png
appsettings.json        DatabaseProvider + both connection strings
Program.cs              Provider-aware DbContext registration & startup migration
```

---

## Data model

```csharp
public class CertificateOfAttendance
{
    public string?   StudentName  { get; set; }   // required
    public DateTime? BornDate     { get; set; }   // required, date column
    public string?   StudentID    { get; set; }   // required, primary key
    public DateTime? StudiedFrom  { get; set; }   // required, date column → "MMMM yyyy"
    public DateTime? StudiedTo    { get; set; }   // null → prints as "Date"
    public string?   Year         { get; set; }   // dropdown: One / Two / Three (Semester N)
    public string?   Faculty      { get; set; }   // required
    public string?   Major        { get; set; }   // required
    public string?   AcademicYear { get; set; }   // free-form, parens supplies Validity
    public string?   ApprovedBy   { get; set; }   // signing director
    public string?   Comment      { get; set; }   // optional italic note
    public string?   Status       { get; set; }   // Active / Completed / Suspended
}
```

`StudiedFrom` / `StudiedTo` are real `date` columns; a migration converts any legacy
free-form "January 2026" text into a date when run against an existing PostgreSQL
database. The certificate prints them as `MMMM yyyy` (e.g. **January 2026**).

`AcademicYear` text inside parentheses is treated as the certificate **Validity**
(`2025-2026 (September 2025-August 2026)` → Validity `September 2025-August 2026`).

---

## Configuration

> **First-time setup:** `appsettings.json` is **gitignored** so your real
> connection strings never reach the repo. After cloning, copy the template:
>
> ```bash
> cp appsettings.Example.json appsettings.json
> ```
>
> Then edit `appsettings.json` with your real passwords / hosts.

`appsettings.Example.json` carries both database choices side by side:

```json
{
  "DatabaseProvider": "Postgres",
  "ConnectionStrings": {
    "Postgres":  "Host=localhost;Port=5432;Database=CertificateDB;Username=postgres;Password=YOUR_POSTGRES_PASSWORD",
    "SqlServer": "Server=localhost;Database=CertificateDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

- **`"DatabaseProvider": "Postgres"`** — uses Npgsql, applies the EF migrations in
  `Migrations/` on startup.
- **`"DatabaseProvider": "SqlServer"`** — uses `Microsoft.EntityFrameworkCore.SqlServer`
  and runs `Database.EnsureCreated()` (the bundled migrations are PostgreSQL-specific).

Override at runtime without editing the file:

```bash
DatabaseProvider=SqlServer dotnet run
# or
dotnet run -- --DatabaseProvider=SqlServer
```

> **Note on secrets:** the example file ships a development password. For anything
> beyond local development, move the connection strings to **User Secrets**
> (`dotnet user-secrets`) or environment variables.

---

## Running locally

### 1. Prerequisites

- .NET SDK 8.0
- PostgreSQL 14+ **or** SQL Server 2019+ (whichever provider you target)

### 2. Install dependencies

```bash
dotnet restore
```

### 3. Apply the schema

- **Postgres:** `dotnet run` — startup calls `Database.Migrate()` automatically.
- **SQL Server:** flip `DatabaseProvider` to `SqlServer`, set the connection string,
  then `dotnet run` — the schema is materialised via `EnsureCreated()`.

### 4. Open the app

```text
http://localhost:5158
```

(Port from `Properties/launchSettings.json` — adjust as needed.)

---

## Feature reference

### Dashboard (`/`)

- KPI strip: total records, active students, currently shown.
- Search by name or student ID; status filter pills (All / Active / Completed / Suspended).
- Card grid — one card per student with avatar initials, ID, faculty, major, year,
  status, and per-row icon actions (view / edit / download PDF / delete).

### New certificate / Edit (`/Home/Create`, `/Home/Edit/{id}`)

- Four labelled sections — Student identity, Study period, Academic record,
  Authorization — using outlined notched-label inputs.
- Native datepickers; "Currently studying" toggle clears `StudiedTo`.
- Year is a three-option dropdown.
- Inline example placeholders and helper text on every field.

### Details (`/Home/Details/{id}`)

- Sticky action bar (Back / Edit / Preview PDF / Download PDF).
- PDF-viewer-framed live preview (dark window with traffic-light dots, "Open PDF" link).
- Horizontal data tiles below: Student / Academic / Period / Authorization (+ optional
  Comment).

### PDF generation (`/Home/GeneratePdf/{id}`, `/Home/PreviewPdf/{id}`)

- A4 portrait, 20 mm margins, Arial 11.5 pt body.
- Letterhead → contact strip (Mobile Phone + Email) → date → underlined title → director
  attestation → student name → period → labelled lines → closing paragraph →
  signature (left) + QR (right).

---

## Browser print

A `@media print` block hides the chrome (sidebar, topbar, action buttons) so
`Ctrl + P` from the **Details** page prints the certificate frame at A4 portrait
with 20 mm margins, matching the downloaded PDF.

---

## Possible next steps

- Move connection strings into User Secrets / Key Vault.
- Add authentication + role-based authorisation (only the Director / DAAR staff issue).
- Generate a per-student verification QR code (currently a static asset).
- Audit log of who issued / printed each certificate.
- Generate native SQL Server migrations alongside the existing Postgres ones.

---

## Licence

Internal AUCA / DAAR project. QuestPDF is used under its Community licence.
