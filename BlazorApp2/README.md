# PDF Manager & Extractor

Ein Blazor WebAssembly/Server Projekt zur Verwaltung und Extraktion von Daten aus PDF-Dokumenten (speziell Lieferscheine/PODs). Das System empfängt PDFs, extrahiert Daten mittels Python (pdfplumber) und ermöglicht die Validierung sowie den Export an externe APIs (z.B. Lobster).

## Features

- **PDF Upload:** 
  - Drag & Drop Upload im Browser
  - API Endpunkt für automatisierten Upload (`/api/documents/upload`)
  - Base64-JSON Endpunkt für externe Systeme (`/api/documents/receive`)
- **Automatische Extraktion:**
  - Extrahiert strukturierte Daten aus PDFs mithilfe eines integrierten Python-Skripts.
  - Fallback auf Default-Template bei Fehlern.
- **Daten-Validierung:**
  - UI zur Überprüfung und Korrektur der extrahierten JSON-Daten.
  - Side-by-Side Ansicht von PDF und Daten.
- **Export:**
  - Senden der validierten Daten an konfigurierte Endpunkte (Lobster API).
  - Automatischer Export im Hintergrund möglich.
- **Benutzerverwaltung:**
  - Rollenbasiertes System (Admin/User).
  - Dokumenten-Sperre (Claiming) zur Vermeidung gleichzeitiger Bearbeitung.

## Voraussetzungen

- .NET 10.0 SDK (oder aktuellste Version)
- Python 3.x (für das Extraktions-Skript)
  - Benötigte Pakete: `pdfplumber`
- SQLite (wird automatisch erstellt)

## Installation & Start

1. **Repository klonen:**
   ```bash
   git clone https://github.com/FaresAl-jaar/BlazorApp2.git
   cd BlazorApp2
   ```

2. **Python Abhängigkeiten installieren:**
   ```bash
   pip install pdfplumber
   ```

3. **Anwendung starten:**
   ```bash
   cd BlazorApp2
   dotnet run
   ```
   Die Anwendung ist unter `https://localhost:7198` (oder ähnlich) erreichbar.

## API Endpunkte

### PDF Empfang (Incoming)

1. **Multipart Upload (Standard):**
   - URL: `/api/documents/upload`
   - Methode: `POST`
   - Content-Type: `multipart/form-data`
   - Body: `file` (PDF-Datei)

2. **JSON Base64 Upload (Lobster/Extern):**
   - URL: `/api/documents/receive`
   - Methode: `POST`
   - Content-Type: `application/json`
   - Body:
     ```json
     {
       "ExternalId": "ID-123",
       "FileName": "datei.pdf",
       "Base64Content": "JVBERi0xLjQK...", 
       "SourceSystem": "Lobster"
     }
     ```

### Daten Export (Outgoing)

Die Anwendung sendet die extrahierten Daten an den in `appsettings.json` konfigurierten `LobsterApi` Endpunkt.
- Format: `multipart/form-data`
- Feld: `file` (enthält das JSON als Datei, Dateiname = `OriginalName.json`)

## Konfiguration

Die wichtigsten Einstellungen befinden sich in `appsettings.json`:

```json
{
  "LobsterApi": {
    "BaseUrl": "http://pdlobapp99/dw/Request/",
    "SubmitEndpoint": "/api/documents/upload", 
    "ApiKey": ""
  },
  "PdfPlumber": {
    "Enabled": true,
    "PythonPath": "python",
    "ScriptPath": "Python/processor.py"
  }
}
```

## Projektstruktur

- **BlazorApp2/** - Hauptprojekt
  - **Controllers/** - API Controller
  - **Services/** - Geschäftslogik (PDF Verarbeitung, API Calls)
  - **Components/** - Blazor UI Komponenten
  - **Python/** - Extraktions-Skript
  - **Data/** - Datenbank Kontext (SQLite)
