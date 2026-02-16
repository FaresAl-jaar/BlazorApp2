# PDF-Management-System

Ein webbasiertes System zur automatischen Verarbeitung und Verwaltung von PDF-Lieferscheinen (POD - Proof of Delivery) mit Datenextraktion und API-Integration.

![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![Blazor](https://img.shields.io/badge/Blazor-Server-blue)
![Python](https://img.shields.io/badge/Python-3.11-green)
![Docker](https://img.shields.io/badge/Docker-Ready-blue)

## Features

- ?? **PDF-Upload** - Einzeln oder als Batch über REST-API
- ?? **Automatische Datenextraktion** - Python-basierte Textextraktion mit konfigurierbaren Regex-Pattern
- ?? **Web-Editor** - PDF-Vorschau + JSON-Editor zur Datenvalidierung
- ?? **Benutzerverwaltung** - Rollen-basierte Zugriffskontrolle (Admin/User)
- ?? **API-Export** - Validierte Daten an externe Systeme senden
- ?? **Dark Mode** - Augenfreundliche Dunkelansicht
- ?? **Mehrsprachig** - Konfigurierbare Keywords für DE/PL/EN

## Technologie-Stack

| Komponente | Technologie |
|------------|-------------|
| Backend | ASP.NET Core Blazor Server (.NET 10) |
| Datenbank | SQLite / PostgreSQL |
| PDF-Extraktion | Python 3.11 + pdfplumber |
| Frontend | Bootstrap 5 + Bootstrap Icons |
| Container | Docker |

## Schnellstart

### Voraussetzungen

- .NET 10 SDK
- Python 3.11+
- Node.js (optional, für CSS-Build)

### Installation

```bash
# Repository klonen
git clone https://github.com/[username]/BlazorApp2.git
cd BlazorApp2

# Python-Abhängigkeiten installieren
pip install pdfplumber

# .NET-Abhängigkeiten wiederherstellen
dotnet restore

# Anwendung starten
cd BlazorApp2
dotnet run
```

Die Anwendung ist unter `https://localhost:7059` erreichbar.

### Docker

```bash
# Mit Docker Compose starten
docker-compose up -d

# Logs ansehen
docker-compose logs -f
```

Die Anwendung ist unter `http://localhost:8080` erreichbar.

## Konfiguration

### appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=pdfmanager.db"
  },
  "LobsterApi": {
    "BaseUrl": "https://your-api.example.com",
    "SubmitEndpoint": "/api/documents/upload",
    "ApiKey": "your-api-key"
  },
  "PdfPlumber": {
    "Enabled": true,
    "PythonPath": "python",
    "ScriptPath": "Python/processor.py"
  }
}
```

### OCR-Konfiguration

Die Regex-Pattern für die Datenextraktion können über die Web-UI unter "OCR-Konfiguration" angepasst werden.

## API-Endpunkte

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| POST | `/api/documents/upload` | PDF hochladen (multipart/form-data) |
| POST | `/api/documents/upload-batch` | Mehrere PDFs hochladen |
| POST | `/api/documents/receive` | PDF als Base64 empfangen |
| GET | `/api/documents` | Alle Dokumente auflisten |
| GET | `/api/documents/{id}/pdf` | PDF herunterladen |
| GET | `/api/documents/{id}/extracted-data` | Extrahierte JSON-Daten |
| PUT | `/api/documents/{id}/extracted-data` | JSON-Daten speichern |
| POST | `/api/documents/{id}/submit` | An externe API senden |
| DELETE | /api/documents/all | Alle Dokumente löschen |
| DELETE | /api/documents/extracted-data/all | Alle JSON-Daten löschen |

## Projektstruktur

```
BlazorApp2/
??? Components/
?   ??? Layout/          # MainLayout, NavMenu
?   ??? Pages/           # Razor-Seiten (Home, Documents, etc.)
??? Controllers/         # REST-API Controller
??? Services/            # Business-Logik
??? Models/              # Entity-Klassen
??? Data/                # EF Core DbContext
??? Python/              # PDF-Extraktion Skript
??? Config/              # OCR-Konfiguration
??? wwwroot/css/         # Stylesheets
```

## Dokumentation

- [Benutzerhandbuch](BlazorApp2/Dokumentation/Benutzerhandbuch.md)
- [IHK-Projektdokumentation](BlazorApp2/Dokumentation/IHK_Projektdokumentation.md)

## Lizenz

Dieses Projekt ist für interne Nutzung bestimmt.

## Autor

Fares Al-jaar - IHK Abschlussprojekt 2026
