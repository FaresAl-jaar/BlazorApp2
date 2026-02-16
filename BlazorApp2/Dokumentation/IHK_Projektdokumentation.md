# IHK Projektdokumentation

## PDF-Management-System mit automatischer Datenextraktion

---

## 1. Projektbeschreibung

### 1.1 Ausgangssituation
Das Unternehmen erhält täglich eine große Anzahl von PDF-Lieferscheinen (POD - Proof of Delivery) von verschiedenen Depots und Filialen. Diese Dokumente enthalten strukturierte Daten wie Fahrzeuginformationen, Fahrerdaten, Lieferzeiten, Warenmengen und Leergut-Informationen. 

Bisher wurden diese Daten manuell aus den PDFs in das ERP-System übertragen, was zeitaufwändig und fehleranfällig war.

### 1.2 Projektziel
Entwicklung eines webbasierten PDF-Management-Systems mit folgenden Kernfunktionen:
- Automatischer Empfang von PDF-Dokumenten über eine REST-API (Multipart & Base64-JSON).
- Automatische Datenextraktion mittels Python-basierter OCR/Text-Extraktion (pdfplumber).
- Manuelle Validierung und Korrektur der extrahierten Daten.
- Export der validierten Daten als JSON-Datei an das externe System (Lobster API).
- Benutzerverwaltung mit Rollen und Berechtigungen.

### 1.3 Projektumfeld
- **Auftraggeber:** [Firmenname]
- **Projektdauer:** [Zeitraum]
- **Projektverantwortlicher:** [Name]
- **Technische Umgebung:** Windows Server, Docker, SQLite

---

## 2. Projektplanung

### 2.1 Zeitplanung

| Phase | Tätigkeit | Geplante Stunden |
|-------|-----------|------------------|
| 1 | Analyse und Anforderungserhebung | 8 |
| 2 | Technische Konzeption | 6 |
| 3 | Datenbankdesign | 4 |
| 4 | Backend-Entwicklung (API, Services) | 15 |
| 5 | Frontend-Entwicklung (Blazor UI) | 12 |
| 6 | PDF-Extraktion (Python) | 10 |
| 7 | Tests und Fehlerbehebung | 8 |
| 8 | Deployment und Dokumentation | 7 |
| **Gesamt** | | **70** |

---

## 3. Analyse

### 3.1 Ist-Analyse
**Aktueller Prozess:**
1. PDF-Lieferscheine werden per E-Mail oder FTP empfangen.
2. Mitarbeiter öffnen jede PDF manuell.
3. Daten werden abgetippt in Excel/ERP.
4. Fehlerquote: ca. 5-10% durch Tippfehler.
5. Zeitaufwand: ca. 3-5 Minuten pro Dokument.

**Probleme:**
- Hoher manueller Aufwand.
- Fehleranfällig.
- Keine Nachverfolgbarkeit.
- Keine zentrale Datenhaltung.

### 3.2 Soll-Analyse
**Zielzustand:**
1. PDFs werden automatisch per API empfangen.
2. Daten werden automatisch extrahiert (< 5 Sekunden).
3. Mitarbeiter validieren nur noch die Ergebnisse.
4. Zeitaufwand: ca. 30 Sekunden pro Dokument.
5. Fehlerquote: < 1%.

### 3.3 Anforderungen

#### Funktionale Anforderungen
| ID | Anforderung | Priorität |
|----|-------------|-----------|
| F01 | PDF-Upload über REST-API (Base64) | Muss |
| F02 | Automatische Textextraktion aus PDF | Muss |
| F03 | Strukturierte JSON-Ausgabe | Muss |
| F04 | Web-basierter Editor zur Datenkorrektur | Muss |
| F05 | Benutzeranmeldung mit Rollen | Muss |
| F06 | Export als JSON-Datei an externe API | Muss |
| F07 | Batch-Upload mehrerer PDFs | Soll |
| F08 | JSON-Export als ZIP | Soll |
| F09 | Dark Mode | Kann |
| F10 | Fehlerprotokollierung | Soll |

---

## 4. Entwurf

### 4.1 Systemarchitektur

```
┌───────────────────────────────────────────────────────────────┐
│                     Blazor Web Application                   │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────────────┐  │
│  │   Razor     │  │  Services   │  │    Controllers      │  │
│  │   Pages     │  │             │  │    (REST API)       │  │
│  └───────────────┘  └───────────────┘  └───────────────────────┘  │
│         │               │                    │               │
│         └──────────────────────────────────────┘               │
│                         │                                    │
│  ┌───────────────────────────────────────────────────────┐    │
│  │              Entity Framework Core                   │    │
│  │                   (SQLite)                           │    │
│  └───────────────────────────────────────────────────────┘    │
└───────────────────────────────────────────────────────────────┘
                          │
                          ▼
┌───────────────────────────────────────────────────────────────┐
│                    Python Processor                          │
│  ┌───────────────┐  ┌───────────────┐  ┌───────────────────────┐  │
│  │  pdfplumber │  │   Regex     │  │   JSON Output       │  │
│  │  (PDF Text) │  │  Extraction │  │                     │  │
│  └───────────────┘  └───────────────┘  └───────────────────────┘  │
└───────────────────────────────────────────────────────────────┘
```

### 4.2 Datenbankmodell

```
┌───────────────────────┐     ┌───────────────────────┐
│    PdfDocument      │     │   ExtractedData     │
├───────────────────────┤     ├───────────────────────┤
│ Id (PK)             │◄────┤ Id (PK)             │
│ ExternalId          │     │ PdfDocumentId (FK)  │
│ FileName            │     │ JsonContent         │
│ FileContent (Blob)  │     │ ExtractedAt         │
│ FileSize            │     │ IsValidated         │
│ FileHash            │     │ Version             │
│ Status              │     │ ModifiedBy          │
│ ReceivedAt          │     │ LastModifiedAt      │
│ ProcessedAt         │     └───────────────────────┘
│ ClaimedByUserId     │
│ ClaimedByUserName   │
│ SourceSystem        │
└───────────────────────┘
```

### 4.3 API-Design

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| POST | /api/documents/upload | Einzelne PDF hochladen (Multipart) |
| POST | /api/documents/receive | PDF als Base64 empfangen (JSON) |
| GET | /api/documents | Alle Dokumente auflisten |
| GET | /api/documents/{id}/pdf | PDF herunterladen |
| POST | /api/documents/{id}/submit | An externe API senden |

### 4.4 Benutzeroberfläche

**Hauptseiten:**
1. **Dashboard (Home)** - Übersicht mit Statistiken.
2. **Dokumentenübersicht** - Tabelle aller PDFs mit Filter/Suche.
3. **Dokumenteneditor** - PDF-Vorschau + JSON-Editor.
4. **Benutzerverwaltung** - Admin-Bereich für Benutzer.

---

## 5. Implementierung

### 5.1 Technologiestack

| Komponente | Technologie | Version |
|------------|-------------|---------|
| Backend | ASP.NET Core Blazor | .NET 10 |
| Frontend | Blazor Server | - |
| Datenbank | SQLite | 3.x |
| PDF-Extraktion | Python + pdfplumber | 3.11 |
| CSS Framework | Bootstrap | 5.3 |
| Icons | Bootstrap Icons | 1.11 |

### 5.2 Kernfunktionen

#### PDF-Empfang (ReceiveDocumentAsync)
Der Empfang erfolgt über `PdfUploadRequest` (JSON mit Base64). Die Datei wird decodiert, gehasht (zur Duplikaterkennung) und in der DB gespeichert. Anschließend wird synchron der Python-Prozess für die Extraktion gestartet.

#### Daten-Export (SubmitToExternalApiAsync)
Die validierten Daten werden als JSON-Datei (`OriginalName.json`) verpackt und als `multipart/form-data` an den konfigurierten Lobster-Endpunkt gesendet.

```csharp
var fileContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
var exportFileName = Path.ChangeExtension(fileName, ".json");
content.Add(fileContent, "file", exportFileName);
```

---

## 6. Qualitätssicherung

### 6.1 Testarten

| Testart | Werkzeug |
|---------|----------|
| Integration Tests | Postman |
| Manueller Test | Browser |

### 6.2 Bekannte Einschränkungen
- PDF-Struktur muss dem erwarteten Format entsprechen.
- Handschriftliche Signaturen werden nicht erkannt.

---

## 7. Deployment

### 7.1 Docker-Deployment

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y python3 python3-pip
RUN pip3 install pdfplumber --break-system-packages

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "BlazorApp2.dll"]
```

### 7.2 Konfiguration (appsettings.json)

```json
{
  "LobsterApi": {
    "BaseUrl": "http://pdlobapp99/dw/Request/",
    "SubmitEndpoint": "/api/documents/upload",
    "ApiKey": ""
  },
  "IncomingApi": {
    "DockerEndpoint": "http://FRDTESTLXSRV:8086/api/documents/receive"
  },
  "PdfPlumber": {
    "Enabled": true,
    "PythonPath": "python3",
    "ScriptPath": "/app/Python/processor.py"
  }
}
```

---

## 8. Fazit

Das Projekt stellt eine robuste Lösung zur Automatisierung der PDF-Datenextraktion dar. Durch die flexible Python-Integration und die moderne Blazor-Oberfläche wird der Arbeitsaufwand signifikant reduziert.

**Erstellt am:** 16.02.2026
**Version:** 1.0
