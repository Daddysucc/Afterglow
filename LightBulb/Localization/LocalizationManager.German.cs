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
        // Dashboard
        [nameof(BedtimeLabel)] = "Schlafenszeit",
        [nameof(DayStateLabel)] = "Tag",
        [nameof(EveningStateLabel)] = "Abend",
        [nameof(NightStateLabel)] = "Nacht",
        [nameof(SunriseTransitionPhaseLabel)] = "Übergang (Sonnenaufgang)",
        [nameof(SunsetTransitionPhaseLabel)] = "Übergang (Sonnenuntergang)",
        [nameof(NightTransitionPhaseLabel)] = "Übergang (Schlafenszeit)",
        [nameof(StatusDayScheduleText)] = "Tagesplan aktiv mit {0} K und {1} % Helligkeit.",
        [nameof(StatusEveningScheduleText)] = "Abendplan aktiv mit {0} K und {1} % Helligkeit.",
        [nameof(StatusNightScheduleText)] = "Nachtplan aktiv mit {0} K und {1} % Helligkeit.",
        [nameof(StatusWarmingText)] = "Erwärmung auf {0} K {2}.",
        [nameof(StatusCoolingText)] = "Abkühlung auf {0} K {2}.",
        [nameof(StatusDimmingText)] = "Dimmen auf {1} % {2}.",
        [nameof(StatusBrighteningText)] = "Aufhellen auf {1} % {2}.",
        [nameof(StatusWarmingDimmingText)] = "Erwärmung auf {0} K und Dimmen auf {1} % {2}.",
        [nameof(StatusWarmingBrighteningText)] = "Erwärmung auf {0} K und Aufhellen auf {1} % {2}.",
        [nameof(StatusCoolingDimmingText)] = "Abkühlung auf {0} K und Dimmen auf {1} % {2}.",
        [nameof(StatusCoolingBrighteningText)] = "Abkühlung auf {0} K und Aufhellen auf {1} % {2}.",
        [nameof(StatusForDayText)] = "für den Tag",
        [nameof(StatusForEveningText)] = "für den Abend",
        [nameof(StatusForNightText)] = "für die Nacht",
        [nameof(StatusManualOverrideText)] =
            "Manuelle Anpassung aktiv mit {0} K und {1} % Helligkeit.",
        [nameof(StatusPausedRemoteStreamingText)] =
            "Pausiert wegen Remote-Streaming, Originalfarben wiederhergestellt.",
        [nameof(StatusPausedFullScreenText)] =
            "Pausiert, solange eine Vollbildanwendung aktiv ist.",
        [nameof(StatusPausedScreenLockedText)] = "Pausiert, solange der Bildschirm gesperrt ist.",
        [nameof(StatusPausedManuallyText)] = "Manuell pausiert, automatischer Zeitplan ausgesetzt.",
        [nameof(StatusPausedAutomaticallyText)] =
            "Automatisch pausiert, Originalfarben wiederhergestellt.",
        [nameof(StatusPreviewText)] = "Vorschau der nächsten 24 Stunden…",
        [nameof(PausedRemoteStreamingLabel)] = "Pausiert (Remote-Streaming)",
        [nameof(PausedScreenLockedLabel)] = "Pausiert (Bildschirm gesperrt)",
        [nameof(PausedFullScreenLabel)] = "Pausiert (Vollbildanwendung)",
        [nameof(PausedWhitelistLabel)] = "Pausiert (Whitelist-Anwendung)",
        [nameof(PausedCaption)] = "{0} · {1} K ({2} Helligkeit)",
        [nameof(SolarEventPastText)] = "{0}: vor {1}",
        [nameof(SolarEventFutureText)] = "{0}: in {1}",
        [nameof(SolarEventNowText)] = "{0}: jetzt",
        [nameof(DurationMinuteText)] = "1 Minute",
        [nameof(DurationMinutesText)] = "{0} Minuten",
        [nameof(DurationHourText)] = "1 Stunde",
        [nameof(DurationHoursText)] = "{0} Stunden",
        [nameof(ActivePhaseCaption)] = "Aktive Phase: {0} · {1} K ({2} Helligkeit)",
        [nameof(ActiveStageTemperatureTooltip)] =
            "Ziehen, um die Farbtemperatur der aktuellen Phase anzupassen ({0})",
        [nameof(ManualScheduleText)] = "Manueller Zeitplan",
        [nameof(LocationNotSetText)] = "Standort: nicht festgelegt",
        [nameof(LocationCoordinatesText)] = "Standort: {0}",
        [nameof(OpenLocationSettingsTooltip)] = "Standorteinstellungen ändern",
        // Cycle
        [nameof(EveningStageTooltip)] =
            "Vom Sonnenuntergang bis zum Beginn der Nacht eine Abendkonfiguration als Zwischenstufe verwenden, statt bei Sonnenuntergang direkt zur Nachtkonfiguration zu wechseln",
        [nameof(EveningTemperatureTooltip)] =
            "Farbtemperatur zwischen Sonnenuntergang und Beginn der Nacht",
        [nameof(EveningBrightnessTooltip)] = """
            Helligkeit zwischen Sonnenuntergang und Beginn der Nacht

            Diese Helligkeitseinstellung wirkt auf das Farbgamma, nicht auf die tatsächliche Bildschirmhelligkeit.
            """,
        [nameof(NightStartTimeTooltip)] = """
            Zeitpunkt, zu dem der Übergang von der Abend- zur Nachtkonfiguration beginnt (wie der Sonnenuntergang um den Übergangsversatz angepasst)

            Beginnt die Nacht, bevor der Übergang bei Sonnenuntergang abgeschlossen ist, wird die Abendphase an diesem Tag übersprungen.
            """,
        // Settings page
        [nameof(BackTooltip)] = "Zurück zum Dashboard (Änderungen werden gespeichert)",
        [nameof(CycleTabName)] = "Zyklus & Farben",
        [nameof(ScheduleTabName)] = "Zeitplan & Standort",
        [nameof(RulesTabName)] = "Regeln & Whitelist",
        [nameof(ControlsTabName)] = "Steuerung & App",
        [nameof(DayCardSubtitle)] = "Sonnenaufgang bis Sonnenuntergang",
        [nameof(EveningCardSubtitle)] = "Sonnenuntergang bis Schlafenszeit",
        [nameof(NightCardSubtitle)] = "Schlafenszeit bis Sonnenaufgang",
        [nameof(TemperatureLabel)] = "Temperatur",
        [nameof(BrightnessLabel)] = "Helligkeit",
        [nameof(BedtimeNightStartLabel)] = "Schlafenszeit (Beginn der Nacht)",
        [nameof(TransitionsCardTitle)] = "Übergänge",
        [nameof(DurationLabel)] = "Dauer",
        [nameof(TransitionOffsetShortLabel)] = "Versatz",
        [nameof(LocationCardTitle)] = "Standort & Koordinaten",
        [nameof(LocationCardSubtitle)] =
            "Wird zur Berechnung von Sonnenaufgang und Sonnenuntergang verwendet. Suchen Sie einen Ort, geben Sie Koordinaten ein oder lassen Sie ihn automatisch erkennen.",
        [nameof(LocationInUseText)] = "Verwendet: {0}, {1}",
        [nameof(SolarScheduleCardTitle)] = "Sonnenzeitplan",
        [nameof(SolarScheduleCardSubtitle)] =
            "Sonnenaufgang und Sonnenuntergang anhand Ihres Standorts berechnen oder manuell festlegen.",
        [nameof(OffLabel)] = "Aus",
        // Rules & whitelist
        [nameof(AutoPauseCardTitle)] = "Intelligente Auto-Pause",
        [nameof(AutoPauseCardSubtitle)] =
            "In diesen Situationen vorübergehend die normalen Farben wiederherstellen.",
        [nameof(PauseWhenScreenLockedLabel)] = "Pausieren, wenn der Bildschirm gesperrt ist",
        [nameof(PauseWhenScreenLockedTooltip)] =
            "LightBulb pausieren, solange der Computer gesperrt ist",
        [nameof(WhitelistCardSubtitle)] =
            "LightBulb pausieren, solange eines der ausgewählten Programme im Vordergrund ist.",
        [nameof(PerMonitorFullscreenPauseLabel)] = "Nur den Monitor mit dem Vollbild pausieren",
        [nameof(PerMonitorFullscreenPauseTooltip)] =
            "Wenn eine Vollbildanwendung im Vordergrund ist, die normalen Farben nur auf dem Bildschirm wiederherstellen, auf dem sie läuft, und die anderen Bildschirme im Zyklus belassen",
        [nameof(StreamingAutoPauseLabel)] = "Beim Remote-Streaming pausieren (Sunshine / RDP)",
        [nameof(StreamingAutoPauseTooltip)] =
            "Die normalen Farben auf allen Bildschirmen wiederherstellen, solange dieser Computer mit Sunshine gestreamt oder per Remotedesktop verwendet wird",
        [nameof(StreamingPortsLabel)] = "Streaming-TCP-Ports",
        [nameof(StreamingPortsTooltip)] =
            "Remotedesktop wird automatisch auf jedem Port erkannt. Sunshine und andere Streaming-Apps werden an einer aktiven Verbindung auf einem dieser lokalen TCP-Ports erkannt, getrennt durch Kommas (z. B. 48010 für Sunshine, oder 48010, 27036, um auch Steam Remote Play einzubeziehen).",
        [nameof(DisplaysCardTitle)] = "Verbundene Bildschirme",
        [nameof(DisplaysCardSubtitle)] =
            "Bildschirme ausschließen oder ihre Wärme einzeln feinabstimmen. Bildschirme werden beim erneuten Verbinden wiedererkannt.",
        [nameof(IdentifyButton)] = "IDENTIFIZIEREN",
        [nameof(IdentifyTooltip)] = "Die Nummer jedes Bildschirms auf ihm anzeigen",
        [nameof(OffsetScalingLabel)] = "Versatzskalierung",
        [nameof(OffsetScalingTooltip)] =
            "Wie der Temperaturversatz jedes Bildschirms angewendet wird, während der Zyklus vom Tag zur Nacht übergeht",
        [nameof(PerceptualOffsetScalingOption)] = "Mired (Wahrnehmung)",
        [nameof(FixedOffsetScalingOption)] = "Kelvin (fest)",
        [nameof(PerceptualOffsetScalingDescription)] =
            "Temperaturversätze gelten exakt bei Ihrer Tagestemperatur und skalieren zum Abend und zur Nacht hin wahrnehmungsgetreu, sodass unterschiedliche Bildschirmtypen optisch aufeinander abgestimmt bleiben.",
        [nameof(FixedOffsetScalingDescription)] =
            "Temperaturversätze werden jederzeit als gleiche, feste Anzahl Kelvin angewendet, ohne Skalierung zwischen Tag und Nacht.",
        [nameof(NoDisplaysText)] = "Keine Bildschirme erkannt.",
        [nameof(PrimaryBadge)] = "PRIMÄR",
        [nameof(DisplayEnabledTooltip)] =
            "LightBulb auf diesen Bildschirm anwenden. Ein ausgeschlossener Bildschirm behält neutrale Farben.",
        [nameof(MonitorTemperatureOffsetLabel)] = "Temperaturversatz",
        [nameof(MonitorTemperatureOffsetTooltip)] =
            "Verschiebt die Farbtemperatur nur auf diesem Bildschirm. Bei wahrnehmungsgetreuer Skalierung ist dies die Verschiebung bei Ihrer Tagestemperatur.",
        [nameof(MonitorBrightnessOffsetLabel)] = "Helligkeitsversatz",
        [nameof(MonitorBrightnessOffsetTooltip)] =
            "Wird nur auf diesem Bildschirm zur aktuellen Helligkeit addiert",
        [nameof(ResetOffsetsTooltip)] = "Versätze zurücksetzen",
        [nameof(ReferenceDisplayNote)] =
            "Referenzbildschirm · Folgt dem Hauptzeitplan von „Zyklus & Farben“",
        [nameof(EffectiveNowText)] = "Aktuell wirksam: {0} K · {1}",
        [nameof(ExcludedNeutralText)] = "Ausgeschlossen · Neutral 6600 K",
        [nameof(DisconnectedDisplaysLabel)] = "Gespeicherte getrennte Bildschirme ({0})",
        [nameof(ForgetButton)] = "VERGESSEN",
        [nameof(ForgetDisplayTooltip)] =
            "Die gespeicherten Einstellungen für diesen Bildschirm entfernen",
        // Controls & app
        [nameof(HotkeysCardTitle)] = "Globale Tastenkombinationen",
        [nameof(HotkeysCardSubtitle)] =
            "Klicken Sie auf ein Feld und drücken Sie eine Tastenkombination, um sie zuzuweisen.",
        [nameof(SystemPreferencesCardTitle)] = "Systemeinstellungen",
        [nameof(StartMinimizedLabel)] = "Minimiert starten",
        [nameof(StartMinimizedTooltip)] =
            "LightBulb beim Start in den Infobereich minimieren, auch wenn es manuell gestartet wird",
        [nameof(ExtendedGammaRangeLabel)] = "Erweiterter Gamma-Bereich",
        [nameof(ExtendedGammaRangeTooltip)] =
            "Den vollen Gamma-Bereich in Windows entsperren, der wärmere Farbtemperaturen und geringere Helligkeit erlaubt. Für die Änderung sind Administratorrechte erforderlich.",
        [nameof(GammaPollingIntervalLabel)] = "Aktualisieren alle",
        [nameof(GammaPollingIntervalTooltip)] =
            "Wie oft das Gamma bei aktivierter Gamma-Abfrage aufgefrischt wird",
        [nameof(SecondsValueText)] = "{0} s",
        // Tray menu
        [nameof(TrayOpenMenuItem)] = "LightBulb öffnen",
        [nameof(TrayPauseMenuItem)] = "Pausieren",
        [nameof(TrayResumeMenuItem)] = "Fortsetzen",
        [nameof(TrayPreviewMenuItem)] = "24-Stunden-Zyklus vorschauen",
        [nameof(PauseFor15MinutesChip)] = "15 Min.",
        [nameof(PauseFor1HourChip)] = "1 Std.",
        [nameof(PauseFor3HoursChip)] = "3 Std.",
        [nameof(PausedStateLabel)] = "Pausiert",
    };
}
