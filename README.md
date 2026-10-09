# Game Pause
A Unity package for pausing games at scheduled events. Each kind of event is its own feature module.

**Salah Events – Accurate Prayer Times for Unity** is the first feature: it calculates the five
daily prayer times for any location, so a game can pause or react at each prayer.

## Layout

```
Assets/GamePause/
├── SalahEvents/                    feature: prayer-time events
│   └── PrayerTimes/
│       ├── Runtime/      GamePause.SalahEvents.PrayerTimes        pure C# (no UnityEngine): calculator + astronomy
│       │   ├── PrayerTimeCalculator.cs     entry point: Calculate(date, coordinates) → DailyPrayerTimes
│       │   ├── Astronomical/               Julian dates, solar position, NOAA formulas
│       │   ├── Data/                       value objects (coordinates, parameters, results)
│       │   ├── Enums/                      CalculationMethod, AsrJuristicMethod, HighLatitudeRule, PolarEstimationRule, ShiaMarja, PrayerType
│       │   ├── Methods/                    calculation-method presets + factory
│       │   └── Validation/                 coordinate / parameter validators
│       ├── Tests/        GamePause.SalahEvents.PrayerTimes.Tests  EditMode unit tests
│       └── AccuracyTest/                   4,048-record accuracy check against the AlAdhan API
├── Location/                       shared: where the player is
│   ├── Runtime/          GamePause.Location         LocationManager (MonoBehaviour) + Data/Providers/Persistence
│   ├── UI/               GamePause.Location.UI      ManualLocationUI (uGUI + TextMeshPro)
│   └── Editor/           GamePause.Location.Editor  LocationManager inspector
└── Debugging/            GamePause.Debugging        shared: on-screen DebugLogger (TextMeshPro)
```

Features live under `GamePause.<Feature>` and may use the shared modules. Dependencies point one way:
`Location.UI → Location → Debugging`. `SalahEvents.PrayerTimes` depends on nothing.

## Calculating prayer times

```csharp
using GamePause.SalahEvents.PrayerTimes;
using GamePause.SalahEvents.PrayerTimes.Data;
using GamePause.SalahEvents.PrayerTimes.Enums;

var calculator = new PrayerTimeCalculator(CalculationMethod.EgyptianAuthority);
DailyPrayerTimes times = calculator.Calculate(DateTime.Today, new GeographicCoordinates(30.0444, 31.2357));

DateTime maghribUtc = times.Maghrib;                                     // always present, UTC
DateTime maghribLocal = times.GetLocal(PrayerType.Maghrib, TimeZoneInfo.Local);
bool estimated = times.IsEstimated(PrayerType.Maghrib);                  // true at polar / high latitudes
if (times.TryGetNext(DateTime.UtcNow, out PrayerType next, out DateTime at)) { /* schedule pause */ }
```

### High latitudes

Every prayer always gets a time, because a missing time would be a missing in-game event.
Times that had to be estimated are flagged with `DailyPrayerTimes.IsEstimated`.
These rules follow published rulings where we could find them; anything marked
*provisional* is an extrapolation, pending a source.

**Twilight that never ends while the sun still rises and sets (about 48–66°):** `HighLatitudeRule` covers Fajr and Isha.

| Rule | What it does | Source |
|---|---|---|
| `AngleBased` (default) | Limits Fajr/Isha to angle/60 of the night | PrayTimes.org; used by AlAdhan (our accuracy reference). No institutional ruling found. |
| `NightFractionAt45` | Fajr/Isha take the same fraction of the night as at 45° on the same meridian | Islamic Fiqh Council (MWL), 9th session, Rajab 1406 AH, decree 8, region 2 |
| `SeventhOfNight` | Limits Fajr/Isha to 1/7 of the night | Hanafi authorities (Ashraf Ali Thanwi, Ibn ʿĀbidīn); Moonsighting Committee |
| `MiddleOfNight` | Limits Fajr/Isha to half the night | Bulghār practice reported by Ibn ʿĀbidīn / al-Marjānī. No institutional ruling found. |
| `None` | No limit; unreached times fall through to the polar rule | — |

**No sunrise or sunset (polar day or night):** `PolarEstimationRule` takes the whole day from a reference latitude on the same meridian, so Dhuhr stays at local solar noon.

| Rule | Reference latitude | Source |
|---|---|---|
| `ReferenceLatitude` (default) | 45° | Islamic Fiqh Council (MWL), 1406 AH, decree 8, region 3 |
| `NearestNormalLatitude` | Nearest latitude that needs no correction | Saudi Permanent Committee (Fatawa al-Lajnah 6/130), nearest place where the five prayers can be distinguished; nearest *latitude on the same meridian* is our simplification |
| `Mecca` | Mecca's latitude (21.42°) | Minority view noted by AMJA (fatwa 21730) |

**Other rules**

- **Fixed-interval Isha (Umm Al-Qura's 90/120 min) that would fall after Fajr:** combined with Maghrib.
  Source: European Council for Fatwa and Research.
- **Isha before Maghrib:** never allowed; such an Isha is moved to Maghrib.
- **Precaution minutes:** the MWL decree's ±2 minutes are not applied by default, because they move
  results away from the published timetables (MWL within ±1 min fell from 90.3% to 19.0%).
  Use `PrayerTimeAdjustments` to apply them.

**Shia methods (Tehran, Qom):** for these, `ShiaMarja` replaces the polar rule. Maghrib is always the local astronomical time.

| `ShiaMarja` | Polar day/night | Source |
|---|---|---|
| `Khamenei` (default) | Own horizon. Where there is no sunset, 45° on the same meridian (*provisional*) | Ajwibat al-Istiftaʾāt, q. 357 |
| `Sistani` | Nearest latitude on the same meridian with a day and night within 24 h | Minhaj al-Ṣāliḥīn 1/469, m. 88 (obligatory precaution) |
| `Makarem` | Temperate regions on the same meridian, taken as 45° (*provisional*) | Istiftāʾāt Jadīd 2/73, q. 139 |

When twilight never ends but the sun rises and sets, none of these rulings address Fajr/Isha, so
the general `HighLatitudeRule` is used there (*provisional*).

**Known limits**

- `MiddleOfNight` places Isha and the next Fajr together at midnight. They can differ by up to 0.4 min.
