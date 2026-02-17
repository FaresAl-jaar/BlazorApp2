# Karteikarten – C# / Blazor / API (Projektbezogen)

## 1) `async`
**Frage:** Was bedeutet `async` in C#?  
**Antwort:** `async` markiert eine Methode als asynchron, damit sie mit `await` auf laufende Aufgaben warten kann, ohne den Ablauf komplett zu blockieren.

## 2) `Task`
**Frage:** Was ist `Task`?  
**Antwort:** `Task` ist der Rückgabetyp für asynchrone Methoden ohne direkten Rückgabewert.

## 3) `Task<T>`
**Frage:** Was ist der Unterschied zwischen `Task` und `Task<T>`?  
**Antwort:** `Task<T>` liefert zusätzlich ein Ergebnis vom Typ `T` zurück, z. B. `Task<bool>`.

## 4) `await`
**Frage:** Was macht `await`?  
**Antwort:** `await` wartet auf die Fertigstellung eines `Task`, ohne den Thread hart zu blockieren.

## 5) Parameter
**Frage:** Was sind Parameter in Methoden?  
**Antwort:** Parameter sind Eingabewerte einer Methode, z. B. `int id` oder `string userId`.

## 6) Optionale Parameter
**Frage:** Was ist ein optionaler Parameter?  
**Antwort:** Ein optionaler Parameter hat einen Standardwert und muss beim Aufruf nicht zwingend übergeben werden.

## 7) Nullable (`?`)
**Frage:** Was bedeutet `?` hinter einem Typ (z. B. `string?`)?  
**Antwort:** Der Typ darf `null` sein.

## 8) Interface
**Frage:** Was ist ein Interface?  
**Antwort:** Ein Interface definiert einen Vertrag (Methodensignaturen), aber keine konkrete Implementierung.

## 9) Dependency Injection (DI)
**Frage:** Was ist Dependency Injection?  
**Antwort:** Abhängigkeiten werden zentral registriert und automatisch in Klassen bereitgestellt (z. B. über den Konstruktor).

## 10) Controller
**Frage:** Wofür ist ein Controller in ASP.NET Core da?  
**Antwort:** Ein Controller verarbeitet HTTP-Anfragen und gibt API-Antworten zurück.

## 11) Routing-Attribute
**Frage:** Was machen `[Route]`, `[HttpGet]`, `[HttpPost]`?  
**Antwort:** Sie definieren die URL-Pfade und HTTP-Methoden von Endpunkten.

## 12) Model Binding (`[FromBody]`, `[FromForm]`, `[FromQuery]`)
**Frage:** Wofür sind Binding-Attribute da?  
**Antwort:** Sie legen fest, aus welchem Teil der Anfrage Daten gelesen werden (Body, Formular, Querystring).

## 13) Blazor `[Parameter]`
**Frage:** Was macht `[Parameter]` in Blazor?  
**Antwort:** Es markiert eine Eigenschaft, die von außen (z. B. über die Route) an die Komponente übergeben wird.

## 14) `OnInitializedAsync()`
**Frage:** Was ist `OnInitializedAsync()` in Blazor?  
**Antwort:** Das ist eine Lifecycle-Methode, die beim Start der Komponente asynchron ausgeführt wird.

## 15) SignalR Hub
**Frage:** Was ist ein SignalR Hub?  
**Antwort:** Ein Hub ermöglicht Echtzeit-Kommunikation zwischen Server und verbundenen Clients.

## 16) Middleware-Pipeline
**Frage:** Was ist die Middleware-Pipeline?  
**Antwort:** Das ist die Reihenfolge der Request-Verarbeitung in `Program.cs` (z. B. Authentifizierung, Autorisierung, Routing).

## 17) `Task.CompletedTask`
**Frage:** Was ist `Task.CompletedTask`?  
**Antwort:** Ein bereits abgeschlossener Task, wenn keine echte asynchrone Arbeit anfällt.

## 18) DTO
**Frage:** Was ist ein DTO?  
**Antwort:** Ein Data Transfer Object ist ein schlankes Datenobjekt zur Übertragung zwischen Schichten/API.

## 19) Service-Schicht
**Frage:** Wozu dient die Service-Schicht?  
**Antwort:** Sie kapselt Geschäftslogik und hält Controller schlank.

## 20) `IActionResult` / `ActionResult<T>`
**Frage:** Wofür nutzt man `IActionResult` oder `ActionResult<T>`?  
**Antwort:** Damit kann ein Endpunkt flexibel unterschiedliche HTTP-Antworten zurückgeben (z. B. `Ok`, `BadRequest`, `NotFound`).
