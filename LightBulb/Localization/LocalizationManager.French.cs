using System.Collections.Generic;

namespace LightBulb.Localization;

public partial class LocalizationManager
{
    private static readonly IReadOnlyDictionary<string, string> FrenchLocalization = new Dictionary<
        string,
        string
    >
    {
        [nameof(SunsetLabel)] = "Coucher",
        [nameof(SunriseLabel)] = "Lever",
        [nameof(OffsetTooltipHeader)] =
            "Les valeurs actuelles de température et de luminosité sont ajustées par un décalage :",
        [nameof(TemperatureOffsetLabel)] = "Décalage de température :",
        [nameof(BrightnessOffsetLabel)] = "Décalage de luminosité :",
        [nameof(ClickToResetLabel)] = "Cliquez pour réinitialiser",
        [nameof(OffsetLabel)] = "décalage",
        [nameof(ToggleLightBulbTooltip)] = "Activer/désactiver LightBulb",
        [nameof(HideToTrayTooltip)] = "Réduire LightBulb dans la barre des tâches",
        [nameof(PreviewText)] = "APERÇU",
        [nameof(StopPreviewTooltip)] = "Arrêter l'aperçu",
        [nameof(StartPreviewTooltip)] = "Aperçu du cycle de 24 heures",
        [nameof(OpenSettingsTooltip)] = "Ouvrir les paramètres",
        [nameof(ResetTooltip)] = "Réinitialiser tous les paramètres à leurs valeurs par défaut",
        [nameof(LanguageLabel)] = "Langue",
        [nameof(LanguageTooltip)] = "Langue d'interface préférée",
        [nameof(StartWithWindowsLabel)] = "Démarrer avec Windows",
        [nameof(StartWithWindowsTooltip)] = "Lancer LightBulb au démarrage de Windows",
        [nameof(DefaultToDayConfigLabel)] = "Revenir à la configuration diurne",
        [nameof(DefaultToDayConfigTooltip)] =
            "Quand LightBulb est désactivé ou en pause, restaurer la température et la luminosité diurnes configurées au lieu du gamma par défaut du moniteur",
        [nameof(PauseWhenFullscreenLabel)] = "Pause en plein écran",
        [nameof(PauseWhenFullscreenTooltip)] =
            "Mettre LightBulb en pause quand une fenêtre plein écran est au premier plan",
        [nameof(GammaSmoothingLabel)] = "Lissage gamma",
        [nameof(GammaSmoothingTooltip)] =
            "Transition en douceur lors de l'activation ou désactivation de LightBulb pour laisser le temps aux yeux de s'adapter",
        [nameof(GammaPollingLabel)] = "Interrogation gamma",
        [nameof(GammaPollingTooltip)] =
            "Forcer le rafraîchissement du gamma du moniteur à intervalles réguliers pour empêcher d'autres programmes de le remplacer",
        [nameof(DayTemperatureTooltip)] = "Température de couleur pendant la journée",
        [nameof(NightTemperatureTooltip)] = "Température de couleur pendant la nuit",
        [nameof(DayBrightnessTooltip)] = """
            Luminosité pendant la journée

            Ce réglage de luminosité s'applique au gamma des couleurs, pas à la luminosité réelle du moniteur.
            Si votre ordinateur est déjà capable d'ajuster automatiquement la luminosité de l'écran en fonction des conditions d'éclairage (courant sur les ordinateurs portables), il est recommandé de désactiver le contrôle de luminosité de LightBulb en maintenant les deux réglages à 100%.
            """,
        [nameof(NightBrightnessTooltip)] = """
            Luminosité pendant la nuit

            Ce réglage de luminosité s'applique au gamma des couleurs, pas à la luminosité réelle du moniteur.
            Si votre ordinateur est déjà capable d'ajuster automatiquement la luminosité de l'écran en fonction des conditions d'éclairage (courant sur les ordinateurs portables), il est recommandé de désactiver le contrôle de luminosité de LightBulb en maintenant les deux réglages à 100%.
            """,
        [nameof(TransitionDurationTooltip)] =
            "Durée nécessaire pour passer de la configuration diurne à nocturne",
        [nameof(TransitionOffsetTooltip)] =
            "Décalage qui spécifie l'avance ou le retard du début de la transition par rapport au lever et coucher de soleil",
        [nameof(ManualLabel)] = "Manuel",
        [nameof(ManualTooltip)] = "Configurer manuellement le lever et coucher de soleil",
        [nameof(LocationBasedLabel)] = "Basé sur la localisation",
        [nameof(LocationBasedTooltip)] =
            "Configurer votre emplacement pour calculer automatiquement les heures de lever et coucher de soleil",
        [nameof(YourLocationLabel)] = "Votre emplacement :",
        [nameof(AutoDetectLocationTooltip)] =
            "Essayer de détecter automatiquement l'emplacement via votre adresse IP",
        [nameof(LocationQueryTooltip)] = """
            Spécifiez votre emplacement avec des coordonnées géographiques ou une requête de recherche

            Exemples d'entrées valides :
            **41.25, -120.9762**
            **41.25°N, 120.9762°W**
            **New York, USA**
            **Germany**
            """,
        [nameof(SetLocationTooltip)] = "Définir l'emplacement",
        [nameof(LocationErrorText)] = "Erreur lors de la résolution de l'emplacement, réessayez",
        [nameof(ToggleLightBulbHotkeyLabel)] = "Basculer LightBulb",
        [nameof(ToggleLightBulbHotkeyTooltip)] =
            "Raccourci global pour activer/désactiver LightBulb",
        [nameof(ToggleWindowLabel)] = "Basculer la fenêtre",
        [nameof(ToggleWindowHotkeyTooltip)] =
            "Raccourci global pour afficher/masquer la fenêtre principale de LightBulb",
        [nameof(IncreaseTemperatureOffsetLabel)] = "Décalage de température ↑",
        [nameof(IncreaseTemperatureOffsetTooltip)] =
            "Raccourci global pour augmenter le décalage de température actuel",
        [nameof(DecreaseTemperatureOffsetLabel)] = "Décalage de température ↓",
        [nameof(DecreaseTemperatureOffsetTooltip)] =
            "Raccourci global pour diminuer le décalage de température actuel",
        [nameof(IncreaseBrightnessOffsetLabel)] = "Décalage de luminosité ↑",
        [nameof(IncreaseBrightnessOffsetTooltip)] =
            "Raccourci global pour augmenter le décalage de luminosité actuel",
        [nameof(DecreaseBrightnessOffsetLabel)] = "Décalage de luminosité ↓",
        [nameof(DecreaseBrightnessOffsetTooltip)] =
            "Raccourci global pour diminuer le décalage de luminosité actuel",
        [nameof(ResetOffsetLabel)] = "Réinitialiser le décalage",
        [nameof(ResetOffsetHotkeyTooltip)] =
            "Raccourci global pour réinitialiser les décalages de température et de luminosité actuels",
        [nameof(AppWhitelistLabel)] = "Liste blanche d'applications",
        [nameof(RefreshAppsTooltip)] = "Actualiser les applications en cours d'exécution",
        [nameof(PauseForWhitelistedTooltip)] =
            "Mettre LightBulb en pause quand une des applications sélectionnées est au premier plan",
        [nameof(TraySettingsMenuItem)] = "Paramètres",
        [nameof(TrayDisableUntilSunriseMenuItem)] = "Jusqu'au lever du soleil",
        [nameof(TrayExitMenuItem)] = "Quitter",
        [nameof(TrayTooltipDisabled)] = "Désactivé",
        [nameof(CloseButton)] = "FERMER",
        [nameof(UnstableBuildTitle)] = "Avertissement de build instable",
        [nameof(UnstableBuildMessage)] = """
            Vous utilisez un build de développement de {0}. Ces builds n'ont pas été soigneusement testés et peuvent contenir des bogues.

            Les mises à jour automatiques sont désactivées pour les builds de développement. Si vous souhaitez passer à une version stable, veuillez la télécharger manuellement.
            """,
        [nameof(SeeReleasesButton)] = "VOIR LES VERSIONS",
        [nameof(LimitedGammaRangeTitle)] = "Plage gamma limitée",
        [nameof(LimitedGammaRangeMessage)] = """
            {0} a détecté que les contrôles de plage gamma étendue ne sont pas activés sur ce système.
            Cela peut entraîner un dysfonctionnement de certaines configurations de couleurs.

            Appuyez sur CORRIGER pour déverrouiller la plage gamma. Des droits d'administrateur peuvent être requis.
            """,
        [nameof(FixButton)] = "CORRIGER",
        [nameof(WelcomeTitle)] = "Bienvenue !",
        [nameof(WelcomeMessage)] = """
            Merci d'avoir installé {0} !
            Pour une expérience plus personnalisée, veuillez définir votre configuration solaire préférée.

            Appuyez sur OK pour ouvrir les paramètres.
            """,
        [nameof(OkButton)] = "OK",
        // Dashboard
        [nameof(BedtimeLabel)] = "Heure du coucher",
        [nameof(DayStateLabel)] = "Jour",
        [nameof(EveningStateLabel)] = "Soirée",
        [nameof(NightStateLabel)] = "Nuit",
        [nameof(SunriseTransitionPhaseLabel)] = "Transition du lever",
        [nameof(SunsetTransitionPhaseLabel)] = "Transition du coucher",
        [nameof(NightTransitionPhaseLabel)] = "Transition vers la nuit",
        [nameof(StatusDayScheduleText)] = "Programme de jour actif à {0} K et {1} % de luminosité.",
        [nameof(StatusEveningScheduleText)] =
            "Programme du soir actif à {0} K et {1} % de luminosité.",
        [nameof(StatusNightScheduleText)] =
            "Programme de nuit actif à {0} K et {1} % de luminosité.",
        [nameof(StatusWarmingText)] = "Réchauffement à {0} K {2}.",
        [nameof(StatusCoolingText)] = "Refroidissement à {0} K {2}.",
        [nameof(StatusDimmingText)] = "Luminosité réduite à {1} % {2}.",
        [nameof(StatusBrighteningText)] = "Luminosité augmentée à {1} % {2}.",
        [nameof(StatusWarmingDimmingText)] =
            "Réchauffement à {0} K, luminosité réduite à {1} % {2}.",
        [nameof(StatusWarmingBrighteningText)] =
            "Réchauffement à {0} K, luminosité augmentée à {1} % {2}.",
        [nameof(StatusCoolingDimmingText)] =
            "Refroidissement à {0} K, luminosité réduite à {1} % {2}.",
        [nameof(StatusCoolingBrighteningText)] =
            "Refroidissement à {0} K, luminosité augmentée à {1} % {2}.",
        [nameof(StatusForDayText)] = "pour le jour",
        [nameof(StatusForEveningText)] = "pour le soir",
        [nameof(StatusForNightText)] = "pour la nuit",
        [nameof(StatusManualOverrideText)] = "Réglage manuel actif à {0} K et {1} % de luminosité.",
        [nameof(StatusPausedRemoteStreamingText)] =
            "En pause pour le streaming à distance, couleurs d'origine rétablies.",
        [nameof(StatusPausedFullScreenText)] =
            "En pause pendant qu'une application est en plein écran.",
        [nameof(StatusPausedScreenLockedText)] = "En pause tant que l'écran est verrouillé.",
        [nameof(StatusPausedManuallyText)] = "En pause manuelle, programme automatique suspendu.",
        [nameof(StatusPausedAutomaticallyText)] =
            "En pause automatique, couleurs d'origine rétablies.",
        [nameof(StatusPreviewText)] = "Aperçu des 24 prochaines heures…",
        [nameof(PausedRemoteStreamingLabel)] = "En pause (streaming à distance)",
        [nameof(PausedScreenLockedLabel)] = "En pause (écran verrouillé)",
        [nameof(PausedFullScreenLabel)] = "En pause (application en plein écran)",
        [nameof(PausedWhitelistLabel)] = "En pause (application sur liste blanche)",
        [nameof(PausedCaption)] = "{0} · {1} K (luminosité {2})",
        [nameof(SolarEventPastText)] = "{0} : il y a {1}",
        [nameof(SolarEventFutureText)] = "{0} : dans {1}",
        [nameof(SolarEventNowText)] = "{0} : maintenant",
        [nameof(DurationMinuteText)] = "1 minute",
        [nameof(DurationMinutesText)] = "{0} minutes",
        [nameof(DurationHourText)] = "1 heure",
        [nameof(DurationHoursText)] = "{0} heures",
        [nameof(ActivePhaseCaption)] = "Phase active : {0} · {1} K (luminosité {2})",
        [nameof(ActiveStageTemperatureTooltip)] =
            "Faites glisser pour régler la température de couleur de l'étape actuelle ({0})",
        [nameof(ManualScheduleText)] = "Horaire manuel",
        [nameof(LocationNotSetText)] = "Emplacement : non défini",
        [nameof(LocationCoordinatesText)] = "Emplacement : {0}",
        [nameof(OpenLocationSettingsTooltip)] = "Modifier les paramètres d'emplacement",
        // Cycle
        [nameof(EveningStageTooltip)] =
            "Utiliser une configuration intermédiaire de soirée du coucher du soleil jusqu'au début de la nuit, au lieu de passer directement à la configuration de nuit au coucher du soleil",
        [nameof(EveningTemperatureTooltip)] =
            "Température de couleur entre le coucher du soleil et le début de la nuit",
        [nameof(EveningBrightnessTooltip)] = """
            Luminosité entre le coucher du soleil et le début de la nuit

            Notez que ce réglage de luminosité s'applique au gamma des couleurs, et non à la luminosité réelle du moniteur.
            """,
        [nameof(NightStartTimeTooltip)] = """
            Heure à laquelle commence la transition de la configuration de soirée vers la configuration de nuit (ajustée par le décalage de transition, comme le coucher du soleil)

            Si la nuit commence avant la fin de la transition du coucher du soleil, l'étape de soirée est ignorée ce jour-là.
            """,
        // Settings page
        [nameof(BackTooltip)] = "Retour au tableau de bord (les modifications sont enregistrées)",
        [nameof(CycleTabName)] = "Cycle et couleurs",
        [nameof(ScheduleTabName)] = "Horaire et lieu",
        [nameof(RulesTabName)] = "Règles et liste blanche",
        [nameof(ControlsTabName)] = "Commandes et appli",
        [nameof(DayCardSubtitle)] = "Du lever au coucher du soleil",
        [nameof(EveningCardSubtitle)] = "Du coucher du soleil à l'heure du coucher",
        [nameof(NightCardSubtitle)] = "De l'heure du coucher au lever du soleil",
        [nameof(TemperatureLabel)] = "Température",
        [nameof(BrightnessLabel)] = "Luminosité",
        [nameof(BedtimeNightStartLabel)] = "Heure du coucher (début de la nuit)",
        [nameof(TransitionsCardTitle)] = "Transitions",
        [nameof(DurationLabel)] = "Durée",
        [nameof(TransitionOffsetShortLabel)] = "Décalage",
        [nameof(LocationCardTitle)] = "Emplacement et coordonnées",
        [nameof(LocationCardSubtitle)] =
            "Sert à calculer le lever et le coucher du soleil. Recherchez un lieu, saisissez des coordonnées ou détectez-le automatiquement.",
        [nameof(LocationInUseText)] = "Utilise {0}, {1}",
        [nameof(SolarScheduleCardTitle)] = "Horaire solaire",
        [nameof(SolarScheduleCardSubtitle)] =
            "Calculer le lever et le coucher du soleil à partir de votre emplacement, ou les définir manuellement.",
        [nameof(OffLabel)] = "Désactivé",
        // Rules & whitelist
        [nameof(AutoPauseCardTitle)] = "Pause automatique intelligente",
        [nameof(AutoPauseCardSubtitle)] =
            "Rétablir temporairement les couleurs normales dans ces situations.",
        [nameof(PauseWhenScreenLockedLabel)] = "Pause lorsque l'écran est verrouillé",
        [nameof(PauseWhenScreenLockedTooltip)] =
            "Mettre LightBulb en pause pendant que la session est verrouillée",
        [nameof(WhitelistCardSubtitle)] =
            "Mettre LightBulb en pause lorsque l'un des programmes sélectionnés est au premier plan.",
        [nameof(PerMonitorFullscreenPauseLabel)] =
            "Mettre en pause uniquement l'écran en plein écran",
        [nameof(PerMonitorFullscreenPauseTooltip)] =
            "Lorsqu'une application en plein écran est au premier plan, rétablir les couleurs normales uniquement sur l'écran qui l'affiche, et laisser les autres écrans suivre le cycle",
        [nameof(StreamingAutoPauseLabel)] =
            "Pause pendant le streaming à distance (Sunshine / RDP)",
        [nameof(StreamingAutoPauseTooltip)] =
            "Rétablir les couleurs normales sur tous les écrans pendant que cet ordinateur est diffusé avec Sunshine ou utilisé via le Bureau à distance",
        [nameof(StreamingPortsLabel)] = "Ports TCP de streaming",
        [nameof(StreamingPortsTooltip)] =
            "Le Bureau à distance est détecté automatiquement, sur n'importe quel port. Sunshine et les autres applications de streaming sont détectés par une connexion active sur l'un de ces ports TCP locaux, séparés par des virgules (par ex. 48010 pour Sunshine, ou 48010, 27036 pour inclure aussi Steam Remote Play).",
        [nameof(DisplaysCardTitle)] = "Écrans connectés",
        [nameof(DisplaysCardSubtitle)] =
            "Excluez des écrans ou ajustez leur chaleur individuellement. Les écrans sont reconnus lorsqu'ils sont reconnectés.",
        [nameof(IdentifyButton)] = "IDENTIFIER",
        [nameof(IdentifyTooltip)] = "Afficher le numéro de chaque écran sur celui-ci",
        [nameof(OffsetScalingLabel)] = "Mise à l'échelle du décalage",
        [nameof(OffsetScalingTooltip)] =
            "Manière dont le décalage de température de chaque écran est appliqué lorsque le cycle passe du jour à la nuit",
        [nameof(PerceptualOffsetScalingOption)] = "Mireds (perceptuel)",
        [nameof(FixedOffsetScalingOption)] = "Kelvin (fixe)",
        [nameof(PerceptualOffsetScalingDescription)] =
            "Les décalages de température sont exacts à votre température de jour et s'ajustent de manière perceptuelle vers le soir et la nuit, afin que des écrans de types différents restent visuellement assortis.",
        [nameof(FixedOffsetScalingDescription)] =
            "Les décalages de température sont appliqués comme un même nombre fixe de kelvins en permanence, sans ajustement entre le jour et la nuit.",
        [nameof(NoDisplaysText)] = "Aucun écran détecté.",
        [nameof(PrimaryBadge)] = "PRINCIPAL",
        [nameof(DisplayEnabledTooltip)] =
            "Appliquer LightBulb à cet écran. Un écran exclu conserve des couleurs neutres.",
        [nameof(MonitorTemperatureOffsetLabel)] = "Décalage de température",
        [nameof(MonitorTemperatureOffsetTooltip)] =
            "Décale la température de couleur sur cet écran uniquement. Avec la mise à l'échelle perceptuelle, il s'agit du décalage à votre température de jour.",
        [nameof(MonitorBrightnessOffsetLabel)] = "Décalage de luminosité",
        [nameof(MonitorBrightnessOffsetTooltip)] =
            "Ajouté à la luminosité actuelle sur cet écran uniquement",
        [nameof(ResetOffsetsTooltip)] = "Réinitialiser les décalages",
        [nameof(ReferenceDisplayNote)] =
            "Écran de référence · Suit l'horaire principal de « Cycle et couleurs »",
        [nameof(EffectiveNowText)] = "Actuellement : {0} K · {1}",
        [nameof(ExcludedNeutralText)] = "Exclu · Neutre 6600 K",
        [nameof(DisconnectedDisplaysLabel)] = "Écrans déconnectés enregistrés ({0})",
        [nameof(ForgetButton)] = "OUBLIER",
        [nameof(ForgetDisplayTooltip)] = "Supprimer les préférences enregistrées pour cet écran",
        // Controls & app
        [nameof(HotkeysCardTitle)] = "Raccourcis globaux",
        [nameof(HotkeysCardSubtitle)] =
            "Cliquez sur un champ et appuyez sur une combinaison de touches pour l'attribuer.",
        [nameof(SystemPreferencesCardTitle)] = "Préférences système",
        [nameof(StartMinimizedLabel)] = "Démarrer réduit",
        [nameof(StartMinimizedTooltip)] =
            "Réduire LightBulb dans la zone de notification au démarrage, même lorsqu'il est lancé manuellement",
        [nameof(ExtendedGammaRangeLabel)] = "Plage gamma étendue",
        [nameof(ExtendedGammaRangeTooltip)] =
            "Déverrouiller toute la plage gamma de Windows, ce qui permet des températures de couleur plus chaudes et une luminosité plus faible. Cette modification nécessite des privilèges d'administrateur.",
        [nameof(GammaPollingIntervalLabel)] = "Actualiser toutes les",
        [nameof(GammaPollingIntervalTooltip)] =
            "Fréquence à laquelle le gamma est réappliqué lorsque l'interrogation gamma est activée",
        [nameof(SecondsValueText)] = "{0} s",
        // Tray menu
        [nameof(TrayOpenMenuItem)] = "Ouvrir LightBulb",
        [nameof(TrayPauseMenuItem)] = "Pause",
        [nameof(TrayResumeMenuItem)] = "Reprendre",
        [nameof(TrayPreviewMenuItem)] = "Aperçu du cycle de 24 h",
        [nameof(PauseFor15MinutesChip)] = "15 min",
        [nameof(PauseFor1HourChip)] = "1 h",
        [nameof(PauseFor3HoursChip)] = "3 h",
        [nameof(PausedStateLabel)] = "En pause",
    };
}
