# Prayer Time Accuracy Test

A general, data-driven accuracy test for the prayer-time calculation module
(`GamePause.SalahEvents.PrayerTimes`). It computes all six prayer times with the **real
C# calculation module** for **4,048 reference records** (24,288 prayer-time
comparisons) taken from a trusted online source (the AlAdhan API) and compares the
results against the reference.

The dataset is bundled as a Unity asset (`Resources/prayer_times_dataset.json`), so the
test runs **fully offline** — no network access is needed at test time.

---

## Running the test

### 1. Inspector button (single click)
1. Add the `PrayerTimeAccuracyTester` component to any GameObject.
2. Press the **Run Accuracy Test** button in the Inspector.

The result counters populate on the component, a full report is logged to the
Console, and the `onTestingDone` UnityEvent fires (wire it to your own UI if needed).

### 2. Editor window
Menu **Game Pause → Prayer Time Accuracy Test** → **Run Full Accuracy Test**.
Shows the same report in a scrollable window, plus **Save Outlier CSV** which writes
every deviation of ≥2 minutes to `Temp/prayer_accuracy_outliers.csv`.

### 3. PlayMode tests (CI / headless)
Seven NUnit PlayMode tests in `Tests/PrayerTimeAccuracyPlayModeTests.cs` assert the
accuracy thresholds (they fail the build if accuracy regresses):

```
"…/Unity.exe" -batchmode -quit -nographics -projectPath "F:/Unity Projects/Game Pause" \
  -runTests -testPlatform playmode -assemblyNames GamePause.SalahEvents.PrayerTimeAccuracyTests -logFile -
```

(Use `-assemblyNames` to run only this suite and skip unrelated tests in the project.)

### 4. Plain function call
```csharp
var dataset = PrayerTimeAccuracyTester.LoadDataset();          // Resources.Load + JsonUtility
var report  = PrayerTimeAccuracyEngine.Evaluate(dataset);      // 4048 records, ~40 ms
Debug.Log(PrayerTimeAccuracyTester.FormatReport(report));
```

---

## The dataset

**Source:** AlAdhan API v1 (`https://api.aladhan.com/v1/timings`). Its internal engine
is the Meezaan/PrayerTimes.org algorithm family — the same reference family most Islamic
apps use. Downloaded for the year **2026**, one sample every **4 days**.

**Shape:** 4,048 records = 44 (city × method × Asr-school) combinations × 92 dates.
Each record stores the reference times for six prayers (Fajr, Sunrise, Dhuhr, Asr,
Maghrib, Isha), so the full test performs **24,288 comparisons**.

### Record schema

| Field | Meaning |
|---|---|
| `city`, `country` | Location name |
| `lat`, `lon` | WGS-84 coordinates (degrees) |
| `tz` | IANA timezone identifier |
| `date` | Gregorian date `YYYY-MM-DD` |
| `method` | Calculation method name (enum name, see below) |
| `method_id` | AlAdhan numeric method id |
| `asr_school` | `Standard` (Shafi, 1× shadow) or `Hanafi` (2× shadow) |
| `hijri_day`, `hijri_month_number` | Hijri date (day, month 1–12) |
| `utc_offset` | UTC offset **at local noon** of `date`, in hours |
| `fajr` … `isha` | Reference times, `HH:MM` 24h local wall clock |
| `ramadan` | `true` for UmmAlQura records in Hijri month 9 (Ramaḍān) |

### Reference conventions (important)

- **Timezone / DST:** the reference local times follow the UTC offset **at local noon**
  of the record date. This was verified empirically against the API on all 6
  DST-transition days inside the date range, so the engine must apply the same
  per-record offset (stored in `utc_offset`) rather than a fixed zone offset.
- **Rounding:** the reference rounds each computed time by **+30 s then truncates** to
  the minute (PrayTimes `modifyFormats` behavior). The engine replicates this so the
  comparison isolates astronomical accuracy, not rounding.
- **High-latitude rule:** the reference uses `ANGLE_BASED` (the API default). In winter
  at high latitudes the Fajr/Isha angle is sometimes unreachable (326 records); the
  engine applies the same `ANGLE_BASED` clamp (portion = angle/60 × night) so those
  records are still comparable.
- **UmmAlQura Ramadan:** during Ramaḍān UmmAlQura sets **Isha = Maghrib + 120 min**
  (instead of the 90-min interval). These 32 records are flagged `ramadan: true` and the
  engine applies the same rule, so the comparison again measures astronomy only.

### Locations (21 cities, 20 IANA timezones)

| City | Country | Lat | Timezone | Method(s) |
|---|---|---:|---|---|
| Auckland | New Zealand | −36.9° | Pacific/Auckland | MuslimWorldLeague |
| Cape Town | South Africa | −33.9° | Africa/Johannesburg | MuslimWorldLeague |
| Sydney | Australia | −33.9° | Australia/Sydney | MuslimWorldLeague |
| Singapore | Singapore | +1.4° | Asia/Singapore | Singapore |
| Kuala Lumpur | Malaysia | +3.1° | Asia/Kuala_Lumpur | MuslimWorldLeague |
| Mecca | Saudi Arabia | +21.4° | Asia/Riyadh | UmmAlQura |
| Jeddah | Saudi Arabia | +21.5° | Asia/Riyadh | UmmAlQura |
| Karachi | Pakistan | +24.9° | Asia/Karachi | Karachi |
| Cairo | Egypt | +30.0° | Africa/Cairo | EgyptianAuthority |
| Tokyo | Japan | +35.7° | Asia/Tokyo | MuslimWorldLeague |
| Tehran | Iran | +35.7° | Asia/Tehran | Qom, Tehran |
| New York | USA | +40.7° | America/New_York | ISNA |
| Istanbul | Turkey | +41.0° | Europe/Istanbul | MuslimWorldLeague |
| Toronto | Canada | +43.7° | America/Toronto | ISNA |
| Paris | France | +48.9° | Europe/Paris | MuslimsOfFrance |
| London | UK | +51.5° | Europe/London | MuslimWorldLeague |
| Moscow | Russia | +55.8° | Europe/Moscow | Russia |
| Stockholm | Sweden | +59.3° | Europe/Stockholm | MuslimWorldLeague |
| Oslo | Norway | +59.9° | Europe/Oslo | MuslimWorldLeague |
| Helsinki | Finland | +60.2° | Europe/Helsinki | MuslimWorldLeague |
| Reykjavik | Iceland | +64.1° | Atlantic/Reykjavik | MuslimWorldLeague |

### Calculation methods (all 10 supported by the module)

| Method | Records | Tested in |
|---|---:|---|
| MuslimWorldLeague | 2,024 | 11 cities (the API default, globally used) |
| ISNA | 368 | New York, Toronto |
| UmmAlQura | 368 | Mecca, Jeddah |
| EgyptianAuthority | 184 | Cairo |
| Karachi | 184 | Karachi |
| MuslimsOfFrance | 184 | Paris |
| Qom | 184 | Tehran |
| Russia | 184 | Moscow |
| Singapore | 184 | Singapore |
| Tehran | 184 | Tehran |

Each method is tested in the country that actually uses it (MuslimWorldLeague in 11
countries, since it is the common default). The **Asr school is exactly 50/50**
(2,024 Standard + 2,024 Hanafi records), so both shadow factors (1× and 2×) are
equally stress-tested.

### Deliberate edge-case coverage

- **Seasons** — 92 evenly spaced dates across 2026 (sun declination −23°…+23°).
- **High latitudes** — Reykjavik 64.1°N (Fajr/Isha unreachable in winter → 326
  records exercise the high-latitude clamp), Helsinki/Oslo/Stockholm 59–60°.
- **Equator** — Singapore 1.4°N, Kuala Lumpur 3.1°N (day ≈ 12 h year-round).
- **Southern hemisphere** — Auckland, Cape Town, Sydney (seasons inverted).
- **Longitude/timezone span** — −79.4° (Toronto) to +174.8° (Auckland), 20 timezones,
  several with DST transitions inside the range.
- **UmmAlQura Ramadan policy** — 32 records (2026-02-18 → 2026-03-18, Hijri month 9).

### Regenerating / extending the dataset

`Tools/regenerate_dataset.py` is self-contained and reproduces the bundled dataset
byte-for-byte (validated). It downloads fresh data from the AlAdhan API and rebuilds
the JSON with the exact schema above.

```
python Tools/regenerate_dataset.py                    # download + build (~10 min)
python Tools/regenerate_dataset.py --from-raw DIR     # build from cached raw responses
python Tools/regenerate_dataset.py --out FILE         # custom output path
```

Requires Python 3.9+ and `requests` (on Windows without system tz data: `pip install
tzdata`). Cached raw responses are written to `Tools/raw/` and skipped on re-runs.
To add cities, methods, or a finer date step, edit `CITIES`, `METHODS`, `ASR_VARIANTS`
or the `YEAR/STEP_DAYS/N_DATES` constants at the top of the script, then overwrite
`Resources/prayer_times_dataset.json` and re-run the test.

---

## Accuracy results (baseline)

Verified identically in three environments: the Python mirror of the pipeline, a
standalone .NET harness compiling the real module sources, and Unity 6000.2.10f1
headless PlayMode (the numbers below are from the C# engine).

### Overall (4,048 records, 24,288 comparisons)

| Metric | Value |
|---|---|
| Within ±1 min | **93.56%** (22,724) |
| Within ±2 min | **98.56%** (23,939) |
| Within ±5 min | **99.93%** (24,270) |
| Mean absolute error | **0.431 min** |
| Max absolute error | **11 min** |
| Invalid raw times (high-latitude clamp) | 326 |

### Per prayer (diff = calculated − reference, minutes)

| Prayer | n | mean | std | min | max | ±1% | ±2% |
|---|---:|---:|---:|---:|---:|---:|---:|
| Fajr | 4048 | 0.00 | 0.43 | −2 | 2 | 99.9% | 100.0% |
| Sunrise | 4048 | −0.00 | 0.47 | −1 | 1 | 100.0% | 100.0% |
| Dhuhr | 4048 | −0.01 | 0.29 | −1 | 1 | 100.0% | 100.0% |
| Asr | 4048 | 0.02 | 1.10 | −8 | 11 | 92.8% | 96.5% |
| Maghrib | 4048 | −0.01 | 1.09 | −3 | 3 | 83.0% | 97.6% |
| Isha | 4048 | 0.01 | 1.09 | −4 | 5 | 85.7% | 97.3% |

### Per method

| Method | n | mean\|d\| | ±1% | ±2% | max |
|---|---:|---:|---:|---:|---:|
| EgyptianAuthority | 1104 | 0.274 | 100.0% | 100.0% | 1 |
| ISNA | 2208 | 0.585 | 92.2% | 100.0% | 2 |
| Karachi | 1104 | 0.198 | 100.0% | 100.0% | 1 |
| MuslimWorldLeague | 12144 | 0.518 | 90.3% | 97.3% | 11 |
| MuslimsOfFrance | 1104 | 0.512 | 93.0% | 100.0% | 2 |
| Qom | 1104 | 0.248 | 100.0% | 100.0% | 1 |
| Russia | 1104 | 0.585 | 88.6% | 98.4% | 3 |
| Singapore | 1104 | 0.124 | 99.5% | 100.0% | 2 |
| Tehran | 1104 | 0.279 | 100.0% | 100.0% | 1 |
| UmmAlQura | 2208 | 0.198 | 100.0% | 100.0% | 1 |

### Outliers

1,564 comparisons deviate by ≥2 minutes; only **33 exceed ±5 minutes**, and every one
of them is **winter Asr (29× Reykjavik, 2× Helsinki) or winter Isha (2× Reykjavik)**
under MuslimWorldLeague, on dates Jan–Feb and Nov–Dec. The maximum is +11 min
(Reykjavik Asr, early January).

**Why:** the reference (PrayTimes family) evaluates the sun position with the
USNO *approximation* (a truncated series) while the module under test uses the full
NOAA formula, and the reference samples the sun at the *previous* iteration's time.
At 64°N in winter the Asr shadow angle is very shallow, so those two algorithmic
differences amplify into multi-minute deltas. This is a genuine, expected algorithmic
delta — **not** a bug in the module. Every other city, method, and date combination
is within ±3 minutes. Eight of the ten methods are 100% within ±2 minutes; the two
exceptions are MuslimWorldLeague (97.3%) and Russia (98.4%), both driven by the
winter high-latitude cases above.

---

## File layout

```
Assets/GamePause/SalahEvents/PrayerTimes/AccuracyTest/
├── README.md                          ← this file
├── Runtime/                           asmdef: GamePause.SalahEvents.PrayerTimeAccuracyTest.Runtime
│   ├── PrayerTimeAccuracyEngine.cs    core: real module → 6 times per record + report
│   ├── PrayerTimeAccuracyModels.cs    JsonUtility-compatible POCOs (public fields)
│   ├── PrayerTimeAccuracyTester.cs    MonoBehaviour, [Button], counters, events
│   ├── PrayerButtonAttribute.cs       local [Button] (not built into Unity 6000.x)
│   └── GamePause.SalahEvents.PrayerTimeAccuracyTest.Runtime.asmdef
├── Editor/                            asmdef: …Editor
│   ├── PrayerTimeAccuracyTestWindow.cs  menu window + run + CSV export
│   ├── PrayerButtonDrawer.cs            inspector button rendering
│   └── GamePause.SalahEvents.PrayerTimeAccuracyTest.Editor.asmdef
├── Tests/                             asmdef: GamePause.SalahEvents.PrayerTimeAccuracyTests (PlayMode)
│   ├── PrayerTimeAccuracyPlayModeTests.cs  7 threshold tests
│   └── GamePause.SalahEvents.PrayerTimeAccuracyTests.asmdef
├── Resources/
│   └── prayer_times_dataset.json      1.7 MB, 4,048 records (loaded via Resources.Load)
└── Tools/
    └── regenerate_dataset.py          self-contained dataset (re)generator
```

## Technical notes

- **Engine isolation:** the engine calls the public calculator API
  (`PrayerTimeCalculator.Calculate`) configured like the reference: the `AngleBased`
  high-latitude rule and the dataset's UmmAlQura Ramadan flag. Only reporting concerns
  (+30 s rounding, noon UTC offset) live in the engine, so the test measures the same
  code the game uses; residual differences are the genuine USNO-approximation vs
  full-NOAA algorithmic delta.
- **JsonUtility:** the model POCOs use public fields with exact JSON key names
  (no nullables, no dictionaries) to stay compatible with `JsonUtility`.
- **`[Button]`:** Unity 6000.2.10f1 has no built-in `UnityEngine.Button` attribute;
  `PrayerButtonAttribute` + `PrayerButtonDrawer` provide it.
- **PlayMode test asmdef:** must **not** set `includePlatforms: ["Editor"]` — the test
  framework classifies EditorOnly assemblies as EditMode and silently skips them in
  `-testPlatform playmode` runs. The test asmdef uses `includePlatforms: []` +
  `defineConstraints: ["UNITY_INCLUDE_TESTS"]` + `overrideReferences` + precompiled
  `nunit.framework.dll` (same shape as the project's existing calculator test asmdef).
- **Unity 6000.x C# caveat:** interpolated-string alignment takes a plain integer
  (`{x,6}`); a `>` prefix (`{x,>6}`) is a compile error (CS1525).
