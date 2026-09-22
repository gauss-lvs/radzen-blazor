# GAUSS-Abweichungen gegenüber Upstream

Alle Änderungen, die den GAUSS-Fork vom offiziellen Radzen.Blazor unterscheiden — also das, was
bei jedem Upstream-Merge erhalten bleiben muss.

| | |
| --- | --- |
| Branch | `gauss-next` |
| Stand | 22.09.2026 |
| Upstream-Basis | `5dc34d34b` ("Version updated", Radzen 11.4.2) |
| Paketversion | `11.4.2.1-next.0` |
| Umfang | 26 Dateien, +1582 / −52 Zeilen |

## Wie dieser Delta ermittelt wurde

Ein naheliegendes `git diff master..gauss-next` ist **irreführend**: `master` ist im Fork nur ein
Spiegel des Radzen-Upstream und hinkt hinterher. Der weitaus größte Teil dieser Differenz ist
reiner Radzen-Fortschritt und keine GAUSS-Abweichung.

Die Historie ist aber günstig gebaut: die GAUSS-Commits sitzen als **geschlossener Block** auf
einer reinen Upstream-Linie. Den Schnittpunkt — den jüngsten Commit, der nicht von GAUSS stammt —
findet man so:

```bash
BASE=$(git log gauss-next --format="%H %an" | grep -vm1 " pb$" | cut -d' ' -f1)

# Gegenprobe, muss "pb" und nur "pb" ausgeben:
git log $BASE..gauss-next --format="%an" | sort -u

# Der GAUSS-Delta:
git diff $BASE..gauss-next
```

Zum Stand dieses Dokuments ist `$BASE` = `5dc34d34b`. Nach jedem Upstream-Merge verschiebt sich
der Wert; das Kommando oben ermittelt ihn neu.

Im Code sind die Abweichungen zusätzlich durch `#region GAUSS-spezifische Änderungen` markiert
(`grep -rn "GAUSS-spezifisch" Radzen.Blazor/`), das deckt aber nur einen Teil ab — siehe
[Merge-Risiken](#merge-risiken).

## Übersicht

| Bereich | Dateien | Kurzbeschreibung |
| --- | --- | --- |
| [A. Paket & Build](#a-paket--build) | 1 | Eigene PackageId, gepinnte Abhängigkeiten, reparierte CSS/JS-Generierung |
| [B. Veröffentlichung](#b-veröffentlichung) | 4 | Release über Forgejo statt GitHub Actions |
| [C. Entwicklungsumgebung](#c-entwicklungsumgebung) | 2 | GAUSS-Devcontainer |
| [D. GIcon / GBusyIcon](#d-gicon--gbusyicon) | 12 | Icon-Integration aus `GAUSS.RadzenBase` |
| [E. RadzenTree](#e-radzentree) | 2 | `BeforeChange`-Event, `Reload(value)` |
| [F. DropDown: LoadDataOnOpen](#f-dropdown-loaddataonopen) | 1 | Verzögertes Laden erst beim Öffnen |
| [G. DropDown: Wertauflösung](#g-dropdown-wertauflösung) | 1 | In-Memory-Quellen über `Equals` statt Query-Expression |
| [H. DropDown: PopupClasses](#h-dropdown-popupclasses) | 1 | Zusätzliche CSS-Klassen am Popup |
| [I. Tests](#i-tests) | 2 | Regressionstests zu F und G |

## Testlage

`dotnet test` auf `gauss-next` (net10.0): **5369 Tests, 4909 grün, 460 rot.**

Alle 460 Fehler liegen in `Radzen.Documents.Markdown.Tests.*` und sind **nicht GAUSS-verursacht**:
die CommonMark-Tests erwarten CRLF und scheitern unter Linux. Am reinen Upstream-Stand schlagen
dieselben Tests fehl — verifiziert.

Außerhalb der Markdown-Tests ist der Baum grün, einschließlich der 17 GAUSS-eigenen Tests
(Abschnitt I).

---

## A. Paket & Build

`Radzen.Blazor/Radzen.Blazor.csproj`

### Paketidentität

```xml
<PackageId>Radzen.Blazor.GAUSS</PackageId>
<Version>11.4.2.1-next.0</Version>
<Authors>Radzen Ltd. and GAUSS-LVS mbH</Authors>
```

`Product` und `Copyright` bleiben bei Radzen Ltd. Die `-next.N`-Prerelease-Kennung ist spezifisch
für `gauss-next`; der Branch `gauss` verwendet stabile Versionen.

### Zusätzliche Abhängigkeit

```xml
<PackageReference Include="GAUSS.RadzenBase" Version="1.0.0.2" />
```

Liefert `GRadzenBase.Icons.IRadzenFontIcon` mit den Membern `CodePoint` und `IconSetCssClass()` —
Basis für alle `GIcon`-Properties (Abschnitt D). Diese Abhängigkeit ist der Grund, warum das Paket
nicht mehr gegen das offizielle `Radzen.Blazor` austauschbar ist.

### Gepinnte Framework-Versionen

Upstream verwendet `8.0.0` sowie die Wildcards `9.*-*` und `10.*-*`. Der Fork pinnt auf `8.0.26`,
`9.0.15` und `10.0.7`, damit ein Build nicht von neu erschienenen Preview-Paketen abhängt und
reproduzierbar bleibt.

### Reparierte Generierung von CSS und minifiziertem JS

Die umfangreichste Build-Änderung. Upstream erzeugt CSS (`SassCompile`) und
`Radzen.Blazor.min.js` (`TerserMinify`) nur unter `'$(TargetFramework)' == 'net10.0'`.

**Problem:** Beim Packen eines cross-targeting Projekts packt das SDK die Static Web Assets nur
*eines* Inner-Builds — `ResolveStaticWebAssetsEffectiveTargetFramework` wählt das niedrigste TFM,
also `net8.0`. Das erzeugte Paket kam dadurch **ohne jedes CSS** heraus.

**Lösung:** Die Generierung läuft einmal im Outer-Build, vor den Inner-Builds:

- `MoveCss` und `MinifyJs` hängen an `BeforeTargets="DispatchToInnerBuilds"`.
- `CompileSass` nutzt einen gemeinsamen Stamp unter `$(BaseIntermediateOutputPath)` statt
  `$(IntermediateOutputPath)`, damit die Inner-Builds ihn überspringen.
- Neues Target `AddGeneratedStaticWebAssets` fügt die erzeugten Dateien pro Inner-Build in
  `Content` ein. Es hat bewusst **keine** `Inputs`/`Outputs`: ein Up-to-date-Check würde auch die
  `ItemGroup` überspringen, und die Dateien fehlten dann im Static-Web-Asset-Manifest.
- Der Task-Import aus `Radzen.MSBuild` wird explizit nachgezogen, weil NuGet `build/*.targets` nur
  in die Inner-Builds importiert. Dafür ist die Version in
  `<RadzenMSBuildVersion>0.0.6</RadzenMSBuildVersion>` ausgelagert.

Laut Commit-Nachricht von `cfdf5f9a` per `dotnet pack -c Release` verifiziert: das Paket enthält
30 CSS-Dateien, das minifizierte JS und `lib`-Ordner für net8.0, net9.0 und net10.0.

## B. Veröffentlichung

### `publish.ps1` (neu)

Hebt die Version an, committet, taggt `v<Version>` und pusht mit `--follow-tags` nach
`git.gauss-lvs.de`. Ohne `--follow-tags` bliebe der annotierte Tag lokal und es würde nichts
gebaut. Auf `gauss-next` wird `dotnet version ... prerelease` verwendet.

### `.forgejo/workflows/release.yml` (neu)

Ausgelöst durch einen Tag-Push `v*`; baut, packt und veröffentlicht nach `nuget.gauss-lvs.de`.

- `runs-on: virtual-runner`, Image `docker.sopart.de/gauss/runner-image:gauss-latest`.
- Prüft vorab, dass `secrets.NUGET_API_KEY` gesetzt ist.
- Prüft bei Tag-Builds, dass der Tag zur `<Version>` in der csproj passt, und bricht sonst ab.
- Pusht Paket **und** Symbolpaket mit `--skip-duplicate`.
- `workflow_dispatch` erlaubt einen manuellen Nachlauf ohne Versionsabgleich.
- **Der Testschritt ist auskommentiert** — siehe [Befund 3](#3-release-ohne-testlauf).

### `.github/workflows/ci.yml` und `premium-themes.yml` (deaktiviert)

Bei beiden sind die `push`/`pull_request`-Trigger entfernt, nur `workflow_dispatch` bleibt. Die
Dateien werden bewusst nicht gelöscht, damit Upstream-Merges sie nicht immer wieder
zurückbringen. Bei `premium-themes.yml` würde der Trigger sonst bei jeder Theme-Änderung einen
Build in `radzenhq/radzen-next` anstoßen.

## C. Entwicklungsumgebung

`.devcontainer/devcontainer.json` und `devcontainer-lock.json` (neu). Basis
`docker.sopart.de/gauss/devcontainers-gauss:latest`, mit `claude-code`-Feature, persistenten
Volumes für Claude-Code- und OpenCode-Konfiguration, `dotnet restore Radzen.sln` als
`postCreateCommand` und einem Git-Setup-Skript als `postStartCommand`. Playwright ist nicht
enthalten, weil es bereits im Image liegt.

## D. GIcon / GBusyIcon

Die durchgängigste inhaltliche Abweichung: alle Komponenten mit `Icon`-Parameter erhalten
zusätzlich `GIcon` vom Typ `GRadzenBase.Icons.IRadzenFontIcon?`.

### Muster

In sieben Typen wortgleich umgesetzt:

```csharp
[Parameter]
public GRadzenBase.Icons.IRadzenFontIcon? GIcon
{
    get => _GIcon;
    set
    {
        _GIcon = value;
        Icon = value?.CodePoint;      // GIcon überschreibt Icon
    }
}
private GRadzenBase.Icons.IRadzenFontIcon? _GIcon;
```

Der Setter schreibt den Codepoint in das vorhandene `Icon`; das Rendering des Zeichens bleibt
unverändert. Zusätzlich hängt jede Komponente `GIcon?.IconSetCssClass()` an ihre
Komponenten-CSS-Klasse, damit die richtige Schriftart greift.

> Zur Reihenfolgeabhängigkeit dieses Musters siehe [Befund 2](#2-gicon-überschreibt-icon-nur-abhängig-von-der-attributreihenfolge).

### Betroffene Dateien

| Datei | Neben der `GIcon`-Property |
| --- | --- |
| `RadzenIcon.razor.cs` | `IconSetCssClass()` in `GetComponentCssClass()` |
| `RadzenButton.razor.cs` | `IconSetCssClass()` in `ButtonClass`; zusätzlich `GBusyIcon` |
| `RadzenSplitButton.razor.cs` | wie Button; zusätzlich `GBusyIcon` |
| `RadzenLink.razor.cs` | `GetComponentCssClass()` auf `ClassList` umgestellt |
| `RadzenMenuItem.razor.cs` | `IconSetCssClass()` in `GetComponentCssClass()` |
| `RadzenPanelMenuItem.razor.cs` | neue Hilfsmethode `getIconCssClass()` |
| `ContextMenuService.cs` (`ContextMenuItem`) | zusätzlich `CssClass`-Property |
| `RadzenButton.razor`, `RadzenSplitButton.razor`, `RadzenToggleButton.razor` | `GBusyIcon` im Busy-Zustand |
| `RadzenPanelMenuItem.razor` | `<i class="@getIconCssClass()">` statt fester Klassenliste |
| `RadzenContextMenu.razor` | reicht `GIcon` und `CssClass` durch |

### `GBusyIcon`

Ersetzt — wenn gesetzt — das fest verdrahtete `refresh`-Icon der Busy-Animation. Ohne `GBusyIcon`
bleibt das Verhalten unverändert. `RadzenToggleButton` erbt die Property von `RadzenButton`, nur
das Markup ist dort zusätzlich angepasst.

### Kontextmenü

`ContextMenuItem` erhält neben `GIcon` eine `CssClass`-Property, die gesetzte Klassen mit
`GIcon.IconSetCssClass()` kombiniert. `RadzenContextMenu.razor` reicht beides an das erzeugte
`RadzenMenuItem` durch:

```razor
<RadzenMenuItem ... GIcon="@item.GIcon" ... class="@item.CssClass"></RadzenMenuItem>
```

## E. RadzenTree

### `BeforeChange` (mit `TreeCancelEventArgs.cs`, neu)

Ein `EventCallback<TreeCancelEventArgs>`, ausgelöst **bevor** eine neue Auswahl übernommen wird.
Der Abonnent kann über `args.Cancel()` abbrechen:

```csharp
if (selectedItem != item)
{
    if (await IsChangeCancelled(item))
    {
        item.Unselect();
        return;
    }
    SelectedItem = item;
    ...
}
```

`TreeCancelEventArgs` erbt von `TreeEventArgs` und trägt ein internes `Cancelled`-Flag. Typischer
Anwendungsfall: Nachfrage bei ungespeicherten Änderungen.

### `Reload(object? value = null)`

Erzwingt die Neubewertung eines Knotens, sodass per `Expand` lazy erzeugte Items auch dann
realisiert werden, wenn das Datenmodell von außen geändert wurde. Laut Code-Kommentar behebt die
Methode zwei Unzulänglichkeiten des originalen `Reload()`:

1. Von außen gibt es keinen Zugriff auf ein `RadzenTreeItem` — daher die Adressierung über den
   `value`.
2. Der Aufruf ohne Item funktioniert bei Lazy Loading nicht, weil sich `items` während der
   Aufzählung ändert und eine Exception auslöst. `Reload` iteriert deshalb über `items.ToList()`.

> Diese Methode bricht die öffentliche API — siehe [Befund 4](#4-radzentreereload-ist-nicht-mehr-parameterlos-aufrufbar).

## F. DropDown: LoadDataOnOpen

`Radzen.Blazor/RadzenDropDown.razor.cs`

Verzögert den initialen `LoadData`-Aufruf bis zum ersten Öffnen des Popups, statt ihn beim Rendern
der Seite auszulösen. Zweck: keine Daten für Dropdowns laden, die der Benutzer nie öffnet.

| Parameter | Typ | Beschreibung |
| --- | --- | --- |
| `LoadDataOnOpen` | `bool` | Aktiviert das verzögerte Laden. Default `false`. |
| `LoadDataOnOpenSelectedItem` | `object?` | Item, das den aktuellen `Value` repräsentiert, solange noch nichts geladen ist. |

Die Umsetzung verteilt sich über die Klasse:

- `SetParametersAsync`: Bedingung um `&& !LoadDataOnOpen` erweitert — der Aufruf beim Rendern
  unterbleibt.
- `OpenPopup`: ruft `await LoadDeferredData()` auf.
- `HandleKeyPress`: ruft `LoadDeferredData()` **vor** `base.HandleKeyPress(...)` auf, sofern das
  Popup geschlossen ist und die Taste nicht `Escape`/`Tab` ist. Nötig, weil die
  Basisimplementierung bei `Data == null` früh aussteigt und die Taste sonst wirkungslos bliebe.
- `LoadDeferredData()`: lädt nur, wenn `LoadDataOnOpen` gesetzt ist, ein `LoadData`-Delegate
  existiert und `Data == null` ist. Setzt anschließend `shouldReposition = true`, weil die Items
  im Popup gerendert werden und dieses danach neu positioniert werden muss.
- `SelectItemFromValue(object?)` (override): Solange nichts geladen ist, gibt es kein Item, aus dem
  der Text aufgelöst werden könnte — die Basisklasse lässt `selectedItem` also `null`. In diesem
  Fall wird `LoadDataOnOpenSelectedItem` eingesetzt.

**Einschränkung:** `LoadDataOnOpenSelectedItem` gilt nur für Einfachauswahl und nur, solange
`Value` nicht `null` ist. Ohne diesen Parameter zeigt das geschlossene Dropdown bis zum ersten
Öffnen den `Placeholder` statt des ausgewählten Textes.

## G. DropDown: Wertauflösung

`Radzen.Blazor/DropDownBase.cs`

Upstream entscheidet an zwei Stellen mit
`typeof(EnumerableQuery).IsAssignableFrom(view.GetType())`, ob der ausgewählte Wert lokal oder über
eine Query-Expression gesucht wird. Der Fork ersetzt das durch zwei neue Methoden.

### `IsInMemorySource(IEnumerable)`

```csharp
return source is EnumerableQuery || source is not IQueryable;
```

Eine schlichte Liste — etwa die, die ein `LoadData`-Handler an `Data` zuweist — liegt im
Arbeitsspeicher, ist aber keine `EnumerableQuery`. Upstream schickt sie deshalb durch den
Query-Pfad. Nur ein echtes `IQueryable` (z. B. Entity Framework) soll dort landen, damit die Suche
serverseitig bleibt.

### `FindItemByValue(IEnumerable, object)`

Sucht das Item, dessen `ValueProperty` dem Wert entspricht — mit `object.Equals` statt
`Expression.Equal`. Weicht der Typ des Wertes vom Typ der Item-Eigenschaft ab (z. B. `int` gegen
eine Enum-Eigenschaft), wird er einmalig über `CoerceValue` angeglichen, genau wie es
`AddSelectedItemsByValue` für die Mehrfachauswahl tut.

### Warum

Der Query-Pfad vergleicht mit `Expression.Equal`. Das verlangt, dass der gebundene Wert exakt dem
deklarierten Eigenschaftstyp entspricht, und ignoriert Überschreibungen von
`object.Equals(object?)`:

- Bei abweichendem Werttyp wirft es eine Exception.
- Bei einer referenztypisierten oder `object`-typisierten Value-Property findet es still nichts —
  verglichen wird über Objektreferenz statt über die `Equals`-Überschreibung.

## H. DropDown: PopupClasses

`Radzen.Blazor/RadzenDropDown.razor.cs`

```csharp
[Parameter]
public string? PopupClasses { get; set; }
```

Zusätzliche CSS-Klassen für den Popup-Container, angehängt in `PopupCssClass`:

```csharp
string PopupCssClass => ClassList.Create(Multiple ? "rz-multiselect-panel" : "rz-dropdown-panel")
                                 .AddInputSize(InputSize)
                                 .Add(PopupClasses) // GAUSS-spezifisch
                                 .ToString();
```

`ClassList.Add` ignoriert `null` und Leerstrings, eine zusätzliche Prüfung ist nicht nötig.

**Historie und Warnung:** Ursprünglich in `gauss` (Commit `12ca428f`, 20.06.2025) eingeführt und
dort direkt im Markup verdrahtet:

```razor
class="@(Multiple ? "rz-multiselect-panel" : "rz-dropdown-panel") @PopupClasses"
```

Ein späterer Upstream-Merge ersetzte dieses Markup durch `@PopupCssClass` und verlor dabei
`@PopupClasses`. **Im Branch `gauss` ist der Parameter deshalb bis heute wirkungslos** — er wird
deklariert, aber nirgends verwendet. Die Anbindung über `PopupCssClass` in `gauss-next` ist
merge-robuster, weil sie in einer C#-Property statt im Markup sitzt.

Nur `RadzenDropDown` hat diese Property. `RadzenDropDownDataGrid` und `RadzenAutoComplete` haben
dieselbe `PopupCssClass`-Struktur, aber keine entsprechende Property.

## I. Tests

| Datei | Inhalt |
| --- | --- |
| `Radzen.Blazor.Tests/DropDownTests.cs` (+202 Zeilen) | Tests zu Abschnitt F: `LoadData`-Aufruf beim Öffnen und bei Tastendruck, Rendern der Items nach dem Öffnen, Anzeige und Ablösung von `LoadDataOnOpenSelectedItem`, Verhalten ohne `Value` bzw. ohne `LoadDataOnOpen`. |
| `Radzen.Blazor.Tests/DropDownValueResolutionTests.cs` (neu, 182 Zeilen) | Tests zu Abschnitt G: passender Werttyp, `Nullable` gegen nicht-nullable Property, `int` gegen `long` und gegen Enum, Referenztypen über `Equals`, `object`-Property mit geboxtem Wert, unbekannte und nicht konvertierbare Werte, Mehrfachauswahl, unveränderter Query-Pfad für echte `IQueryable`. |

Zu den Abschnitten D, E und H gibt es **keine** eigenen Tests. Die Befunde unten betreffen genau
diese Abschnitte — Befund 1 fiel nur auf, weil er einen bestehenden Upstream-Test brach.

---

## Befunde

Alle vier waren reproduziert, nicht nur vermutet. Befund 1 ist inzwischen behoben.

### 1. Überzählige CSS-Klasse in `RadzenIcon` und `RadzenMenuItem` — behoben

Beide Komponenten hängten die GAUSS-Icon-Set-Klasse per String-Interpolation an:

```csharp
return $"notranslate rzi{(IconStyle.HasValue ? ... : "")} {GIcon?.IconSetCssClass()}";
```

Das Leerzeichen vor `{GIcon?...}` ist literal und blieb auch stehen, wenn `GIcon` null war.
Gerendert wurde `class="notranslate rzi "` statt `class="notranslate rzi"` — was
`Radzen.Blazor.Tests.IconTests.Icon_Renders_IconParameter` brach (am Upstream-Stand war der Test
grün). `RadzenMenuItem.GetComponentCssClass()` hatte dasselbe Muster; dort fiel es mangels Test
nur nicht auf.

Beide Methoden verwenden jetzt `ClassList`, analog zu `RadzenLink` und `RadzenPanelMenuItem`:

```csharp
return ClassList.Create("notranslate rzi")
                .Add($"rzi-{IconStyle?.ToString().ToLowerInvariant()}", IconStyle.HasValue)
                .Add(GIcon?.IconSetCssClass()) // GAUSS-spezifisch
                .ToString();
```

`ClassList.Add` überspringt null und Leerstrings, das Leerzeichen entsteht also nur zwischen
tatsächlich vorhandenen Klassen. Klassenreihenfolge und -inhalt bleiben ansonsten unverändert.

### 2. `GIcon` überschreibt `Icon` nur abhängig von der Attributreihenfolge

Die XML-Dokumentation aller sieben `GIcon`-Properties sagt: *„GIcon overwrites the value of
Icon."* Das stimmt nur, wenn `Icon` im Markup **vor** `GIcon` steht. Blazor setzt
Parameter-Properties in der Reihenfolge der Attribute; steht `Icon` hinten, überschreibt es den
vom `GIcon`-Setter gesetzten Codepoint wieder.

Verifiziert mit bUnit gegen `RadzenIcon` (`GIcon.CodePoint = "star"`, `Icon = "home"`):

| Reihenfolge | Ergebnis |
| --- | --- |
| `Icon="home" GIcon="@g"` | `class="notranslate rzi g-icons"` → **star** |
| `GIcon="@g" Icon="home"` | `class="notranslate rzi g-icons"` → **home** |

Der zweite Fall ist der unangenehme: die Icon-Set-Klasse des GAUSS-Fonts wird gesetzt, der
Codepoint stammt aber aus dem Radzen-Satz — das Zeichen wird also im falschen Font gesucht und
erscheint als Fehlglyphe.

Solange beide Parameter nie gemeinsam gesetzt werden, tritt das nicht auf. Falls doch, wäre eine
Auflösung in `OnParametersSet()` statt im Setter robuster.

### 3. Release ohne Testlauf

In `.forgejo/workflows/release.yml` ist der Testschritt auskommentiert:

```yaml
      # - name: Tests
      #   run: dotnet test "$TEST_PROJECT" -c Release --no-restore
```

Ein Tag-Push veröffentlicht also ungetestet. Das lässt sich derzeit auch nicht ohne Weiteres
aktivieren: von den 5334 Tests scheitern 461 (siehe [Testlage](#testlage)), davon 460 aus
plattformbedingten Gründen. Ein sinnvolles Aktivieren setzt voraus, die Markdown-Tests entweder
zu reparieren oder per `--filter` auszuschließen.

Nebenbei zwei tote Verweise:

- `publish.ps1` nennt `.forgejo/workflows/publish-nuget.yml`, die Datei heißt `release.yml`.
- `release.yml` verweist auf `.forgejo/workflows/README.md`, die es nicht gibt.

### 4. `RadzenTree.Reload()` ist nicht mehr parameterlos aufrufbar

`RadzenTree` hat jetzt zwei `Reload`-Überladungen mit je vollständig optionalem Parameter:

```csharp
public async Task Reload(RadzenTreeItem? item = null)   // Upstream, Zeile 407
public async Task Reload(object? value = null)          // GAUSS,    Zeile 848
```

Ein Aufruf `tree.Reload()` ist damit mehrdeutig. Verifiziert am minimalen Gegenbeispiel:

```
error CS0121: The call is ambiguous between the following methods or properties:
'Tree.Reload(Item?)' and 'Tree.Reload(object?)'
```

Die Bibliothek selbst baut durch, weil sie intern nur mit Argument aufruft. Betroffen sind
**Konsumenten des Pakets**: jeder bestehende `Reload()`-Aufruf bricht beim Umstieg auf
`Radzen.Blazor.GAUSS`. Auch `Reload(null)` ist mehrdeutig.

Ein anderer Name (etwa `ReloadByValue`) oder ein Weglassen des Defaultwertes würde das auflösen.

Randnotiz zur selben Methode: die Suche verwendet `items.FirstOrDefault(i => i.Value == value)`.
Auf `object` ist `==` Referenzvergleich — eine `Equals`-Überschreibung greift nicht. Das ist
genau das Verhalten, das Abschnitt G an anderer Stelle bewusst abgeschafft hat.

---

## Merge-Risiken

Die GAUSS-Regionen am Dateiende sind merge-sicher. Gefährlich sind die Eingriffe **außerhalb**
davon — an einer solchen Stelle ist `PopupClasses` schon einmal verloren gegangen (Abschnitt H).
Diese Stellen nach jedem Upstream-Merge gezielt prüfen:

| Datei | Stelle |
| --- | --- |
| `RadzenIcon.razor.cs`, `RadzenMenuItem.razor.cs`, `RadzenLink.razor.cs` | `GetComponentCssClass()` |
| `RadzenButton.razor.cs`, `RadzenSplitButton.razor.cs` | `ButtonClass` |
| `RadzenDropDown.razor.cs` | `PopupCssClass`, `SetParametersAsync`, `OpenPopup`, `HandleKeyPress` |
| `DropDownBase.cs` | die beiden `IsInMemorySource(view)`-Aufrufstellen |
| `RadzenTree.razor.cs` | `SelectItem` (`IsChangeCancelled`-Block) |
| `RadzenButton.razor`, `RadzenSplitButton.razor`, `RadzenToggleButton.razor` | `GBusyIcon`-Block im Busy-Zustand |
| `RadzenPanelMenuItem.razor` | beide `<i class="@getIconCssClass()">` |
| `RadzenContextMenu.razor` | `GIcon`/`CssClass` am `RadzenMenuItem` |
| `Radzen.Blazor.csproj` | Sass/Terser-Targets — Upstream-Stand ist dort nicht übernehmbar |
| `.github/workflows/` | prüfen, ob `ci.yml` und `premium-themes.yml` wieder aktive Trigger haben |

Ein schneller Selbsttest nach dem Merge:

```bash
grep -rn "GAUSS-spezifisch" Radzen.Blazor/            # 13 Treffer erwartet
grep -rn "GIcon\|GBusyIcon" Radzen.Blazor/*.razor     # 7 Treffer erwartet
grep -n "IsInMemorySource" Radzen.Blazor/DropDownBase.cs   # 3 Treffer erwartet
```

**Konvention:** Neue Abweichungen bitte ebenfalls in `#region GAUSS-spezifische Änderungen` am
unteren Ende der Klasse ablegen und Eingriffe in Upstream-Code mit `// GAUSS-spezifisch`
kennzeichnen — das hält die Konfliktfläche klein und macht sie auffindbar.
