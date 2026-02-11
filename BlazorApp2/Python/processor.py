# -*- coding: utf-8 -*-

import sys
import io
import json
import os
import re
import csv
from datetime import datetime
import pdfplumber

# Setze stdout auf UTF-8 für korrekte Ausgabe von Umlauten
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8', errors='replace')

# --- KONFIGURATION LADEN ---
# Priorisierung: 1. Umgebungsvariable 2. Neben dem Skript 3. Im Config-Ordner des App-Verzeichnisses
def get_config_path():
    # 1. Umgebungsvariable
    env_path = os.environ.get('OCR_CONFIG_PATH')
    if env_path and os.path.exists(env_path):
        return env_path
    
    # 2. Neben dem Skript
    script_dir = os.path.dirname(os.path.abspath(__file__))
    local_config = os.path.join(script_dir, 'ocr_config.json')
    if os.path.exists(local_config):
        return local_config
    
    # 3. Im Config-Ordner (eine Ebene höher)
    parent_config = os.path.join(os.path.dirname(script_dir), 'Config', 'ocr_config.json')
    if os.path.exists(parent_config):
        return parent_config
    
    # Fallback: neben dem Skript (wird dann Fehler werfen wenn nicht vorhanden)
    return local_config

CONFIG_FILE = get_config_path()
CONFIG = {}

def load_config():
    global CONFIG
    if os.path.exists(CONFIG_FILE):
        with open(CONFIG_FILE, 'r', encoding='utf-8') as f:
            CONFIG = json.load(f)
    else:
        # Fallback oder Fehler, falls Config fehlt
        print(json.dumps({"Error": f"Config file not found at {CONFIG_FILE}"}))
        sys.exit(1)

# Output-Ordner - relativ zum Skript-Verzeichnis oder Temp
def get_output_dirs():
    script_dir = os.path.dirname(os.path.abspath(__file__))
    base_output = os.path.join(script_dir, 'output')
    return {
        'json': os.path.join(base_output, 'JSON'),
        'txt': os.path.join(base_output, 'txt_output'),
        'csv': os.path.join(base_output, 'csv_output')
    }

OUTPUT_DIRS = get_output_dirs()
JSON_OUTPUT_DIR = OUTPUT_DIRS['json']
TXT_OUTPUT_DIR = OUTPUT_DIRS['txt']
CSV_OUTPUT_DIR = OUTPUT_DIRS['csv']

def fix_encoding(s: str) -> str:
    """Behebt haeufige Encoding-Probleme in extrahiertem PDF-Text"""
    if not s: 
        return ""
    
    # Grad-Zeichen normalisieren
    s = s.replace("º", "°").replace("\u00ba", "°")
    
    # Replacement-Character entfernen
    if "\ufffd" in s:
        s = s.replace("\ufffd", "")
    
    # Haeufige falsche Kodierungen (CP1252 -> UTF-8 Fehler)
    replacements = {
        # Latin-1/CP1252 falsch als UTF-8 interpretiert
        "Ã¼": "ü", "Ã¶": "ö", "Ã¤": "ä",
        "Ãœ": "Ü", "Ã–": "Ö", "Ã„": "Ä",
        "ÃŸ": "ß",
        # Escape-Sequenzen
        "\x00fc": "ü", "\x00e4": "ä", "\x00f6": "ö",
        "\x00dc": "Ü", "\x00c4": "Ä", "\x00d6": "Ö",
        "\x00df": "ß",
    }
    
    for old, new in replacements.items():
        s = s.replace(old, new)
    
    return s

def clean_spaces(text: str) -> str:
    text = fix_encoding(text or "")
    return re.sub(r"\s{2,}", " ", text).strip()

def clean_int(val) -> int:
    if val is None: return 0
    s = fix_encoding(str(val)).strip()
    if not s: return 0
    s = s.replace(" ", "").replace(".", "")
    s = re.sub(r"[^0-9,-]", "", s).replace(",", ".")
    try: return int(float(s))
    except: return 0

def normalize_dt(s: str) -> str:
    s = clean_spaces(s)
    if not s: return ""
    formats = CONFIG.get("General", {}).get("DateFormatList", ["%d.%m.%y, %H:%M"])
    for fmt in formats:
        try: return datetime.strptime(s, fmt).strftime("%Y/%m/%d, %H:%M")
        except: pass
    return s

def dt_from_norm(s: str):
    s = (s or "").strip()
    if not s or s == "--": return None
    try: return datetime.strptime(s, "%Y/%m/%d, %H:%M")
    except: return None

def hhmm_delta(a: str, b: str) -> str:
    da, db = dt_from_norm(a), dt_from_norm(b)
    if not da or not db: return ""
    mins = int((db - da).total_seconds() // 60)
    if mins < 0: mins += 24 * 60
    if mins > 12 * 60: return "00:00"
    return f"{mins//60:02d}:{mins%60:02d}"

def extract_page_text(page) -> str:
    t = page.extract_text(layout=True, x_tolerance=3) or ""
    t = fix_encoding(t)
    if t.strip(): return t
    words = page.extract_words(use_text_flow=True) or []
    return "\n".join(clean_spaces(w.get("text", "")) for w in words if w.get("text"))

# --- TIME EXTRACTION (Updated to use List of Labels) ---
def get_time_after_label(full_text: str, label_patterns: list) -> str:
    dt_pat = CONFIG["General"]["DateTimePattern"]
    for label in label_patterns:
        # Wir bauen den Regex dynamisch: Label + beliebiger Text + DatumPattern
        pat = rf"(?is){label}.*?({dt_pat})"
        m = re.search(pat, full_text)
        if m:
            return normalize_dt(m.group(1))
    return ""

def parse_temperature_blocks(text: str):
    out = []
    lines = text.split('\n')
    deg = "\u00b0"
    
    # Regex aus Config
    regex_str = CONFIG["Temperature"]["RegexPattern"]
    pat = re.compile(regex_str, re.IGNORECASE)
    
    range_pat = r"([0-9.,-]+\s*-\s*[0-9.,-]+\s*" + deg + r"?\s*C)"
    
    for i, line in enumerate(lines):
        t_line = fix_encoding(line)
        m = pat.search(t_line)
        if m:
            chamber, temp = m.group(1).upper(), m.group(2).replace(",", ".")
            rng = ""
            range_match = re.search(range_pat, t_line[m.end():])
            if range_match: rng = clean_spaces(range_match.group(1))
            if not rng and i > 0:
                rm = re.search(range_pat, lines[i-1])
                if rm: rng = clean_spaces(rm.group(1))
            if not rng and i + 1 < len(lines):
                rm = re.search(range_pat, lines[i+1])
                if rm: rng = clean_spaces(rm.group(1))
            out.append({"Kammer": chamber, "Wert": temp + deg + "C", "Range": rng})
    return out

def extract_tabular_duration(text: str, label: str) -> str:
    lines = text.split('\n')
    dt_pat = CONFIG["General"]["DateTimePattern"]
    for i, line in enumerate(lines):
        if re.search(rf'\b{label}\b', line, re.IGNORECASE):
            for k in range(1, 4):
                if i + k >= len(lines): break
                nxt = lines[i + k]
                if not clean_spaces(nxt): continue
                spec = re.search(rf"{dt_pat}\s+(\d{2}:\d{2})", nxt)
                if spec: return spec.group(1)
                clean = re.sub(rf"{dt_pat}", '', nxt)
                durs = re.findall(r"\b(\d{2}:\d{2})\b", clean)
                if durs: return durs[0]
                if "--" in clean: return "--"
                break
    return ""

def save_raw_text(stem: str, full_text: str):
    os.makedirs(TXT_OUTPUT_DIR, exist_ok=True)
    with open(os.path.join(TXT_OUTPUT_DIR, f"{stem}.txt"), "w", encoding="utf-8") as f: f.write(full_text)

def save_csv(stem: str, data: dict):
    os.makedirs(CSV_OUTPUT_DIR, exist_ok=True)
    stopp, wg, ls, ab = data.get("StoppInfos", {}), data.get("WarenGesamt", {}), data.get("LeergutSummeSeite1", {}), data.get("Abschluss", {})
    temps = "; ".join([f"{t.get('Kammer', '')}: {t.get('Wert', '')}" for t in data.get("Temperaturen", [])])
    row = {
        "FileName": data.get("FileName", ""), "ProcessedAt": data.get("ProcessedAt", ""), "Mandant": data.get("Mandant", ""),
        "Depot": data.get("Depot", ""), "Filiale": data.get("Filiale", ""), "Tour": data.get("Tour", ""),
        "Fahrzeug": data.get("Fahrzeug", ""), "Anhaenger": data.get("Anhaenger", ""), "Fahrer": data.get("Fahrer", ""),
        "Adresse": data.get("Adresse", ""), "GeplanteLieferung": data.get("GeplanteLieferung", ""),
        "GeplantAnkunft": stopp.get("GeplantAnkunft", ""), "TatsAnkunft": stopp.get("TatsAnkunft", ""),
        "BeginnLieferung": stopp.get("BeginnLieferung", ""), "EndeLieferung": stopp.get("EndeLieferung", ""),
        "Abfahrt": stopp.get("Abfahrt", ""), "Lieferzeit": stopp.get("Lieferzeit", ""), "Standzeit": stopp.get("Standzeit", ""),
        "LeistungPuenktlichkeit": stopp.get("LeistungPuenktlichkeit", ""), "Temperaturen": temps,
        "WarenGesamt_AnzArtikel": wg.get("AnzArtikel", ""), "WarenGesamt_MengeBestellt": wg.get("MengeBestellt", ""),
        "WarenGesamt_MengeGeliefert": wg.get("MengeGeliefert", ""), "WarenGesamt_MengeErhalten": wg.get("MengeErhalten", ""),
        "WarenGesamt_Differenz": wg.get("Differenz", ""), "WarenGesamt_GesamtGewicht": wg.get("GesamtGewicht", ""),
        "WarenGesamt_GesPreis": wg.get("GesPreis", ""), "Leergut_Anlieferung": ls.get("Anlieferung", ""),
        "Leergut_Zurueck": ls.get("Zurueck", ""), "Leergut_Differenz": ls.get("Differenz", ""),
        "AnnahmeStatus": ab.get("AnnahmeStatus", ""), "Kommentar": ab.get("Kommentar", ""),
        "FahrerSignatur": ab.get("FahrerSignatur", ""), "Zeitstempel": ab.get("Zeitstempel", ""),
    }
    with open(os.path.join(CSV_OUTPUT_DIR, f"{stem}.csv"), "w", encoding="utf-8-sig", newline="") as f:
        writer = csv.DictWriter(f, fieldnames=row.keys(), delimiter=";")
        writer.writeheader()
        writer.writerow(row)

# --- ROBUSTE FAHRZEUG-EXTRAKTION (Konfigurierbar) ---
def extract_vehicle_full(text):
    patterns = CONFIG["Vehicle"]["PlatePatterns"]
    matches = []
    for pattern in patterns:
        for m in re.finditer(pattern, text):
            # Gruppenlogik: Pattern 1 hat 1 Gruppe, Pattern 2 hat 2 mögliche Gruppen in der Regex OR Verknüpfung
            # Wir nehmen einfach die erste nicht-leere Gruppe
            val = next((g for g in m.groups() if g), None)
            if val:
                if re.search(r"\d{4}-\d{2}", val): continue # Datum filtern
                matches.append(clean_spaces(val))
    return matches


def process_pdf(path: str, original_filename: str = None):
    load_config() # Config laden
    
    # Wenn ein Original-Dateiname uebergeben wurde, diesen verwenden
    if original_filename:
        filename = original_filename
    else:
        filename = os.path.basename(path)
    
    stem = os.path.splitext(filename)[0]
    parts = stem.split("_")
    os.makedirs(JSON_OUTPUT_DIR, exist_ok=True)
    json_path = os.path.join(JSON_OUTPUT_DIR, f"{stem}.json")
    
    data = {
        "FileName": filename, "ProcessedAt": datetime.now().astimezone().isoformat(), "Mandant": parts[1] if len(parts) > 1 else "",
        "Depot": "", "Filiale": parts[2] if len(parts) > 2 else "", "Tour": parts[4] if len(parts) > 4 else "",
        "Fahrzeug": "", "Anhaenger": "", "Fahrer": "", "Adresse": "", "GeplanteLieferung": "",
        "StoppInfos": {}, "Temperaturen": [], "Waren": [], "WarenGesamt": {}, "LeergutSummeSeite1": {},
        "LeergutDetails": [], "LeergutZusammenfassung": {}, "Abschluss": {}
    }
    
    try:
        with pdfplumber.open(path) as pdf:
            pages_text = [extract_page_text(p) for p in pdf.pages]
            full_text = "\n\n".join([t for t in pages_text if t])
            save_raw_text(stem, full_text)
            if not full_text.strip():
                data["Error"] = "No extractable text"
                _save_and_output(data, json_path); save_csv(stem, data); return
            
            text1 = pages_text[0] if pages_text else ""
            lines1 = [clean_spaces(l) for l in text1.split("\n") if clean_spaces(l)]
            if lines1: data["Depot"] = lines1[0]
            
            # --- FAHRZEUG & ANHÄNGER ---
            all_plates = extract_vehicle_full(text1)
            if all_plates:
                data["Fahrzeug"] = all_plates[0]
                # Zweites Kennzeichen als Anhaenger, auch wenn gleich (manche LKW haben gleiche Nummern)
                if len(all_plates) > 1:
                    data["Anhaenger"] = all_plates[1]
            
            # Fallback über Keywords für Fahrzeug
            if not data["Fahrzeug"]:
                for kw in CONFIG["Vehicle"]["Keywords"]:
                    m = re.search(rf"{kw}\s*:?[\s\n]+(.+)", text1)
                    if m: 
                        data["Fahrzeug"] = clean_spaces(m.group(1).split('\n')[0])
                        break
            
            # Fallback über Keywords für Anhaenger - IMMER suchen wenn leer
            if not data["Anhaenger"]:
                for kw in CONFIG["Vehicle"]["TrailerKeywords"]:
                    m = re.search(rf"{kw}\s*:?[\s\n]+(.+)", text1)
                    if m:
                        trailer_val = clean_spaces(m.group(1).split('\n')[0])
                        # Nur setzen wenn nicht leer und nicht nur Striche/Leerzeichen
                        if trailer_val and trailer_val not in ['--', '-', '']:
                            data["Anhaenger"] = trailer_val
                        break
            
            # --- FAHRER ---
            ignore_drivers = [x.upper() for x in CONFIG["Driver"]["IgnoreList"]]
            driver_keywords = CONFIG["Driver"]["Keywords"]
            driver_name_pattern = CONFIG["Driver"]["NamePattern"]
            
            for i, line in enumerate(lines1):
                # Check ob Zeile ein Fahrer-Keyword enthält
                if any(re.search(rf"(?i)\b{kw}\b", line) for kw in driver_keywords):
                    for j in range(i + 1, min(i + 6, len(lines1))):
                        cand = lines1[j]
                        if any(bad in cand.upper() for bad in ignore_drivers): continue
                        if re.search(r"\d{4,5}\s*$", cand): continue
                        name_match = re.search(driver_name_pattern, cand)
                        if name_match:
                            found_name = clean_spaces(name_match.group(1))
                            if found_name != data.get("Fahrzeug"): 
                                data["Fahrer"] = found_name; break
                    break
            
            # --- ADRESSE ---
            addr_keywords = CONFIG["Address"]["Keywords"]
            zip_pattern = CONFIG["General"]["ZipCodePattern"]
            
            for i, line in enumerate(lines1):
                if any(re.search(rf"(?i)\b{kw}\b", line) for kw in addr_keywords):
                    for j in range(i + 1, min(i + 7, len(lines1))):
                        cand = lines1[j]
                        if re.search(zip_pattern, cand) and len(cand) > 10:
                            if "Telefon" not in cand and "Helpdesk" not in cand: 
                                data["Adresse"] = clean_spaces(cand); break
                    break
            
            if not data["Adresse"] or "…" in data["Adresse"]:
                main_note_pat = CONFIG["Address"]["MainNotePattern"]
                haupt_match = re.search(main_note_pat, text1, re.IGNORECASE)
                if haupt_match: data["Adresse"] = clean_spaces(haupt_match.group(1))

            # --- SANITY CHECK (Logik bleibt im Python Code) ---
            anh = data.get("Anhaenger", "")
            fahr = data.get("Fahrer", "")
            adr = data.get("Adresse", "")
            
            if anh and anh == adr: data["Anhaenger"] = ""
            elif "," in anh or re.search(r"\b(Str\.|Strasse|Straße|Weg|Platz|Gasse)\b", anh, re.IGNORECASE):
                data["Anhaenger"] = ""
            
            if fahr and fahr == adr: data["Fahrer"] = ""
            elif re.search(r"\b(Str\.|Strasse|Straße|Weg|Platz|Gasse)\b", fahr, re.IGNORECASE):
                data["Fahrer"] = ""
            # -----------------------------------------------------

            # Timestamps
            time_labels = CONFIG["Timestamps"]["Labels"]
            data["GeplanteLieferung"] = get_time_after_label(text1, time_labels["GeplanteLieferung"])
            
            stopp = {
                "GeplantAnkunft": get_time_after_label(text1, time_labels["GeplantAnkunft"]),
                "TatsAnkunft": get_time_after_label(text1, time_labels["TatsAnkunft"]),
                "BeginnLieferung": get_time_after_label(text1, time_labels["BeginnLieferung"]),
                "EndeLieferung": get_time_after_label(text1, time_labels["EndeLieferung"]),
                "Abfahrt": get_time_after_label(text1, time_labels["Abfahrt"]),
                "Lieferzeit": extract_tabular_duration(text1, "Lieferzeit"),
                "Standzeit": extract_tabular_duration(text1, "Standzeit"),
                "LeistungPuenktlichkeit": ""
            }
            if not stopp["Lieferzeit"]: stopp["Lieferzeit"] = hhmm_delta(stopp["BeginnLieferung"], stopp["EndeLieferung"])
            if not stopp["Standzeit"] or stopp["Standzeit"] == "":
                if stopp["Abfahrt"]: stopp["Standzeit"] = hhmm_delta(stopp["EndeLieferung"], stopp["Abfahrt"])
                else: stopp["Standzeit"] = "--"
            
            # Pünktlichkeit Patterns
            puenkt_patterns = CONFIG["Timestamps"]["PunctualityPatterns"]
            for pat in puenkt_patterns:
                m = re.search(pat, text1, re.IGNORECASE)
                if m:
                    status, zeit = m.group(1).strip(), m.group(2) if m.lastindex and m.lastindex >= 2 else ""
                    stopp["LeistungPuenktlichkeit"] = f"{status} ({zeit})" if zeit else status
                    break
            data["StoppInfos"] = stopp
            
            data["Temperaturen"] = parse_temperature_blocks(text1)
            
            # --- WAREN TABELLE ---
            waren_pattern = CONFIG["Goods"]["TablePattern"]
            for line in lines1:
                m = re.search(waren_pattern, line)
                if m:
                    data["Waren"].append({
                        "Lieferschein": m.group(1), "AnzArtikel": int(m.group(2)),
                        "MengeBestellt": int(m.group(3)), "MengeGeliefert": int(m.group(4)),
                        "MengeErhalten": m.group(5).replace(",", "."),
                        "Differenz": float(m.group(6).replace(",", ".")),
                        "GesamtGewicht": m.group(7).replace(",", "."), "GesPreis": m.group(8).replace(",", ".")
                    })
            
            ges_pattern = CONFIG["Goods"]["TotalPattern"]
            ges_match = re.search(ges_pattern, text1)
            if ges_match:
                data["WarenGesamt"] = {
                    "AnzArtikel": int(ges_match.group(1)), "MengeBestellt": int(ges_match.group(2)),
                    "MengeGeliefert": int(ges_match.group(3)), "MengeErhalten": ges_match.group(4).replace(",", "."),
                    "Differenz": ges_match.group(5).replace(",", "."), "GesamtGewicht": ges_match.group(6).replace(".", "").replace(",", "."),
                    "GesPreis": ges_match.group(7).replace(",", ".").replace("€", "").strip()
                }
            
            # --- ABSCHLUSS ---
            # Annahmebereitschaft etc. ist sehr spezifisch, Keywords ggf. anpassen
            annahme_block = re.search(r"(?is)Annahmebereitschaft([\s\S]*?)(?:Geliefert\s+an|Kommentar|Haftungsausschluss)", text1)
            annahme_text = " ".join([l.strip() for l in annahme_block.group(1).split('\n') if l.strip() and "Annahmebereitschaft" not in l]) if annahme_block else ""
            
            komm = re.search(r"(?is)Haftungsausschluss\s+(.*?)(?:\n\s*\n|[a-z]{3,}\s*\d{2}\.\d{2}\.\d{2})", text1)
            dt_pat_full = CONFIG["General"]["DateTimePattern"]
            all_times = re.findall(rf"({dt_pat_full})", text1)
            
            data["Abschluss"] = {
                "AnnahmeStatus": annahme_text,
                "Kommentar": clean_spaces(komm.group(1)) if komm else "",
                "FahrerSignatur": "",
                "Zeitstempel": normalize_dt(all_times[-1][0]) if all_times else "" # group 0 weil nested regex
            }
            
            # --- LEERGUT SEITE 2 ---
            if len(pages_text) > 1:
                text2 = pages_text[1]
                lines2 = [clean_spaces(l) for l in text2.split("\n") if clean_spaces(l)]
                
                coll_pat = CONFIG["Empties"]["CollectionPattern"]
                col_indices = {"Anlieferung": 1, "Abholung": 3, "Differenz": 4} if re.search(coll_pat, text2, re.IGNORECASE) else {"Anlieferung": 1, "Abholung": 2, "Differenz": 3}
                
                for line in lines2:
                    m_gen = re.match(r"^\s*(\d{4})\s+(.*?)\s+(--|-?\d+)\s+(.*)", line)
                    if m_gen:
                        art_nr, name, saldo, rest = m_gen.group(1), clean_spaces(m_gen.group(2).replace("--", "")), m_gen.group(3).strip(), m_gen.group(4)
                        nums = re.findall(r"(-?\d+)", rest)
                        anl = nums[1] if len(nums) >= 2 else 0
                        abh = nums[3] if len(nums) == 5 else (nums[2] if len(nums) == 4 else 0)
                        diff = nums[4] if len(nums) == 5 else (nums[3] if len(nums) == 4 else 0)
                        
                        data["LeergutDetails"].append({
                            "ArtikelNr": art_nr, "Bezeichnung": name, "Saldo": saldo, "Geplant": clean_int(nums[0]),
                            "Anlieferung": clean_int(anl), "Abholung": clean_int(abh), "Differenz": clean_int(diff)
                        })
                
                zus_pat = CONFIG["Empties"]["SummaryPattern"]
                zus = re.search(zus_pat, text2)
                best_kw = CONFIG["Conclusion"]["SignatureKeywords"]
                best_pat = rf"(?is)Der\s+({'|'.join(best_kw)}).*?Unterschrift"
                best = re.search(best_pat, text1)
                
                if zus:
                    z_nums = re.findall(r"(-?\d+)", zus.group(1))
                    if len(z_nums) >= 4:
                        geplant, anl = clean_int(z_nums[0]), clean_int(z_nums[1])
                        abh_idx, diff_idx = (3, 4) if len(z_nums) == 5 else (2, 3)
                        abh, diff = clean_int(z_nums[abh_idx]), clean_int(z_nums[diff_idx])
                        data["LeergutZusammenfassung"] = {"Geplant": geplant, "Anlieferung": anl, "Abholung": abh, "Differenz": diff}
                        data["LeergutSummeSeite1"] = {"Anlieferung": anl, "Zurueck": abh, "Differenz": diff, "Bestaetigung": clean_spaces(best.group(0)) if best else ""}
                
                # Signatur Erkennung
                sig_keywords = CONFIG["Conclusion"]["SignatureKeywords"]
                sig_matches = []
                for skw in sig_keywords:
                    sig_matches.extend(list(re.finditer(rf"(?is){skw}", text1)))
                
                if sig_matches:
                    # Sortieren nach Position um den letzten zu finden
                    sig_matches.sort(key=lambda x: x.end())
                    search_chunk = text1[sig_matches[-1].end():sig_matches[-1].end()+5000]
                    ignore_sig = CONFIG["Conclusion"]["IgnoreSignatureContent"]
                    
                    for s_line in search_chunk.split('\n'):
                        slc = clean_spaces(s_line)
                        if not slc or any(ig in slc.lower() for ig in ignore_sig): continue
                        if re.search(r"\d{2}\.\d{2}\.\d{2}", slc):
                            found = re.sub(r"\d{2}\.\d{2}\.\d{2}.*", "", slc).strip()
                            if len(found) > 2: data["Abschluss"]["FahrerSignatur"] = found; break
                        elif re.match(r"^[A-Za-zÄÖÜäöüß\s]+$", slc) and len(slc) > 2: data["Abschluss"]["FahrerSignatur"] = slc; break
            
            if not data["LeergutSummeSeite1"]: data["LeergutSummeSeite1"] = {"Anlieferung": 0, "Zurueck": 0, "Differenz": 0, "Bestaetigung": clean_spaces(best.group(0)) if best else ""}
    except Exception as e: data["Error"] = str(e)
    _save_and_output(data, json_path); save_csv(stem, data)

def _save_and_output(data: dict, json_path: str):
    with open(json_path, "w", encoding="utf-8") as f: json.dump(data, f, ensure_ascii=False, indent=2)
    print(json.dumps(data, ensure_ascii=False, indent=2))

if __name__ == "__main__":
    if len(sys.argv) > 2:
        # Erstes Argument: Pfad zur temp-PDF, Zweites: Original-Dateiname
        process_pdf(sys.argv[1], sys.argv[2])
    elif len(sys.argv) > 1:
        process_pdf(sys.argv[1])
    else:
        print(json.dumps({"Error": "Kein Pfad angegeben"}, ensure_ascii=False))