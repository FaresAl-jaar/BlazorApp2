# Benutzerhandbuch - PDF-Management-System

## Inhaltsverzeichnis
1. [Erste Schritte](#1-erste-schritte)
2. [Dashboard](#2-dashboard)
3. [Dokumentenverwaltung](#3-dokumentenverwaltung)
4. [Dokumenteneditor](#4-dokumenteneditor)
5. [OCR-Konfiguration](#5-ocr-konfiguration)
6. [Benutzerverwaltung](#6-benutzerverwaltung)
7. [API-Nutzung](#7-api-nutzung)

---

## 1. Erste Schritte

### Anmeldung
1. Öffnen Sie die Anwendung im Browser
2. Geben Sie Benutzername und Passwort ein
3. Klicken Sie auf "Anmelden"

### Standard-Admin-Account
- **Benutzername:** admin
- **Passwort:** [Vom Administrator festgelegt]

### Dark Mode
Klicken Sie auf das Mond-Symbol (??) in der oberen rechten Ecke, um zwischen Hell- und Dunkelmodus zu wechseln.

---

## 2. Dashboard

Das Dashboard zeigt eine Übersicht über:
- **Gesamtzahl Dokumente** - Alle empfangenen PDFs
- **In Bearbeitung** - Noch nicht abgeschlossene Dokumente
- **Abgeschlossen** - Validierte und übermittelte Dokumente
- **Fehler** - Dokumente mit Verarbeitungsfehlern

### Schnellaktionen
- **Dokumente ansehen** - Zur Dokumentenübersicht
- **Neues Dokument** - Manueller Upload (falls aktiviert)

---

## 3. Dokumentenverwaltung

### Dokumentenliste
Die Tabelle zeigt alle empfangenen Dokumente mit:
- ID und Externe ID
- Dateiname
- Empfangsdatum
- Status (Empfangen, Extrahiert, Geprüft, etc.)
- Aktueller Bearbeiter
- Verfügbarkeit der JSON-Daten

### Filter und Suche
- **Status-Filter:** Dropdown zur Filterung nach Status
- **Suche:** Volltextsuche nach Dateiname oder Externe ID

### Aktionen pro Dokument

| Symbol | Aktion | Beschreibung |
|--------|--------|--------------|
| ? | Übernehmen | Dokument zur Bearbeitung reservieren |
| ? (gefüllt) | Freigeben | Reservierung aufheben |
| ?? | Bearbeiten | Dokumenteneditor öffnen |
| ??? | Ansehen | Nur-Lesen-Modus |
| ?? | Download | JSON herunterladen |
| ??? | Löschen | Dokument entfernen |

### JSON-Export
Über den Button "JSON Export" können Sie:
- **Alle als ZIP** - Alle Dokumente mit JSON-Daten
- **Gefilterte als ZIP** - Nur aktuell angezeigte Dokumente

---

## 4. Dokumenteneditor

### Aufbau
Der Editor ist zweigeteilt:
- **Links:** PDF-Vorschau (eingebettet)
- **Rechts:** JSON-Editor mit den extrahierten Daten

### JSON bearbeiten
1. Klicken Sie in das JSON-Textfeld
2. Bearbeiten Sie die Werte direkt
3. Klicken Sie auf "Speichern" (??)

### Wichtige JSON-Felder

```json
{
  "FileName": "PODReport_M01_012345_67890_Tour123_20260210.pdf",
  "Mandant": "M01",
  "Depot": "Meyer Wildau",
  "Filiale": "012345",
  "Tour": "Tour123",
  "Fahrzeug": "Q12345 (AB-CD 1234)",
  "Anhaenger": "Q67890 (EF-GH 5678)",
  "Fahrer": "Max Mustermann",
  "Adresse": "Musterstraße 1, 12345 Musterstadt",
  "GeplanteLieferung": "2026/02/10, 08:00",
  "StoppInfos": {
    "TatsAnkunft": "2026/02/10, 08:15",
    "Lieferzeit": "00:30",
    "LeistungPuenktlichkeit": "Pünktlich (00:15)"
  },
  "Temperaturen": [
    {"Kammer": "FR", "Wert": "2.00°C"},
    {"Kammer": "TK", "Wert": "-21.00°C"}
  ],
  "WarenGesamt": {
    "AnzArtikel": 50,
    "MengeGeliefert": 100
  }
}
```

### An API senden
Nach der Validierung:
1. Klicken Sie auf "An Lobster senden" (??)
2. Bestätigen Sie die Übermittlung
3. Status wechselt zu "Übermittelt"

---

## 5. OCR-Konfiguration

### Zugriff
Navigation ? OCR-Konfiguration (nur für Administratoren)

### Aufbau der Konfiguration

```json
{
  "Vehicle": {
    "PlatePatterns": ["Regex für Kennzeichen"],
    "Keywords": ["Fahrzeug", "Pojazd"]
  },
  "Driver": {
    "Keywords": ["Fahrer", "Kierowca"],
    "IgnoreList": ["PARK", "FILIALE"]
  },
  "Timestamps": {
    "Labels": {
      "GeplanteLieferung": ["Geplante Lieferung"]
    }
  }
}
```

### Regex-Pattern bearbeiten
1. Navigieren Sie zur gewünschten Sektion
2. Bearbeiten Sie das Pattern (Regex-Syntax)
3. Klicken Sie auf "Speichern"
4. Testen Sie mit einem neuen PDF

### Zurücksetzen
Klicken Sie auf "Auf Standard zurücksetzen" um die Originalkonfiguration wiederherzustellen.

---

## 6. Benutzerverwaltung

### Benutzer anlegen (Admin)
1. Navigation ? Benutzerverwaltung
2. Klicken Sie auf "Neuer Benutzer"
3. Füllen Sie das Formular aus
4. Wählen Sie die Rolle (User/Admin)
5. Speichern

### Rollen

| Rolle | Berechtigungen |
|-------|----------------|
| User | Dokumente ansehen, bearbeiten, senden |
| Admin | Alle Funktionen + Benutzerverwaltung + Löschen |

### Passwort ändern
1. Navigation ? Profil
2. Neues Passwort eingeben
3. Bestätigen und Speichern

---

## 7. API-Nutzung

### Endpunkte

#### PDF hochladen (einzeln)
```
POST /api/documents/upload
Content-Type: multipart/form-data

file: [PDF-Datei]
sourceSystem: "MeinSystem"
```

#### PDF hochladen (mehrere)
```
POST /api/documents/upload-batch
Content-Type: multipart/form-data

files: [PDF-Datei 1]
files: [PDF-Datei 2]
files: [PDF-Datei 3]
sourceSystem: "BatchUpload"
```

#### PDF als Base64
```
POST /api/documents/receive
Content-Type: application/json

{
  "externalId": "12345",
  "fileName": "dokument.pdf",
  "base64Content": "JVBERi0xLjQ...",
  "sourceSystem": "Lobster"
}
```

### Antwort
```json
{
  "success": true,
  "documentId": 42,
  "message": "Dokument erfolgreich empfangen",
  "externalId": "12345"
}
```

### Postman Collection
Eine Postman-Collection für alle API-Endpunkte finden Sie unter:
`/Dokumentation/API_Collection.json`

---

## Häufige Probleme

### PDF wird nicht verarbeitet
- Prüfen Sie, ob Python korrekt installiert ist
- Prüfen Sie die Logs unter "Fehlerprotokolle"
- Stellen Sie sicher, dass die PDF Textebenen enthält

### Umlaute werden nicht erkannt
- Das System verwendet UTF-8
- Prüfen Sie die Quell-PDF auf korrekte Kodierung

### Verbindung unterbrochen
- Blazor Server benötigt eine aktive Verbindung
- Bei Trennung erscheint ein Reconnect-Dialog
- Warten Sie oder laden Sie die Seite neu

---

**Support:** [support@example.com]
**Version:** 1.0
**Stand:** Februar 2026
