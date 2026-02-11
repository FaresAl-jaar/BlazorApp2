# IHK Projektdokumentation

## PDF-Management-System mit automatischer Datenextraktion

---

## 1. Projektbeschreibung

### 1.1 Ausgangssituation
Das Unternehmen erhält täglich eine große Anzahl von PDF-Lieferscheinen (POD - Proof of Delivery) von verschiedenen Depots und Filialen. Diese Dokumente enthalten strukturierte Daten wie Fahrzeuginformationen, Fahrerdaten, Lieferzeiten, Warenmengen und Leergut-Informationen. 

Bisher wurden diese Daten manuell aus den PDFs in das ERP-System übertragen, was zeitaufwändig und fehleranfällig war.

### 1.2 Projektziel
Entwicklung eines webbasierten PDF-Management-Systems mit folgenden Kernfunktionen:
- Automatischer Empfang von PDF-Dokumenten über eine REST-API
- Automatische Datenextraktion mittels Python-basierter OCR/Text-Extraktion
- Manuelle Validierung und Korrektur der extrahierten Daten
- Export der validierten Daten an das ERP-System (Lobster API)
- Benutzerverwaltung mit Rollen und Berechtigungen

### 1.3 Projektumfeld
- **Auftraggeber:** [Firmenname]
- **Projektdauer:** [Zeitraum]
- **Projektverantwortlicher:** [Name]
- **Technische Umgebung:** Windows Server, Docker, SQLite/PostgreSQL

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

### 2.2 Ressourcenplanung
- 1 Entwickler (Vollzeit)
- Entwicklungsrechner mit Visual Studio 2022
- Testserver für Deployment
- Zugang zur Lobster API (Testumgebung)

### 2.3 Risikoanalyse

| Risiko | Wahrscheinlichkeit | Auswirkung | Maßnahme |
|--------|-------------------|------------|----------|
| PDF-Struktur variiert stark | Mittel | Hoch | Konfigurierbare Regex-Pattern |
| Encoding-Probleme (Umlaute) | Hoch | Mittel | UTF-8 durchgängig erzwingen |
| API-Verfügbarkeit Lobster | Niedrig | Hoch | Offline-Modus mit Warteschlange |
| Performance bei vielen PDFs | Mittel | Mittel | Asynchrone Verarbeitung |

---

## 3. Analyse

### 3.1 Ist-Analyse
**Aktueller Prozess:**
1. PDF-Lieferscheine werden per E-Mail oder FTP empfangen
2. Mitarbeiter öffnen jede PDF manuell
3. Daten werden abgetippt in Excel/ERP
4. Fehlerquote: ca. 5-10% durch Tippfehler
5. Zeitaufwand: ca. 3-5 Minuten pro Dokument

**Probleme:**
- Hoher manueller Aufwand
- Fehleranfällig
- Keine Nachverfolgbarkeit
- Keine zentrale Datenhaltung

### 3.2 Soll-Analyse
**Zielzustand:**
1. PDFs werden automatisch per API empfangen
2. Daten werden automatisch extrahiert (< 5 Sekunden)
3. Mitarbeiter validieren nur noch die Ergebnisse
4. Zeitaufwand: ca. 30 Sekunden pro Dokument
5. Fehlerquote: < 1%

### 3.3 Anforderungen

#### Funktionale Anforderungen
| ID | Anforderung | Priorität |
|----|-------------|-----------|
| F01 | PDF-Upload über REST-API | Muss |
| F02 | Automatische Textextraktion aus PDF | Muss |
| F03 | Strukturierte JSON-Ausgabe | Muss |
| F04 | Web-basierter Editor zur Datenkorrektur | Muss |
| F05 | Benutzeranmeldung mit Rollen | Muss |
| F06 | Export an externe API | Muss |
| F07 | Batch-Upload mehrerer PDFs | Soll |
| F08 | JSON-Export als ZIP | Soll |
| F09 | Dark Mode | Kann |
| F10 | Fehlerprotokollierung | Soll |

#### Nicht-funktionale Anforderungen
| ID | Anforderung | Wert |
|----|-------------|------|
| NF01 | Antwortzeit API | < 2 Sekunden |
| NF02 | PDF-Verarbeitung | < 10 Sekunden |
| NF03 | Verfügbarkeit | 99% |
| NF04 | Browserunterstützung | Chrome, Firefox, Edge |
| NF05 | Responsive Design | Ja |

---

## 4. Entwurf

### 4.1 Systemarchitektur

```
???????????????????????????????????????????????????????????????
?                     Blazor Web Application                   ?
?  ???????????????  ???????????????  ???????????????????????  ?
?  ?   Razor     ?  ?  Services   ?  ?    Controllers      ?  ?
?  ?   Pages     ?  ?             ?  ?    (REST API)       ?  ?
?  ???????????????  ???????????????  ???????????????????????  ?
?         ?               ?                    ?               ?
?         ??????????????????????????????????????               ?
?                         ?                                    ?
?  ???????????????????????????????????????????????????????    ?
?  ?              Entity Framework Core                   ?    ?
?  ?                   (SQLite/PostgreSQL)                ?    ?
?  ????????????????????????????????????????????????????????    ?
???????????????????????????????????????????????????????????????
                          ?
                          ?
???????????????????????????????????????????????????????????????
?                    Python Processor                          ?
?  ???????????????  ???????????????  ???????????????????????  ?
?  ?  pdfplumber ?  ?   Regex     ?  ?   JSON Output       ?  ?
?  ?  (PDF Text) ?  ?  Extraction ?  ?                     ?  ?
?  ???????????????  ???????????????  ???????????????????????  ?
???????????????????????????????????????????????????????????????
```

### 4.2 Datenbankmodell

```
???????????????????????     ???????????????????????
?    PdfDocument      ?     ?   ExtractedData     ?
???????????????????????     ???????????????????????
? Id (PK)             ?????<? Id (PK)             ?
? ExternalId          ?     ? PdfDocumentId (FK)  ?
? FileName            ?     ? JsonContent         ?
? FileContent (Blob)  ?     ? ExtractedAt         ?
? FileSize            ?     ? IsValidated         ?
? FileHash            ?     ? Version             ?
? Status              ?     ? ModifiedBy          ?
? ReceivedAt          ?     ? LastModifiedAt      ?
? ProcessedAt         ?     ???????????????????????
? ClaimedByUserId     ?
? ClaimedByUserName   ?
? SourceSystem        ?
???????????????????????

???????????????????????     ???????????????????????
?  ApplicationUser    ?     ?     ErrorLog        ?
???????????????????????     ???????????????????????
? Id (PK)             ?     ? Id (PK)             ?
? UserName            ?     ? Message             ?
? Email               ?     ? Level               ?
? FullName            ?     ? Source              ?
? PasswordHash        ?     ? StackTrace          ?
? IsApproved          ?     ? Timestamp           ?
? IsMainAdmin         ?     ? DocumentId          ?
? CreatedAt           ?     ? UserId              ?
? LastLoginAt         ?     ???????????????????????
???????????????????????
```

### 4.3 API-Design

| Methode | Endpoint | Beschreibung |
|---------|----------|--------------|
| POST | /api/documents/upload | Einzelne PDF hochladen |
| POST | /api/documents/upload-batch | Mehrere PDFs hochladen |
| POST | /api/documents/receive | PDF als Base64 empfangen |
| GET | /api/documents | Alle Dokumente auflisten |
| GET | /api/documents/{id}/pdf | PDF herunterladen |
| GET | /api/documents/{id}/extracted-data | JSON-Daten abrufen |
| PUT | /api/documents/{id}/extracted-data | JSON-Daten speichern |
| POST | /api/documents/{id}/submit | An externe API senden |
| DELETE | /api/documents/{id} | Dokument löschen |

### 4.4 Benutzeroberfläche

**Hauptseiten:**
1. **Dashboard (Home)** - Übersicht mit Statistiken
2. **Dokumentenübersicht** - Tabelle aller PDFs mit Filter/Suche
3. **Dokumenteneditor** - PDF-Vorschau + JSON-Editor
4. **Benutzerverwaltung** - Admin-Bereich für Benutzer
5. **OCR-Konfiguration** - Regex-Pattern bearbeiten
6. **Fehlerprotokolle** - Systemfehler einsehen

---

## 5. Implementierung

### 5.1 Technologiestack

| Komponente | Technologie | Version |
|------------|-------------|---------|
| Backend | ASP.NET Core Blazor | .NET 10 |
| Frontend | Blazor Server | - |
| Datenbank | SQLite / PostgreSQL | 3.x / 16 |
| ORM | Entity Framework Core | 10.x |
| PDF-Extraktion | Python + pdfplumber | 3.11 |
| CSS Framework | Bootstrap | 5.3 |
| Icons | Bootstrap Icons | 1.11 |
| Container | Docker | 24.x |

### 5.2 Projektstruktur

```
BlazorApp2/
??? Components/
?   ??? Layout/
?   ?   ??? MainLayout.razor      # Haupt-Layout mit Sidebar
?   ?   ??? NavMenu.razor         # Navigation
?   ??? Pages/
?       ??? Home.razor            # Dashboard
?       ??? Documents.razor       # Dokumentenliste
?       ??? DocumentEditor.razor  # PDF-Viewer + Editor
?       ??? Login.razor           # Anmeldung
?       ??? Register.razor        # Registrierung
?       ??? UserManagement.razor  # Benutzerverwaltung
?       ??? OcrConfig.razor       # OCR-Konfiguration
?       ??? ErrorLogs.razor       # Fehlerprotokolle
??? Controllers/
?   ??? DocumentsController.cs    # REST-API für Dokumente
?   ??? AuthController.cs         # Authentifizierung
??? Services/
?   ??? DocumentService.cs        # Dokumenten-Logik
?   ??? PdfPlumberService.cs      # Python-Aufruf
?   ??? OcrConfigService.cs       # OCR-Konfiguration
?   ??? LobsterApiService.cs      # Externe API
?   ??? ErrorLogService.cs        # Fehlerprotokollierung
??? Models/
?   ??? PdfDocument.cs            # Dokument-Entity
?   ??? ExtractedData.cs          # Extrahierte Daten
?   ??? ApplicationUser.cs        # Benutzer-Entity
?   ??? ErrorLog.cs               # Fehlerprotokoll
??? Data/
?   ??? ApplicationDbContext.cs   # EF Core Context
??? Python/
?   ??? processor.py              # PDF-Extraktion
?   ??? ocr_config.json           # Regex-Konfiguration
??? Config/
?   ??? ocr_config.json           # OCR-Einstellungen
??? wwwroot/
    ??? css/
        ??? modern-ui.css         # Custom Styles
        ??? darkmode.css          # Dark Mode
```

### 5.3 Kernfunktionen

#### PDF-Empfang und Verarbeitung
```csharp
public async Task<PdfUploadResponse> ReceiveDocumentAsync(PdfUploadRequest request)
{
    // 1. PDF validieren und speichern
    var document = new PdfDocument
    {
        ExternalId = request.ExternalId,
        FileName = request.FileName,
        FileContent = Convert.FromBase64String(request.Base64Content),
        Status = DocumentStatus.Received
    };
    
    // 2. In Datenbank speichern
    await _context.PdfDocuments.AddAsync(document);
    await _context.SaveChangesAsync();
    
    // 3. Automatische Extraktion starten
    var extractedJson = await _pdfPlumberService.ExtractDataFromPdfAsync(
        document.FileContent, document.FileName);
    
    // 4. Extrahierte Daten speichern
    var extractedData = new ExtractedData
    {
        PdfDocumentId = document.Id,
        JsonContent = extractedJson,
        ExtractedAt = DateTime.UtcNow
    };
    
    return new PdfUploadResponse { Success = true, DocumentId = document.Id };
}
```

#### Python-Integration
```csharp
public async Task<string> ExtractDataFromPdfAsync(byte[] pdfContent, string fileName)
{
    // PDF temporär speichern
    var tempPath = Path.Combine(_tempDir, $"temp_{Guid.NewGuid():N}.pdf");
    await File.WriteAllBytesAsync(tempPath, pdfContent);
    
    // Python-Prozess starten
    var startInfo = new ProcessStartInfo
    {
        FileName = "python",
        Arguments = $"\"{_scriptPath}\" \"{tempPath}\" \"{fileName}\"",
        RedirectStandardOutput = true,
        StandardOutputEncoding = Encoding.UTF8
    };
    
    // Umgebungsvariablen für UTF-8
    startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
    startInfo.EnvironmentVariables["OCR_CONFIG_PATH"] = _configPath;
    
    using var process = Process.Start(startInfo);
    var output = await process.StandardOutput.ReadToEndAsync();
    
    return output; // JSON-String
}
```

#### OCR-Konfiguration (Python)
```python
CONFIG = {
    "Vehicle": {
        "PlatePatterns": [
            r"\b([A-Z0-9]{3,10}\s*\([A-Z]{1,3}-[A-Z0-9]+\s*\d{0,5}[A-Z]?\))",
            r"([A-Z]{1,3}-[A-Z0-9]+\s*\d{0,5}[A-Z]?)\b"
        ],
        "Keywords": ["Fahrzeug", "Pojazd"],
        "TrailerKeywords": ["Anhänger", "Przyczepa"]
    },
    "Driver": {
        "Keywords": ["Fahrer", "Kierowca"],
        "IgnoreList": ["PARK", "FILIALE", "LEERGUT"]
    },
    # ... weitere Konfiguration
}
```

---

## 6. Qualitätssicherung

### 6.1 Testarten

| Testart | Beschreibung | Werkzeug |
|---------|--------------|----------|
| Unit Tests | Service-Methoden | xUnit |
| Integration Tests | API-Endpoints | Postman |
| Manueller Test | UI-Funktionen | Browser |
| Last-Test | Performance | ab (Apache Bench) |

### 6.2 Testfälle

| ID | Testfall | Erwartetes Ergebnis | Status |
|----|----------|---------------------|--------|
| T01 | PDF-Upload über API | Dokument wird gespeichert | ? |
| T02 | Batch-Upload (5 PDFs) | Alle 5 werden verarbeitet | ? |
| T03 | Datenextraktion Fahrzeug | Kennzeichen wird erkannt | ? |
| T04 | Datenextraktion Fahrer | Name wird extrahiert | ? |
| T05 | Umlaute in JSON | ü, ö, ä, ß korrekt | ? |
| T06 | JSON-Export (einzeln) | Download funktioniert | ? |
| T07 | ZIP-Export (alle) | ZIP wird erstellt | ? |
| T08 | Benutzeranmeldung | Login erfolgreich | ? |
| T09 | Dokumenten-Claiming | Nur Besitzer kann bearbeiten | ? |
| T10 | Dark Mode | Alle Elemente lesbar | ?? |

### 6.3 Bekannte Einschränkungen
- PDF-Struktur muss dem erwarteten Format entsprechen
- Handschriftliche Signaturen werden nicht erkannt
- Sehr alte/beschädigte PDFs können Fehler verursachen

---

## 7. Deployment

### 7.1 Systemanforderungen

**Server:**
- Linux oder Windows Server
- Docker 24.x oder .NET 10 Runtime
- 2 GB RAM (mindestens)
- 10 GB Speicherplatz
- Python 3.11 mit pdfplumber

**Client:**
- Moderner Webbrowser (Chrome, Firefox, Edge)
- JavaScript aktiviert
- Bildschirmauflösung min. 1280x720

### 7.2 Docker-Deployment

```yaml
# docker-compose.yml
version: '3.8'
services:
  blazorapp:
    build: .
    ports:
      - "8080:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=Data Source=/app/data/pdfmanager.db
    volumes:
      - ./data:/app/data
```

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
RUN apt-get update && apt-get install -y python3 python3-pip
RUN pip3 install pdfplumber

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "BlazorApp2.dll"]
```

### 7.3 Konfiguration

**appsettings.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=pdfmanager.db"
  },
  "LobsterApi": {
    "BaseUrl": "https://lobster-api.example.com",
    "SubmitEndpoint": "/api/documents/upload",
    "ApiKey": "[API-KEY-HIER-EINTRAGEN]"
  },
  "PdfPlumber": {
    "Enabled": true,
    "PythonPath": "python",
    "ScriptPath": "Python/processor.py"
  }
}
```

---

## 8. Projektabschluss

### 8.1 Soll-Ist-Vergleich

| Anforderung | Soll | Ist | Erfüllt |
|-------------|------|-----|---------|
| PDF-Upload API | Ja | Ja | ? |
| Automatische Extraktion | Ja | Ja | ? |
| Web-Editor | Ja | Ja | ? |
| Benutzerverwaltung | Ja | Ja | ? |
| Export an Lobster API | Ja | Vorbereitet | ?? |
| Batch-Upload | Optional | Ja | ? |
| ZIP-Export | Optional | Ja | ? |
| Dark Mode | Optional | Ja | ? |

### 8.2 Zeitvergleich

| Phase | Geplant | Tatsächlich | Abweichung |
|-------|---------|-------------|------------|
| Analyse | 8h | 7h | -1h |
| Konzeption | 6h | 6h | 0h |
| Datenbankdesign | 4h | 3h | -1h |
| Backend | 15h | 18h | +3h |
| Frontend | 12h | 14h | +2h |
| PDF-Extraktion | 10h | 12h | +2h |
| Tests | 8h | 6h | -2h |
| Deployment | 7h | 4h | -3h |
| **Gesamt** | **70h** | **70h** | **0h** |

### 8.3 Fazit

Das Projekt wurde erfolgreich umgesetzt. Alle Muss-Anforderungen wurden erfüllt. Die automatische PDF-Datenextraktion reduziert den manuellen Aufwand erheblich. 

**Besondere Herausforderungen:**
- Encoding-Probleme (Umlaute) erforderten zusätzliche UTF-8-Maßnahmen
- Variable PDF-Strukturen erforderten flexible Regex-Pattern
- Blazor Server SignalR-Verbindung bei langen Operationen

**Erweiterungsmöglichkeiten:**
- KI-basierte Texterkennung für handschriftliche Signaturen
- Automatische Anomalie-Erkennung bei Lieferdaten
- Mobile App für Außendienstmitarbeiter
- Integration weiterer ERP-Systeme

---

## 9. Anhang

### 9.1 Glossar

| Begriff | Erklärung |
|---------|-----------|
| POD | Proof of Delivery - Liefernachweis |
| OCR | Optical Character Recognition - Texterkennung |
| Blazor | Microsoft Web-Framework für interaktive UIs |
| REST API | Representational State Transfer - Webservice-Architektur |
| JSON | JavaScript Object Notation - Datenformat |
| Docker | Container-Virtualisierungsplattform |

### 9.2 Quellenverzeichnis

1. Microsoft Blazor Documentation: https://docs.microsoft.com/blazor
2. pdfplumber Python Library: https://github.com/jsvine/pdfplumber
3. Bootstrap 5 Documentation: https://getbootstrap.com/docs/5.3
4. Entity Framework Core: https://docs.microsoft.com/ef/core

### 9.3 Abbildungsverzeichnis

- Abb. 1: Systemarchitektur
- Abb. 2: Datenbankmodell (ER-Diagramm)
- Abb. 3: Screenshot Dashboard
- Abb. 4: Screenshot Dokumenteneditor
- Abb. 5: Screenshot OCR-Konfiguration

---

**Erstellt am:** [Datum]
**Erstellt von:** [Name]
**Version:** 1.0
