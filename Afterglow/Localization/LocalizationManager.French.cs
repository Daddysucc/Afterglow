using System.Collections.Generic;

namespace Afterglow.Localization;

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
        [nameof(ToggleAfterglowTooltip)] = "Activer/désactiver Afterglow",
        [nameof(HideToTrayTooltip)] = "Réduire Afterglow dans la barre des tâches",
        [nameof(PreviewText)] = "APERÇU",
        [nameof(StopPreviewTooltip)] = "Arrêter l'aperçu",
        [nameof(StartPreviewTooltip)] = "Aperçu du cycle de 24 heures",
        [nameof(OpenSettingsTooltip)] = "Ouvrir les paramètres",
        [nameof(ResetTooltip)] =
            "Remettre les paramètres de cet onglet à leurs valeurs par défaut (les autres onglets gardent les leurs)",
        [nameof(ResetTabSettingsButton)] = "Réinitialiser les paramètres « {0} »",
        [nameof(LanguageLabel)] = "Langue",
        [nameof(LanguageTooltip)] = "Langue d'interface préférée",
        [nameof(StartWithWindowsLabel)] = "Démarrer avec Windows",
        [nameof(StartWithWindowsTooltip)] = "Lancer Afterglow au démarrage de Windows",
        [nameof(DefaultToDayConfigLabel)] = "Revenir à la configuration diurne",
        [nameof(DefaultToDayConfigTooltip)] =
            "Quand Afterglow est désactivé ou en pause, restaurer la température et la luminosité diurnes configurées au lieu du gamma par défaut du moniteur",
        [nameof(PauseWhenFullscreenLabel)] = "Pause en plein écran",
        [nameof(PauseWhenFullscreenTooltip)] =
            "Mettre Afterglow en pause quand une fenêtre plein écran est au premier plan",
        [nameof(GammaSmoothingLabel)] = "Lissage gamma",
        [nameof(GammaSmoothingTooltip)] =
            "Transition en douceur lors de l'activation ou désactivation de Afterglow pour laisser le temps aux yeux de s'adapter",
        [nameof(GammaPollingLabel)] = "Interrogation gamma",
        [nameof(GammaPollingTooltip)] =
            "Forcer le rafraîchissement du gamma du moniteur à intervalles réguliers pour empêcher d'autres programmes de le remplacer",
        [nameof(DayTemperatureTooltip)] = "Température de couleur pendant la journée",
        [nameof(NightTemperatureTooltip)] = "Température de couleur pendant la nuit",
        [nameof(DayBrightnessTooltip)] = """
            Luminosité pendant la journée

            Ce réglage de luminosité s'applique au gamma des couleurs, pas à la luminosité réelle du moniteur.
            Si votre ordinateur est déjà capable d'ajuster automatiquement la luminosité de l'écran en fonction des conditions d'éclairage (courant sur les ordinateurs portables), il est recommandé de désactiver le contrôle de luminosité de Afterglow en maintenant les deux réglages à 100%.
            """,
        [nameof(NightBrightnessTooltip)] = """
            Luminosité pendant la nuit

            Ce réglage de luminosité s'applique au gamma des couleurs, pas à la luminosité réelle du moniteur.
            Si votre ordinateur est déjà capable d'ajuster automatiquement la luminosité de l'écran en fonction des conditions d'éclairage (courant sur les ordinateurs portables), il est recommandé de désactiver le contrôle de luminosité de Afterglow en maintenant les deux réglages à 100%.
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
        [nameof(ToggleAfterglowHotkeyLabel)] = "Basculer Afterglow",
        [nameof(ToggleAfterglowHotkeyTooltip)] =
            "Raccourci global pour activer/désactiver Afterglow",
        [nameof(ToggleWindowLabel)] = "Basculer la fenêtre",
        [nameof(ToggleWindowHotkeyTooltip)] =
            "Raccourci global pour afficher/masquer la fenêtre principale de Afterglow",
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
            "Mettre Afterglow en pause quand une des applications ou un des titres de fenêtre sélectionnés est au premier plan",
        [nameof(TraySettingsMenuItem)] = "Paramètres",
        [nameof(TrayDisableUntilSunriseMenuItem)] = "Jusqu'au lever du soleil",
        [nameof(TrayExitMenuItem)] = "Quitter",
        [nameof(TrayTooltipDisabled)] = "Désactivé",
        [nameof(CloseButton)] = "FERMER",
        [nameof(UnstableBuildTitle)] = "Avertissement de build instable",
        [nameof(UnstableBuildMessage)] = """
            Vous utilisez un build de développement de {0}. Ces builds n'ont pas été soigneusement testés et peuvent contenir des bogues.

            Si vous souhaitez passer à une version stable, vous pouvez la télécharger depuis la page des versions.
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
        [nameof(AlreadyRunningTitle)] = "Déjà en cours d'exécution",
        [nameof(AlreadyRunningMessage)] =
            "Une autre copie de {0}, ou le {1} d'origine, est déjà en cours d'exécution. Fermez-la depuis son icône dans la zone de notification, puis relancez {0}.",
        [nameof(TemporaryLocationNote)] =
            "{0} s'exécute depuis un dossier temporaire (comme Téléchargements) et ne démarrera donc pas avec Windows depuis cet emplacement. Pour le conserver, utilisez le programme d'installation, ou déplacez {1} dans un dossier permanent et lancez-le depuis celui-ci.",
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
        [nameof(StatusPausedRemoteStreamingDayText)] =
            "En pause pour le streaming, configuration de jour rétablie.",
        [nameof(StatusPausedAutomaticallyDayText)] =
            "En pause automatique, configuration de jour rétablie.",
        [nameof(StatusGrayscaleText)] = "Niveaux de gris actifs à {0} K et {1} % de luminosité.",
        [nameof(StatusGrayscaleNeutralText)] = "Niveaux de gris actifs avec un point blanc neutre.",
        [nameof(StatusGrayscaleSnoozedText)] = "Niveaux de gris désactivés pendant {0}.",
        [nameof(StatusGrayscaleHeldText)] =
            "Niveaux de gris désactivés jusqu'au prochain changement d'horaire.",
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
        [nameof(ProjectPageTooltip)] = "Afterglow sur GitHub",
        [nameof(CycleTabName)] = "Cycle et couleurs",
        [nameof(ScheduleTabName)] = "Horaire et lieu",
        [nameof(RulesTabName)] = "Règles et liste blanche",
        [nameof(ControlsTabName)] = "Appli et commandes",
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
            "Mettre Afterglow en pause pendant que la session est verrouillée",
        [nameof(WhitelistCardSubtitle)] =
            "Mettre Afterglow en pause lorsqu'un programme sélectionné ou une fenêtre correspondante est au premier plan.",
        [nameof(UpdateAvailableButton)] = "Mise à jour disponible",
        [nameof(UpdateAvailableTooltip)] = "{0} {1} est disponible. Cliquez pour mettre à jour.",
        [nameof(UpdateDownloadingText)] = "Téléchargement de la mise à jour… {0}",
        [nameof(UpdateInstallingText)] = "Installation de la mise à jour…",
        [nameof(UpdateFailedButton)] = "Mettre à jour manuellement",
        [nameof(UpdateFailedTooltip)] =
            "{0} n'a pas pu se mettre à jour. Cliquez pour ouvrir la page de téléchargement.",
        [nameof(CheckForUpdatesLabel)] = "Rechercher les mises à jour",
        [nameof(CheckForUpdatesTooltip)] =
            "Rechercher de temps en temps une nouvelle version sur GitHub. Rien n'est téléchargé automatiquement.",
        [nameof(CheckForUpdatesNowTooltip)] = "Rechercher les mises à jour maintenant",
        [nameof(CheckNowButton)] = "Vérifier maintenant",
        [nameof(UpdateCheckNeverOption)] = "Jamais",
        [nameof(UpdateCheckDailyOption)] = "Tous les jours",
        [nameof(UpdateCheckWeeklyOption)] = "Toutes les semaines",
        [nameof(UpdateCheckEveryTwoWeeksOption)] = "Toutes les deux semaines",
        [nameof(CurrentVersionText)] = "Version {0}",
        [nameof(LastUpdateCheckText)] = "Dernière vérification : {0}",
        [nameof(HueCardTitle)] = "Philips Hue",
        [nameof(HueCardSubtitle)] =
            "Faire suivre le cycle de couleurs aux lampes Hue tant que ce PC est allumé et paraître aussi chaudes que l'écran.",
        [nameof(HueEnabledLabel)] = "Contrôler les lampes Hue",
        [nameof(HueEnabledTooltip)] =
            "Les lampes allumées des pièces choisies suivent le cycle de couleurs. Elles ne sont jamais allumées par Afterglow, et reviennent à leur état d'avant quand Afterglow s'arrête (y compris quand le PC s'éteint ou se met en veille). Une lampe que vous modifiez vous-même est laissée telle quelle jusqu'à ce qu'elle soit éteinte puis rallumée.",
        [nameof(HueBridgeLabel)] = "Adresse du pont",
        [nameof(HueBridgeTooltip)] =
            "L'adresse IP de votre pont Hue (indiquée dans l'application Hue, sous Paramètres, Ponts). Laissez-la vide pour trouver le pont automatiquement.",
        [nameof(HueBridgePlaceholder)] = "Rechercher automatiquement",
        [nameof(HueConnectButton)] = "Connecter",
        [nameof(HueReconnectButton)] = "Reconnecter",
        [nameof(HueDisconnectButton)] = "Déconnecter",
        [nameof(HueDisconnectTooltip)] =
            "Remettre les lampes comme avant et oublier ce pont (pour se reconnecter, il faudra appuyer à nouveau sur son bouton)",
        [nameof(HueForgetBridgeButton)] = "Oublier le pont",
        [nameof(HueForgetBridgeTooltip)] =
            "Se déconnecter, et oublier aussi l'adresse du pont ainsi que les pièces et lampes choisies, pour repartir de zéro",
        [nameof(HueCancelButton)] = "Annuler",
        [nameof(HueConnectTooltip)] =
            "Se connecter au pont : après avoir cliqué, appuyez sur le bouton du pont dans les 30 secondes",
        [nameof(HueRoomsLabel)] = "Pièces",
        [nameof(HueFollowTemperatureLabel)] = "Suivre la température",
        [nameof(HueFollowTemperatureTooltip)] =
            "Les lampes prennent la température de couleur du cycle (dans la mesure du possible, d’environ 2000 K à 6500 K)",
        [nameof(LightWarmthLabel)] = "Chaleur",
        [nameof(LightWarmthTooltip)] =
            "À quel point les lumières sont réglées plus chaudes que l'écran, pour paraître aussi chaudes que lui. Les lumières qui affichent des couleurs passent à l'ambre et à l'orange une fois à court de blancs chauds.",
        [nameof(HueFollowBrightnessLabel)] = "Suivre la luminosité",
        [nameof(HueFollowBrightnessTooltip)] =
            "Les lampes prennent la luminosité du cycle (les 100 % d'Afterglow correspondent à la pleine luminosité)",
        [nameof(HuePauseWithScreenLabel)] = "Suspendre avec l'écran",
        [nameof(HuePauseWithScreenTooltip)] =
            "Les lumières reviennent aussi à leur état précédent tant que l'écran est suspendu automatiquement (applications en plein écran, applications de la liste blanche, streaming à distance, écran verrouillé)",
        [nameof(LightDimmingLabel)] = "Atténuation",
        [nameof(LightDimmingTooltip)] =
            "À quel point les lumières baissent quand l'écran baisse. À ×1.0, elles suivent la luminosité de l'écran ; des valeurs plus élevées les baissent davantage, et la pleine luminosité reste toujours pleine.",
        [nameof(HueNotConnectedText)] =
            "Cliquez sur Connecter, puis appuyez sur le bouton de votre pont Hue.",
        [nameof(HueSearchingText)] = "Recherche du pont Hue…",
        [nameof(HueNoBridgeFoundText)] =
            "Aucun pont Hue n'a été trouvé. Saisissez son adresse IP, puis cliquez sur Connecter.",
        [nameof(HueWaitingForButtonText)] =
            "Appuyez sur le bouton situé sur le dessus du pont Hue…",
        [nameof(HueButtonNotPressedText)] =
            "Le bouton du pont n'a pas été pressé à temps. Cliquez sur Connecter pour réessayer.",
        [nameof(HueConnectedText)] = "Connecté au pont Hue.",
        [nameof(HueNoRoomsText)] =
            "Connecté. Choisissez les pièces dont les lampes suivent Afterglow.",
        [nameof(HuePausedText)] =
            "Connecté. Les lampes suivront de nouveau Afterglow dès qu'il sera réactivé.",
        [nameof(HuePausedWithScreenText)] =
            "Connecté. Les lumières sont suspendues avec l'écran et suivront de nouveau Afterglow dès sa reprise.",
        [nameof(HueFollowingText)] = "Lampes qui suivent Afterglow : {0}",
        [nameof(HueFollowingUnresponsiveText)] =
            "Lampes qui suivent Afterglow : {0} ({1} ne répondent pas, nouvel essai de temps en temps)",
        [nameof(HueShowLightsTooltip)] =
            "Afficher ou masquer les lampes des pièces choisies, pour choisir celles qui suivent Afterglow",
        [nameof(HueUnnamedLightText)] = "Lampe sans nom",
        [nameof(HueLightFollowingText)] = "Suit Afterglow",
        [nameof(HueLightNotRespondingText)] = "Ne répond pas, nouvel essai de temps en temps",
        [nameof(HueLightLeftAloneText)] =
            "Modifiée ailleurs, laissée telle quelle jusqu'à ce qu'elle soit éteinte puis rallumée",
        [nameof(HueLightOffText)] = "Éteinte",
        [nameof(HueLightExcludedText)] = "Exclue",
        [nameof(HueLightWaitingText)] = "Ne suit pas encore",
        [nameof(HueFollowAgainButton)] = "Suivre à nouveau",
        [nameof(HueFollowAgainTooltip)] = "Faire suivre à nouveau Afterglow à cette lampe",
        [nameof(HuePausedUntilText)] =
            "Les lampes sont en pause jusqu'à {0} et reviennent à leur état précédent d'ici là.",
        [nameof(HuePausedUntilResumedText)] =
            "Les lampes sont en pause jusqu'à leur reprise depuis la zone de notification, et reviennent à leur état précédent d'ici là.",
        [nameof(TrayPauseLightsMenuItem)] = "Mettre les lampes en pause",
        [nameof(TrayResumeLightsMenuItem)] = "Reprendre les lampes",
        [nameof(TrayTurnOnLightsMenuItem)] = "Allumer les lampes ({0})",
        [nameof(HueLightNotAdjustableText)] = "Non réglable (seulement allumée et éteinte)",
        [nameof(HueLightNothingFollowedText)] =
            "Rien à suivre (elle ne peut faire que ce qui n'est pas suivi)",
        [nameof(HueLightPausedText)] = "En pause",
        [nameof(HueFollowingNoneText)] =
            "Aucune lampe ne suit Afterglow pour le moment. Les lampes des pièces choisies le suivent dès qu'elles sont allumées.",
        [nameof(HueShutdownLabel)] = "À l'arrêt ou à la mise en veille du PC",
        [nameof(HueShutdownTooltip)] =
            "Ce qui arrive aux lampes qui suivent Afterglow quand ce PC s'éteint, redémarre, se déconnecte ou se met en veille. Les lampes allumées par Afterglow sont éteintes (sauf avec un réglage personnalisé), et celles que vous avez modifiées vous-même restent telles quelles.",
        [nameof(HueShutdownLeaveOption)] = "Laisser les lampes telles quelles",
        [nameof(HueShutdownRestoreOption)] = "Remettre les lampes comme avant",
        [nameof(HueShutdownTurnOffOption)] = "Éteindre les lampes",
        [nameof(HuePauseActionLabel)] = "Quand les lampes sont en pause",
        [nameof(HuePauseActionTooltip)] =
            "Ce que font les lampes qui suivent Afterglow pendant une pause : depuis la zone de notification ou avec un raccourci, pendant la pause d'Afterglow, ou avec l'écran. Les lampes éteintes pour une pause sont rallumées ensuite.",
        [nameof(HueCustomOption)] = "Personnalisé",
        [nameof(HueLookTemperatureLabel)] = "Température",
        [nameof(HueLookTemperatureTooltip)] =
            "La température de couleur des lampes elles-mêmes (les lampes de couleur vont un peu plus chaud que les lampes blanches). Maintenez le bouton de la souris enfoncé sur le curseur pour la voir sur les lampes ; elles reviennent dès que vous relâchez.",
        [nameof(HueLookBrightnessLabel)] = "Luminosité",
        [nameof(HueLookBrightnessTooltip)] =
            "La luminosité des lampes. Maintenez le bouton de la souris enfoncé sur le curseur pour la voir sur les lampes ; elles reviennent dès que vous relâchez.",
        [nameof(HueTurnOnAtStartLabel)] = "Allumer les lampes au démarrage d'Afterglow",
        [nameof(HueTurnOnAtStartTooltip)] =
            "Allume toutes les lampes des pièces choisies (sauf celles exclues) au démarrage d'Afterglow, par exemple au démarrage du PC ou à l'ouverture de session, et à la sortie de veille",
        [nameof(HueReclaimLabel)] = "Quand les lampes sont modifiées ailleurs",
        [nameof(HueReclaimTooltip)] =
            "Quand une lampe est modifiée hors d'Afterglow (dans l'app Hue, avec un interrupteur, une scène ou un assistant vocal), elle est laissée telle quelle ; voici pendant combien de temps, à compter de sa dernière modification. « Suivre à nouveau » dans la liste des lampes la fait suivre tout de suite.",
        [nameof(HueReclaimNeverOption)] =
            "Laisser telles quelles jusqu'à être éteintes puis rallumées",
        [nameof(HueLightLeftAloneUntilText)] = "Modifiée ailleurs, suivie à nouveau à {0}",
        [nameof(SmartLightsTabName)] = "Lampes connectées",
        [nameof(HueReclaimAfterOption)] = "Suivre à nouveau après une durée choisie",
        [nameof(HueReclaimAfterLabel)] = "Après",
        [nameof(MinutesValueText)] = "{0} min",
        [nameof(HoursValueText)] = "{0} h",
        [nameof(HoursMinutesValueText)] = "{0} h {1} min",
        [nameof(HueFollowingCardTitle)] = "Suivi",
        [nameof(HueFollowingCardSubtitle)] =
            "Ce que les lampes reprennent de l'écran, et à quel point elles sont plus chaudes et plus sombres que lui.",
        [nameof(HueReclaimCardTitle)] = "Modifications ailleurs",
        [nameof(HueReclaimCardSubtitle)] =
            "Les lampes modifiées dans l'app Hue, avec un interrupteur, une scène ou un assistant vocal sont laissées telles quelles un moment.",
        [nameof(HuePausingCardTitle)] = "Pause",
        [nameof(HuePausingCardSubtitle)] = "Ce que font les lampes pendant une pause.",
        [nameof(HueStartupCardTitle)] = "Démarrage et arrêt",
        [nameof(HueStartupCardSubtitle)] =
            "Ce que font les lampes au démarrage d'Afterglow, et quand le PC s'éteint ou se met en veille.",
        [nameof(HueUnreachableText)] = "Impossible de joindre le pont Hue. Nouvel essai sous peu.",
        [nameof(HueUnauthorizedText)] =
            "Le pont Hue n'accepte plus Afterglow. Cliquez sur Reconnecter, puis appuyez sur le bouton du pont.",
        [nameof(HueCertificateChangedText)] =
            "Un autre appareil que votre pont Hue a répondu à cette adresse. Si vous avez remplacé le pont, cliquez sur Reconnecter.",
        [nameof(NeverCheckedText)] = "Pas encore vérifié",
        [nameof(UpdateCheckingText)] = "Recherche de mises à jour…",
        [nameof(UpToDateText)] = "{0} {1} est la dernière version.",
        [nameof(UpdateCheckFailedText)] =
            "Impossible de rechercher les mises à jour. Vérifiez votre connexion Internet et réessayez.",
        [nameof(UpdateCheckUnavailableText)] =
            "Les versions de développement ne recherchent pas de mises à jour.",
        [nameof(WhitelistedApplicationsPauseLabel)] =
            "Pause pour les applications de la liste blanche",
        [nameof(WhitelistedApplicationsPauseTooltip)] =
            "Mettre en pause lorsque la fenêtre au premier plan appartient à une des applications sélectionnées",
        [nameof(WhitelistedWindowTitlesPauseLabel)] =
            "Pause pour les titres de fenêtre de la liste blanche",
        [nameof(WhitelistedWindowTitlesPauseTooltip)] =
            "Mettre en pause lorsque le titre de la fenêtre au premier plan contient l'un des mots-clés (sans tenir compte de la casse)",
        [nameof(WindowTitleKeywordPlaceholder)] = "Mot-clé du titre, p. ex. Netflix",
        [nameof(AddButton)] = "AJOUTER",
        [nameof(RemoveWindowTitleTooltip)] = "Supprimer le mot-clé",
        [nameof(PerMonitorWhitelistPauseLabel)] =
            "Mettre en pause uniquement l'écran avec la fenêtre correspondante",
        [nameof(PerMonitorWhitelistPauseTooltip)] =
            "Les autres écrans restent ajustés. Si désactivé, tous les écrans sont mis en pause.",
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
            "Appliquer Afterglow à cet écran. Un écran exclu conserve des couleurs neutres.",
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
            "Réduire Afterglow dans la zone de notification au démarrage, même lorsqu'il est lancé manuellement",
        [nameof(ExtendedGammaRangeLabel)] = "Plage gamma étendue",
        [nameof(ExtendedGammaRangeTooltip)] =
            "Déverrouiller toute la plage gamma de Windows, ce qui permet des températures de couleur plus chaudes et une luminosité plus faible. Cette modification nécessite des privilèges d'administrateur.",
        [nameof(GammaPollingIntervalLabel)] = "Actualiser toutes les",
        [nameof(GammaPollingIntervalTooltip)] =
            "Fréquence à laquelle le gamma est réappliqué lorsque l'interrogation gamma est activée",
        [nameof(SecondsValueText)] = "{0} s",
        // Tray menu
        [nameof(TrayOpenMenuItem)] = "Ouvrir Afterglow",
        [nameof(TrayPauseMenuItem)] = "Pause",
        [nameof(TrayResumeMenuItem)] = "Reprendre",
        [nameof(TrayPreviewMenuItem)] = "Aperçu du cycle de 24 h",
        [nameof(PauseFor15MinutesChip)] = "15 min",
        [nameof(PauseFor1HourChip)] = "1 h",
        [nameof(PauseFor3HoursChip)] = "3 h",
        [nameof(PausedStateLabel)] = "En pause",
        // Grayscale
        [nameof(TrayGrayscaleMenuItem)] = "Niveaux de gris",
        [nameof(ToggleGrayscaleHotkeyLabel)] = "Basculer les niveaux de gris",
        [nameof(ToggleGrayscaleHotkeyTooltip)] =
            "Raccourci global pour activer/désactiver le mode niveaux de gris",
        [nameof(ToggleLightsPauseHotkeyLabel)] = "Suspendre/reprendre les lampes Hue",
        [nameof(ToggleLightsPauseHotkeyTooltip)] =
            "Raccourci global pour suspendre les lampes Hue (revenues à leur état précédent) ou les reprendre",
        [nameof(GrayscaleCardTitle)] = "Niveaux de gris",
        [nameof(GrayscaleCardSubtitle)] =
            "Retirer les couleurs de l'écran, manuellement ou selon un horaire.",
        [nameof(GrayscaleScheduleLabel)] = "Niveaux de gris automatiques",
        [nameof(GrayscaleScheduleTooltip)] =
            "Quand les niveaux de gris s'activent d'eux-mêmes. Ils peuvent toujours être basculés manuellement, et un choix manuel dure jusqu'au prochain changement de l'horaire.",
        [nameof(GrayscaleScheduleOffOption)] = "Désactivé",
        [nameof(GrayscaleScheduleNightOption)] = "La nuit uniquement",
        [nameof(GrayscaleScheduleEveningAndNightOption)] = "Le soir et la nuit",
        [nameof(GrayscaleTemperatureShiftLabel)] =
            "Appliquer la température de couleur en niveaux de gris",
        [nameof(GrayscaleTemperatureShiftTooltip)] =
            "Continuer d'appliquer la température de couleur par-dessus les niveaux de gris, pour un rendu monochrome ambré et chaud. Si désactivé, les niveaux de gris utilisent un point blanc neutre, et la luminosité reste appliquée.",
        [nameof(GrayscaleScheduleOnOption)] = "Activé",
        [nameof(TrayResumeGrayscaleMenuItem)] = "Reprendre les niveaux de gris",
    };
}
