using System.Collections.Generic;

namespace Afterglow.Localization;

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
        [nameof(ToggleAfterglowTooltip)] = "Afterglow ein-/ausschalten",
        [nameof(HideToTrayTooltip)] = "Afterglow in den Infobereich minimieren",
        [nameof(PreviewText)] = "VORSCHAU",
        [nameof(StopPreviewTooltip)] = "Vorschau beenden",
        [nameof(StartPreviewTooltip)] = "24-Stunden-Zyklus vorschauen",
        [nameof(OpenSettingsTooltip)] = "Einstellungen öffnen",
        [nameof(ResetTooltip)] =
            "Die Einstellungen auf diesem Tab auf die Standardwerte zurücksetzen (die anderen Tabs behalten ihre)",
        [nameof(ResetTabSettingsButton)] = "Einstellungen „{0}“ zurücksetzen",
        [nameof(LanguageLabel)] = "Sprache",
        [nameof(LanguageTooltip)] = "Bevorzugte Sprache der Benutzeroberfläche",
        [nameof(StartWithWindowsLabel)] = "Mit Windows starten",
        [nameof(StartWithWindowsTooltip)] = "Afterglow beim Windows-Start automatisch starten",
        [nameof(DefaultToDayConfigLabel)] = "Zur Tageskonfiguration zurückkehren",
        [nameof(DefaultToDayConfigTooltip)] =
            "Wenn Afterglow deaktiviert oder pausiert ist, die konfigurierte Tagestemperatur und -helligkeit anstelle des Standard-Monitor-Gammas wiederherstellen",
        [nameof(PauseWhenFullscreenLabel)] = "Bei Vollbild pausieren",
        [nameof(PauseWhenFullscreenTooltip)] =
            "Afterglow pausieren, wenn ein Vollbildfenster im Vordergrund ist",
        [nameof(GammaSmoothingLabel)] = "Gamma-Glättung",
        [nameof(GammaSmoothingTooltip)] =
            "Beim Ein- oder Ausschalten von Afterglow langsam übergehen, um den Augen Zeit zur Anpassung zu geben",
        [nameof(GammaPollingLabel)] = "Gamma-Abfrage",
        [nameof(GammaPollingTooltip)] =
            "Monitor-Gamma in regelmäßigen Abständen auffrischen, um andere Programme daran zu hindern, es zu überschreiben",
        [nameof(DayTemperatureTooltip)] = "Farbtemperatur tagsüber",
        [nameof(NightTemperatureTooltip)] = "Farbtemperatur nachts",
        [nameof(DayBrightnessTooltip)] = """
            Helligkeit tagsüber

            Diese Helligkeitseinstellung wirkt auf das Farbgamma, nicht auf die tatsächliche Bildschirmhelligkeit.
            Wenn Ihr Computer bereits in der Lage ist, die Bildschirmhelligkeit automatisch anzupassen (häufig bei Laptops), wird empfohlen, die Helligkeitssteuerung von Afterglow zu deaktivieren, indem beide Helligkeitswerte auf 100% gesetzt werden.
            """,
        [nameof(NightBrightnessTooltip)] = """
            Helligkeit nachts

            Diese Helligkeitseinstellung wirkt auf das Farbgamma, nicht auf die tatsächliche Bildschirmhelligkeit.
            Wenn Ihr Computer bereits in der Lage ist, die Bildschirmhelligkeit automatisch anzupassen (häufig bei Laptops), wird empfohlen, die Helligkeitssteuerung von Afterglow zu deaktivieren, indem beide Helligkeitswerte auf 100% gesetzt werden.
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
        [nameof(ToggleAfterglowHotkeyLabel)] = "Afterglow umschalten",
        [nameof(ToggleAfterglowHotkeyTooltip)] =
            "Globale Tastenkombination zum Ein-/Ausschalten von Afterglow",
        [nameof(ToggleWindowLabel)] = "Fenster umschalten",
        [nameof(ToggleWindowHotkeyTooltip)] =
            "Globale Tastenkombination zum Anzeigen/Ausblenden des Afterglow-Fensters",
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
            "Afterglow pausieren, wenn eine der ausgewählten Anwendungen oder Fenstertitel im Vordergrund ist",
        [nameof(TraySettingsMenuItem)] = "Einstellungen",
        [nameof(TrayDisableUntilSunriseMenuItem)] = "Bis Sonnenaufgang",
        [nameof(TrayExitMenuItem)] = "Beenden",
        [nameof(TrayTooltipDisabled)] = "Deaktiviert",
        [nameof(CloseButton)] = "SCHLIESSEN",
        [nameof(UnstableBuildTitle)] = "Warnung: Instabiler Build",
        [nameof(UnstableBuildMessage)] = """
            Sie verwenden einen Entwicklungs-Build von {0}. Diese Builds wurden nicht gründlich getestet und können Fehler enthalten.

            Wenn Sie zu einem stabilen Release wechseln möchten, können Sie ihn auf der Release-Seite herunterladen.
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
        [nameof(AlreadyRunningTitle)] = "Bereits gestartet",
        [nameof(AlreadyRunningMessage)] =
            "Eine andere Kopie von {0} oder das ursprüngliche {1} läuft bereits. Beenden Sie sie über ihr Symbol im Infobereich und starten Sie {0} dann erneut.",
        [nameof(TemporaryLocationNote)] =
            "{0} wird aus einem temporären Ordner (z. B. Downloads) ausgeführt und startet von dort nicht mit Windows. Um es zu behalten, verwenden Sie das Installationsprogramm oder verschieben Sie {1} in einen dauerhaften Ordner und starten es von dort.",
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
        [nameof(StatusPausedRemoteStreamingDayText)] =
            "Pausiert wegen Streaming, Tageskonfiguration wiederhergestellt.",
        [nameof(StatusPausedAutomaticallyDayText)] =
            "Automatisch pausiert, Tageskonfiguration wiederhergestellt.",
        [nameof(StatusGrayscaleText)] = "Graustufen aktiv mit {0} K und {1} % Helligkeit.",
        [nameof(StatusGrayscaleNeutralText)] = "Graustufen aktiv mit neutralem Weißpunkt.",
        [nameof(StatusGrayscaleSnoozedText)] = "Graustufen vorübergehend deaktiviert für {0}.",
        [nameof(StatusGrayscaleHeldText)] = "Graustufen deaktiviert bis zur nächsten Planänderung.",
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
        [nameof(ProjectPageTooltip)] = "Afterglow auf GitHub",
        [nameof(CycleTabName)] = "Zyklus & Farben",
        [nameof(ScheduleTabName)] = "Zeitplan & Standort",
        [nameof(RulesTabName)] = "Regeln & Whitelist",
        [nameof(ControlsTabName)] = "App & Steuerung",
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
            "Afterglow pausieren, solange der Computer gesperrt ist",
        [nameof(WhitelistCardSubtitle)] =
            "Afterglow pausieren, solange ein ausgewähltes Programm oder ein passendes Fenster im Vordergrund ist.",
        [nameof(UpdateAvailableButton)] = "Update verfügbar",
        [nameof(UpdateAvailableTooltip)] = "{0} {1} ist verfügbar. Klicken, um zu aktualisieren.",
        [nameof(UpdateDownloadingText)] = "Update wird heruntergeladen … {0}",
        [nameof(UpdateInstallingText)] = "Update wird installiert …",
        [nameof(UpdateFailedButton)] = "Manuell aktualisieren",
        [nameof(UpdateFailedTooltip)] =
            "{0} konnte sich nicht selbst aktualisieren. Klicken, um stattdessen die Downloadseite zu öffnen.",
        [nameof(CheckForUpdatesLabel)] = "Nach Updates suchen",
        [nameof(CheckForUpdatesTooltip)] =
            "Ab und zu auf GitHub nach einer neueren Version suchen. Es wird nichts automatisch heruntergeladen.",
        [nameof(CheckForUpdatesNowTooltip)] = "Jetzt nach Updates suchen",
        [nameof(CheckNowButton)] = "Jetzt prüfen",
        [nameof(UpdateCheckNeverOption)] = "Nie",
        [nameof(UpdateCheckDailyOption)] = "Täglich",
        [nameof(UpdateCheckWeeklyOption)] = "Wöchentlich",
        [nameof(UpdateCheckEveryTwoWeeksOption)] = "Alle zwei Wochen",
        [nameof(CurrentVersionText)] = "Version {0}",
        [nameof(LastUpdateCheckText)] = "Zuletzt geprüft: {0}",
        [nameof(HueCardTitle)] = "Philips Hue",
        [nameof(HueCardSubtitle)] =
            "Hue-Lampen dem Farbzyklus folgen lassen, solange dieser PC an ist, und so warm wirken wie der Bildschirm.",
        [nameof(HueEnabledLabel)] = "Hue-Lampen steuern",
        [nameof(HueEnabledTooltip)] =
            "Eingeschaltete Lampen in den gewählten Räumen folgen dem Farbzyklus. Lampen werden nie eingeschaltet und kehren in ihren vorherigen Zustand zurück, wenn Afterglow aufhört (auch wenn der PC herunterfährt oder in den Energiesparmodus wechselt). Eine Lampe, die Sie selbst ändern, bleibt unverändert, bis sie aus- und wieder eingeschaltet wird.",
        [nameof(HueBridgeLabel)] = "Bridge-Adresse",
        [nameof(HueBridgeTooltip)] =
            "Die IP-Adresse Ihrer Hue Bridge (in der Hue-App unter Einstellungen, Bridges). Leer lassen, um die Bridge automatisch zu finden.",
        [nameof(HueBridgePlaceholder)] = "Automatisch finden",
        [nameof(HueConnectButton)] = "Verbinden",
        [nameof(HueReconnectButton)] = "Neu verbinden",
        [nameof(HueDisconnectButton)] = "Trennen",
        [nameof(HueDisconnectTooltip)] =
            "Lampen zurücksetzen und diese Bridge vergessen (zum erneuten Verbinden muss ihre Taste wieder gedrückt werden)",
        [nameof(HueForgetBridgeButton)] = "Bridge vergessen",
        [nameof(HueForgetBridgeTooltip)] =
            "Trennen und auch die Adresse der Bridge sowie die gewählten Räume und Lampen vergessen, um neu anzufangen",
        [nameof(HueCancelButton)] = "Abbrechen",
        [nameof(HueConnectTooltip)] =
            "Mit der Bridge verbinden: Drücken Sie nach dem Klicken innerhalb von 30 Sekunden die Taste oben auf der Bridge",
        [nameof(HueRoomsLabel)] = "Räume",
        [nameof(HueFollowTemperatureLabel)] = "Farbtemperatur folgen",
        [nameof(HueFollowTemperatureTooltip)] =
            "Lampen übernehmen die Farbtemperatur des Zyklus (soweit möglich, etwa 2000 K bis 6500 K)",
        [nameof(LightWarmthLabel)] = "Wärme",
        [nameof(LightWarmthTooltip)] =
            "Wie viel wärmer die Lampen als der Bildschirm eingestellt werden, damit sie so warm wirken wie er. Farbfähige Lampen wechseln zu Bernstein und Orange, wenn ihre warmen Weißtöne nicht mehr reichen.",
        [nameof(HueFollowBrightnessLabel)] = "Helligkeit folgen",
        [nameof(HueFollowBrightnessTooltip)] =
            "Lampen übernehmen die Helligkeit des Zyklus (100 % in Afterglow ist volle Helligkeit)",
        [nameof(HuePauseWithScreenLabel)] = "Mit dem Bildschirm pausieren",
        [nameof(HuePauseWithScreenTooltip)] =
            "Lampen kehren auch in ihren vorherigen Zustand zurück, solange der Bildschirm automatisch pausiert ist (Vollbildanwendungen, Anwendungen auf der Whitelist, Remote-Streaming, gesperrter Bildschirm)",
        [nameof(LightDimmingLabel)] = "Dimmen",
        [nameof(LightDimmingTooltip)] =
            "Wie stark die Lampen gedimmt werden, wenn der Bildschirm dunkler wird. Bei ×1.0 folgen sie der Helligkeit des Bildschirms; höhere Werte dimmen sie stärker, volle Helligkeit bleibt immer voll.",
        [nameof(HueNotConnectedText)] =
            "Klicken Sie auf Verbinden und drücken Sie dann die Taste auf Ihrer Hue Bridge.",
        [nameof(HueSearchingText)] = "Hue Bridge wird gesucht …",
        [nameof(HueNoBridgeFoundText)] =
            "Es wurde keine Hue Bridge gefunden. Geben Sie ihre IP-Adresse ein und klicken Sie dann auf Verbinden.",
        [nameof(HueWaitingForButtonText)] = "Drücken Sie die Taste oben auf Ihrer Hue Bridge …",
        [nameof(HueButtonNotPressedText)] =
            "Die Taste der Bridge wurde nicht rechtzeitig gedrückt. Klicken Sie auf Verbinden, um es erneut zu versuchen.",
        [nameof(HueConnectedText)] = "Mit der Hue Bridge verbunden.",
        [nameof(HueNoRoomsText)] =
            "Verbunden. Wählen Sie die Räume, deren Lampen Afterglow folgen.",
        [nameof(HuePausedText)] =
            "Verbunden. Die Lampen folgen Afterglow wieder, sobald es wieder eingeschaltet ist.",
        [nameof(HuePausedWithScreenText)] =
            "Verbunden. Die Lampen pausieren mit dem Bildschirm und folgen Afterglow wieder, sobald es weiterläuft.",
        [nameof(HueFollowingText)] = "Lampen, die Afterglow folgen: {0}",
        [nameof(HueFollowingUnresponsiveText)] =
            "Lampen, die Afterglow folgen: {0} ({1} reagieren nicht und werden ab und zu erneut versucht)",
        [nameof(HueShowLightsTooltip)] =
            "Lampen der gewählten Räume ein- oder ausblenden, um zu wählen, welche Afterglow folgen",
        [nameof(HueUnnamedLightText)] = "Unbenannte Lampe",
        [nameof(HueLightFollowingText)] = "Folgt",
        [nameof(HueLightNotRespondingText)] = "Reagiert nicht, wird ab und zu erneut versucht",
        [nameof(HueLightLeftAloneText)] =
            "Anderswo geändert, bleibt unverändert, bis sie aus- und wieder eingeschaltet wird",
        [nameof(HueLightOffText)] = "Aus",
        [nameof(HueLightExcludedText)] = "Ausgenommen",
        [nameof(HueLightWaitingText)] = "Folgt noch nicht",
        [nameof(HueFollowAgainButton)] = "Wieder folgen",
        [nameof(HueFollowAgainTooltip)] = "Diese Lampe wieder Afterglow folgen lassen",
        [nameof(HuePausedUntilText)] =
            "Die Lampen pausieren bis {0} und sind bis dahin wieder wie vorher.",
        [nameof(HuePausedUntilResumedText)] =
            "Die Lampen pausieren, bis sie über den Infobereich fortgesetzt werden, und sind bis dahin wieder wie vorher.",
        [nameof(TrayPauseLightsMenuItem)] = "Lampen pausieren",
        [nameof(TrayResumeLightsMenuItem)] = "Lampen fortsetzen",
        [nameof(TrayTurnOnLightsMenuItem)] = "Lampen einschalten ({0})",
        [nameof(HueLightNotAdjustableText)] = "Nicht einstellbar (nur ein- und ausschaltbar)",
        [nameof(HueLightNothingFollowedText)] =
            "Nichts zu folgen (sie kann nur, was nicht gefolgt wird)",
        [nameof(HueLightPausedText)] = "Pausiert",
        [nameof(HueFollowingNoneText)] =
            "Gerade folgen keine Lampen. Lampen in den gewählten Räumen folgen Afterglow, sobald sie eingeschaltet werden.",
        [nameof(HueShutdownLabel)] = "Wenn der PC herunterfährt oder in den Ruhezustand wechselt",
        [nameof(HueShutdownTooltip)] =
            "Was mit den Lampen passiert, die Afterglow folgen, wenn dieser PC herunterfährt, neu startet, sich abmeldet oder in den Ruhezustand wechselt. Von Afterglow eingeschaltete Lampen werden ausgeschaltet (außer bei einer benutzerdefinierten Einstellung), und selbst geänderte Lampen bleiben, wie sie sind.",
        [nameof(HueShutdownLeaveOption)] = "Lampen lassen, wie sie sind",
        [nameof(HueShutdownRestoreOption)] = "Lampen wie vorher wiederherstellen",
        [nameof(HueShutdownTurnOffOption)] = "Lampen ausschalten",
        [nameof(HuePauseActionLabel)] = "Wenn die Lampen pausieren",
        [nameof(HuePauseActionTooltip)] =
            "Was die Lampen, die Afterglow folgen, während einer Pause tun: über den Infobereich oder ein Tastenkürzel, während Afterglow pausiert oder zusammen mit dem Bildschirm. Für eine Pause ausgeschaltete Lampen werden danach wieder eingeschaltet.",
        [nameof(HueCustomOption)] = "Benutzerdefiniert",
        [nameof(HueLookTemperatureLabel)] = "Farbtemperatur",
        [nameof(HueLookTemperatureTooltip)] =
            "Die Farbtemperatur der Lampen selbst (farbfähige Lampen gehen etwas wärmer als weiße). Halten Sie die Maustaste auf dem Schieberegler gedrückt, um sie an den Lampen zu sehen; sie kehren zurück, sobald Sie loslassen.",
        [nameof(HueLookBrightnessLabel)] = "Helligkeit",
        [nameof(HueLookBrightnessTooltip)] =
            "Wie hell die Lampen sind. Halten Sie die Maustaste auf dem Schieberegler gedrückt, um es an den Lampen zu sehen; sie kehren zurück, sobald Sie loslassen.",
        [nameof(HueTurnOnAtStartLabel)] = "Lampen beim Start von Afterglow einschalten",
        [nameof(HueTurnOnAtStartTooltip)] =
            "Schaltet alle Lampen in den gewählten Räumen (außer ausgenommenen) ein, wenn Afterglow startet, etwa beim Hochfahren oder Anmelden, und wenn der PC aus dem Ruhezustand aufwacht",
        [nameof(HueReclaimLabel)] = "Wenn Lampen anderswo geändert werden",
        [nameof(HueReclaimTooltip)] =
            "Wird eine Lampe außerhalb von Afterglow geändert (in der Hue-App, mit einem Schalter, einer Szene oder einem Sprachassistenten), bleibt sie unverändert; so lange, ab ihrer letzten Änderung gerechnet. „Wieder folgen“ in der Lampenliste lässt sie sofort wieder folgen.",
        [nameof(HueReclaimNeverOption)] =
            "Unverändert lassen, bis sie aus- und wieder eingeschaltet werden",
        [nameof(HueLightLeftAloneUntilText)] = "Anderswo geändert, folgt wieder um {0}",
        [nameof(SmartLightsTabName)] = "Smarte Lampen",
        [nameof(HueReclaimAfterOption)] = "Nach einer festen Zeit wieder folgen",
        [nameof(HueReclaimAfterLabel)] = "Nach",
        [nameof(MinutesValueText)] = "{0} Min.",
        [nameof(HoursValueText)] = "{0} Std.",
        [nameof(HoursMinutesValueText)] = "{0} Std. {1} Min.",
        [nameof(HueFollowingCardTitle)] = "Folgen",
        [nameof(HueFollowingCardSubtitle)] =
            "Was die Lampen vom Bildschirm übernehmen, und wie warm und gedimmt sie daneben sind.",
        [nameof(HueReclaimCardTitle)] = "Anderswo geändert",
        [nameof(HueReclaimCardSubtitle)] =
            "In der Hue-App, mit einem Schalter, einer Szene oder einem Sprachassistenten geänderte Lampen bleiben eine Weile unverändert.",
        [nameof(HuePausingCardTitle)] = "Pausieren",
        [nameof(HuePausingCardSubtitle)] = "Was die Lampen während einer Pause tun.",
        [nameof(HueStartupCardTitle)] = "Start und Herunterfahren",
        [nameof(HueStartupCardSubtitle)] =
            "Was die Lampen tun, wenn Afterglow startet und wenn der PC herunterfährt oder in den Ruhezustand wechselt.",
        [nameof(HueUnreachableText)] =
            "Die Hue Bridge ist nicht erreichbar. Neuer Versuch in Kürze.",
        [nameof(HueUnauthorizedText)] =
            "Die Hue Bridge akzeptiert Afterglow nicht mehr. Klicken Sie auf Neu verbinden und drücken Sie dann die Taste der Bridge.",
        [nameof(HueCertificateChangedText)] =
            "Unter dieser Adresse hat ein anderes Gerät als Ihre Hue Bridge geantwortet. Wenn Sie die Bridge ersetzt haben, klicken Sie auf Neu verbinden.",
        [nameof(NeverCheckedText)] = "Noch nicht geprüft",
        [nameof(UpdateCheckingText)] = "Suche nach Updates…",
        [nameof(UpToDateText)] = "{0} {1} ist die neueste Version.",
        [nameof(UpdateCheckFailedText)] =
            "Die Suche nach Updates ist fehlgeschlagen. Prüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.",
        [nameof(UpdateCheckUnavailableText)] = "Entwicklungsversionen suchen nicht nach Updates.",
        [nameof(WhitelistedApplicationsPauseLabel)] = "Für Anwendungen auf der Whitelist pausieren",
        [nameof(WhitelistedApplicationsPauseTooltip)] =
            "Pausieren, solange das Fenster im Vordergrund zu einer der ausgewählten Anwendungen gehört",
        [nameof(WhitelistedWindowTitlesPauseLabel)] =
            "Für Fenstertitel auf der Whitelist pausieren",
        [nameof(WhitelistedWindowTitlesPauseTooltip)] =
            "Pausieren, solange der Titel des Fensters im Vordergrund eines der Stichwörter enthält (Groß-/Kleinschreibung egal)",
        [nameof(WindowTitleKeywordPlaceholder)] = "Stichwort im Titel, z. B. Netflix",
        [nameof(AddButton)] = "HINZUFÜGEN",
        [nameof(RemoveWindowTitleTooltip)] = "Stichwort entfernen",
        [nameof(PerMonitorWhitelistPauseLabel)] =
            "Nur den Monitor mit dem passenden Fenster pausieren",
        [nameof(PerMonitorWhitelistPauseTooltip)] =
            "Die anderen Monitore bleiben angepasst. Wenn aus, werden alle Monitore pausiert.",
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
            "Afterglow auf diesen Bildschirm anwenden. Ein ausgeschlossener Bildschirm behält neutrale Farben.",
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
            "Afterglow beim Start in den Infobereich minimieren, auch wenn es manuell gestartet wird",
        [nameof(ExtendedGammaRangeLabel)] = "Erweiterter Gamma-Bereich",
        [nameof(ExtendedGammaRangeTooltip)] =
            "Den vollen Gamma-Bereich in Windows entsperren, der wärmere Farbtemperaturen und geringere Helligkeit erlaubt. Für die Änderung sind Administratorrechte erforderlich.",
        [nameof(GammaPollingIntervalLabel)] = "Aktualisieren alle",
        [nameof(GammaPollingIntervalTooltip)] =
            "Wie oft das Gamma bei aktivierter Gamma-Abfrage aufgefrischt wird",
        [nameof(SecondsValueText)] = "{0} s",
        // Tray menu
        [nameof(TrayOpenMenuItem)] = "Afterglow öffnen",
        [nameof(TrayPauseMenuItem)] = "Pausieren",
        [nameof(TrayResumeMenuItem)] = "Fortsetzen",
        [nameof(TrayPreviewMenuItem)] = "24-Stunden-Zyklus vorschauen",
        [nameof(PauseFor15MinutesChip)] = "15 Min.",
        [nameof(PauseFor1HourChip)] = "1 Std.",
        [nameof(PauseFor3HoursChip)] = "3 Std.",
        [nameof(PausedStateLabel)] = "Pausiert",
        // Grayscale
        [nameof(TrayGrayscaleMenuItem)] = "Graustufen",
        [nameof(ToggleGrayscaleHotkeyLabel)] = "Graustufen umschalten",
        [nameof(ToggleGrayscaleHotkeyTooltip)] =
            "Globale Tastenkombination zum Ein-/Ausschalten des Graustufenmodus",
        [nameof(ToggleLightsPauseHotkeyLabel)] = "Hue-Lampen pausieren/fortsetzen",
        [nameof(ToggleLightsPauseHotkeyTooltip)] =
            "Globales Tastenkürzel, um die Hue-Lampen zu pausieren (wieder wie vorher) oder fortzusetzen",
        [nameof(GrayscaleCardTitle)] = "Graustufen",
        [nameof(GrayscaleCardSubtitle)] =
            "Farben vom Bildschirm entfernen, manuell oder nach Zeitplan.",
        [nameof(GrayscaleScheduleLabel)] = "Automatische Graustufen",
        [nameof(GrayscaleScheduleTooltip)] =
            "Wann sich die Graustufen von selbst einschalten. Sie lassen sich jederzeit manuell umschalten, und eine manuelle Wahl gilt bis zur nächsten Änderung durch den Zeitplan.",
        [nameof(GrayscaleScheduleOffOption)] = "Aus",
        [nameof(GrayscaleScheduleNightOption)] = "Nur nachts",
        [nameof(GrayscaleScheduleEveningAndNightOption)] = "Abends & nachts",
        [nameof(GrayscaleTemperatureShiftLabel)] = "Farbtemperatur im Graustufenmodus anwenden",
        [nameof(GrayscaleTemperatureShiftTooltip)] =
            "Die Farbtemperatur weiterhin über den Graustufen anwenden, für einen warmen, bernsteinfarbenen Monochrom-Look. Wenn aus, verwenden die Graustufen einen neutralen Weißpunkt, und die Helligkeit gilt weiterhin.",
        [nameof(GrayscaleScheduleOnOption)] = "Ein",
        [nameof(TrayResumeGrayscaleMenuItem)] = "Graustufen fortsetzen",
    };
}
