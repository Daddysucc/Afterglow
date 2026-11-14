using System.Collections.Generic;

namespace LightBulb.Localization;

public partial class LocalizationManager
{
    private static readonly IReadOnlyDictionary<string, string> GermanLocalization = new Dictionary<
        string,
        string
    >
    {
        [nameof(SunsetLabel)] = "Untergang",
        [nameof(SunriseLabel)] = "Aufgang",
        [nameof(OffsetTooltipHeader)] =
            "Aktuelle Temperatur- und Helligkeitswerte werden durch einen Versatz angepasst:",
        [nameof(TemperatureOffsetLabel)] = "Temperaturversatz:",
        [nameof(BrightnessOffsetLabel)] = "Helligkeitsversatz:",
        [nameof(ClickToResetLabel)] = "Zum Zurücksetzen klicken",
        [nameof(OffsetLabel)] = "Versatz",
        [nameof(ToggleLightBulbTooltip)] = "LightBulb ein-/ausschalten",
        [nameof(HideToTrayTooltip)] = "LightBulb in den Infobereich minimieren",
        [nameof(PreviewText)] = "VORSCHAU",
        [nameof(StopPreviewTooltip)] = "Vorschau beenden",
        [nameof(StartPreviewTooltip)] = "24-Stunden-Zyklus vorschauen",
        [nameof(OpenSettingsTooltip)] = "Einstellungen öffnen",
        [nameof(ResetTooltip)] = "Alle Einstellungen auf die Standardwerte zurücksetzen",
        [nameof(LanguageLabel)] = "Sprache",
        [nameof(LanguageTooltip)] = "Bevorzugte Sprache der Benutzeroberfläche",
        [nameof(StartWithWindowsLabel)] = "Mit Windows starten",
        [nameof(StartWithWindowsTooltip)] = "LightBulb beim Windows-Start automatisch starten",
        [nameof(DefaultToDayConfigLabel)] = "Zur Tageskonfiguration zurückkehren",
        [nameof(DefaultToDayConfigTooltip)] =
            "Wenn LightBulb deaktiviert oder pausiert ist, die konfigurierte Tagestemperatur und -helligkeit anstelle des Standard-Monitor-Gammas wiederherstellen",
        [nameof(PauseWhenFullscreenLabel)] = "Bei Vollbild pausieren",
        [nameof(PauseWhenFullscreenTooltip)] =
            "LightBulb pausieren, wenn ein Vollbildfenster im Vordergrund ist",
        [nameof(GammaSmoothingLabel)] = "Gamma-Glättung",
        [nameof(GammaSmoothingTooltip)] =
            "Beim Ein- oder Ausschalten von LightBulb langsam übergehen, um den Augen Zeit zur Anpassung zu geben",
        [nameof(GammaPollingLabel)] = "Gamma-Abfrage",
        [nameof(GammaPollingTooltip)] =
            "Monitor-Gamma in regelmäßigen Abständen auffrischen, um andere Programme daran zu hindern, es zu überschreiben",
        [nameof(DayTemperatureTooltip)] = "Farbtemperatur tagsüber",
        [nameof(NightTemperatureTooltip)] = "Farbtemperatur nachts",
        [nameof(DayBrightnessTooltip)] = """
            Helligkeit tagsüber

            Diese Helligkeitseinstellung wirkt auf das Farbgamma, nicht auf die tatsächliche Bildschirmhelligkeit.
            Wenn Ihr Computer bereits in der Lage ist, die Bildschirmhelligkeit automatisch anzupassen (häufig bei Laptops), wird empfohlen, die Helligkeitssteuerung von LightBulb zu deaktivieren, indem beide Helligkeitswerte auf 100% gesetzt werden.
            """,
        [nameof(NightBrightnessTooltip)] = """
            Helligkeit nachts

            Diese Helligkeitseinstellung wirkt auf das Farbgamma, nicht auf die tatsächliche Bildschirmhelligkeit.
            Wenn Ihr Computer bereits in der Lage ist, die Bildschirmhelligkeit automatisch anzupassen (häufig bei Laptops), wird empfohlen, die Helligkeitssteuerung von LightBulb zu deaktivieren, indem beide Helligkeitswerte auf 100% gesetzt werden.
            """,
        [nameof(TransitionDurationTooltip)] =
            "Zeitdauer für den Wechsel zwischen Tages- und Nachtkonfiguration",
        [nameof(TransitionOffsetTooltip)] =
            "Versatz, der angibt, wie früh oder spät der Übergang relativ zu Sonnenaufgang und Sonnenuntergang beginnt",
        [nameof(ManualLabel)] = "Manuell",
        [nameof(ManualTooltip)] = "Sonnenaufgang und Sonnenuntergang manuell konfigurieren",
        [nameof(LocationBasedLabel)] = "Standortbasiert",
        [nameof(LocationBasedTooltip)] =
            "Standort konfigurieren und zur automatischen Berechnung der Sonnenzeiten nutzen",
        [nameof(YourLocationLabel)] = "Ihr Standort:",
        [nameof(AutoDetectLocationTooltip)] =
            "Standort automatisch anhand Ihrer IP-Adresse erkennen",
        [nameof(LocationQueryTooltip)] = """
            Geben Sie Ihren Standort mit geografischen Koordinaten oder einer Suchanfrage an

            Beispiele gültiger Eingaben:
            **41.25, -120.9762**
            **41.25°N, 120.9762°W**
            **New York, USA**
            **Germany**
            """,
        [nameof(SetLocationTooltip)] = "Standort festlegen",
        [nameof(LocationErrorText)] = "Fehler beim Ermitteln des Standorts, bitte erneut versuchen",
        [nameof(ToggleLightBulbHotkeyLabel)] = "LightBulb umschalten",
        [nameof(ToggleLightBulbHotkeyTooltip)] =
            "Globale Tastenkombination zum Ein-/Ausschalten von LightBulb",
        [nameof(ToggleWindowLabel)] = "Fenster umschalten",
        [nameof(ToggleWindowHotkeyTooltip)] =
            "Globale Tastenkombination zum Anzeigen/Ausblenden des LightBulb-Fensters",
        [nameof(IncreaseTemperatureOffsetLabel)] = "Temperaturversatz ↑",
        [nameof(IncreaseTemperatureOffsetTooltip)] =
            "Globale Tastenkombination zum Erhöhen des aktuellen Temperaturversatzes",
        [nameof(DecreaseTemperatureOffsetLabel)] = "Temperaturversatz ↓",
        [nameof(DecreaseTemperatureOffsetTooltip)] =
            "Globale Tastenkombination zum Verringern des aktuellen Temperaturversatzes",
        [nameof(IncreaseBrightnessOffsetLabel)] = "Helligkeitsversatz ↑",
        [nameof(IncreaseBrightnessOffsetTooltip)] =
            "Globale Tastenkombination zum Erhöhen des aktuellen Helligkeitsversatzes",
        [nameof(DecreaseBrightnessOffsetLabel)] = "Helligkeitsversatz ↓",
        [nameof(DecreaseBrightnessOffsetTooltip)] =
            "Globale Tastenkombination zum Verringern des aktuellen Helligkeitsversatzes",
        [nameof(ResetOffsetLabel)] = "Versatz zurücksetzen",
        [nameof(ResetOffsetHotkeyTooltip)] =
            "Globale Tastenkombination zum Zurücksetzen der Temperatur- und Helligkeitsversätze",
        [nameof(AppWhitelistLabel)] = "Anwendungs-Whitelist",
        [nameof(RefreshAppsTooltip)] = "Laufende Anwendungen aktualisieren",
        [nameof(PauseForWhitelistedTooltip)] =
            "LightBulb pausieren, wenn eine der ausgewählten Anwendungen im Vordergrund ist",
        [nameof(TraySettingsMenuItem)] = "Einstellungen",
        [nameof(TrayDisableUntilSunriseMenuItem)] = "Bis Sonnenaufgang",
        [nameof(TrayExitMenuItem)] = "Beenden",
        [nameof(TrayTooltipDisabled)] = "Deaktiviert",
        [nameof(CloseButton)] = "SCHLIESSEN",
        [nameof(UnstableBuildTitle)] = "Warnung: Instabiler Build",
        [nameof(UnstableBuildMessage)] = """
            Sie verwenden einen Entwicklungs-Build von {0}. Diese Builds wurden nicht gründlich getestet und können Fehler enthalten.

            Automatische Updates sind für Entwicklungs-Builds deaktiviert. Wenn Sie zu einem stabilen Release wechseln möchten, laden Sie ihn bitte manuell herunter.
            """,
        [nameof(SeeReleasesButton)] = "RELEASES ANZEIGEN",
        [nameof(LimitedGammaRangeTitle)] = "Begrenzter Gamma-Bereich",
        [nameof(LimitedGammaRangeMessage)] = """
            {0} hat festgestellt, dass erweiterte Gamma-Bereichssteuerungen auf diesem System nicht aktiviert sind.
            Dies kann dazu führen, dass einige Farbkonfigurationen nicht korrekt funktionieren.

            Drücken Sie BEHEBEN, um den Gamma-Bereich zu entsperren. Administratorrechte können erforderlich sein.
            """,
        [nameof(FixButton)] = "BEHEBEN",
        [nameof(WelcomeTitle)] = "Willkommen!",
        [nameof(WelcomeMessage)] = """
            Danke für die Installation von {0}!
            Für das beste personalisierte Erlebnis legen Sie bitte Ihre bevorzugte Solarkonfiguration fest.

            Drücken Sie OK, um die Einstellungen zu öffnen.
            """,
        [nameof(OkButton)] = "OK",
    };
}
