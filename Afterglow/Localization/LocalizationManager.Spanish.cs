using System.Collections.Generic;

namespace Afterglow.Localization;

public partial class LocalizationManager
{
    private static readonly IReadOnlyDictionary<string, string> SpanishLocalization =
        new Dictionary<string, string>
        {
            [nameof(SunsetLabel)] = "Puesta",
            [nameof(SunriseLabel)] = "Amanecer",
            [nameof(OffsetTooltipHeader)] =
                "Los valores actuales de temperatura y brillo están ajustados por un desplazamiento:",
            [nameof(TemperatureOffsetLabel)] = "Desplazamiento de temperatura:",
            [nameof(BrightnessOffsetLabel)] = "Desplazamiento de brillo:",
            [nameof(ClickToResetLabel)] = "Clic para restablecer",
            [nameof(OffsetLabel)] = "desplazamiento",
            [nameof(ToggleAfterglowTooltip)] = "Activar/desactivar Afterglow",
            [nameof(HideToTrayTooltip)] = "Minimizar Afterglow en la bandeja del sistema",
            [nameof(PreviewText)] = "VISTA PREVIA",
            [nameof(StopPreviewTooltip)] = "Detener vista previa",
            [nameof(StartPreviewTooltip)] = "Vista previa del ciclo de 24 horas",
            [nameof(OpenSettingsTooltip)] = "Abrir ajustes",
            [nameof(ResetTooltip)] =
                "Restablecer los ajustes de esta pestaña a sus valores predeterminados (las demás pestañas conservan los suyos)",
            [nameof(ResetTabSettingsButton)] = "Restablecer ajustes de «{0}»",
            [nameof(LanguageLabel)] = "Idioma",
            [nameof(LanguageTooltip)] = "Idioma de interfaz preferido",
            [nameof(StartWithWindowsLabel)] = "Iniciar con Windows",
            [nameof(StartWithWindowsTooltip)] = "Iniciar Afterglow al arrancar Windows",
            [nameof(DefaultToDayConfigLabel)] = "Colores de día al estar apagado o en pausa",
            [nameof(DefaultToDayConfigTooltip)] =
                "Cuando Afterglow está desactivado o en pausa, restaurar la temperatura y el brillo diurnos configurados en lugar del gamma predeterminado del monitor",
            [nameof(PauseWhenFullscreenLabel)] = "Pausar en pantalla completa",
            [nameof(PauseWhenFullscreenTooltip)] =
                "Pausar Afterglow cuando alguna ventana a pantalla completa esté en primer plano",
            [nameof(GammaSmoothingLabel)] = "Suavizado de gamma",
            [nameof(GammaSmoothingTooltip)] =
                "Transicionar lentamente al activar o desactivar Afterglow para dar tiempo a los ojos a adaptarse",
            [nameof(GammaPollingLabel)] = "Sondeo de gamma",
            [nameof(GammaPollingTooltip)] =
                "Actualizar forzosamente el gamma del monitor a intervalos regulares para evitar que otros programas lo sobreescriban",
            [nameof(DayTemperatureTooltip)] = "Temperatura de color durante el día",
            [nameof(NightTemperatureTooltip)] = "Temperatura de color durante la noche",
            [nameof(DayBrightnessTooltip)] = """
                Brillo durante el día

                Este ajuste de brillo se aplica al gamma de colores, no al brillo real del monitor.
                Si su ordenador ya puede ajustar automáticamente el brillo de la pantalla según las condiciones de iluminación (común en portátiles), se recomienda deshabilitar el control de brillo de Afterglow manteniendo ambos ajustes de brillo al 100%.
                """,
            [nameof(NightBrightnessTooltip)] = """
                Brillo durante la noche

                Este ajuste de brillo se aplica al gamma de colores, no al brillo real del monitor.
                Si su ordenador ya puede ajustar automáticamente el brillo de la pantalla según las condiciones de iluminación (común en portátiles), se recomienda deshabilitar el control de brillo de Afterglow manteniendo ambos ajustes de brillo al 100%.
                """,
            [nameof(TransitionDurationTooltip)] =
                "Duración del tiempo que tarda en cambiar entre las configuraciones diurna y nocturna",
            [nameof(TransitionOffsetTooltip)] =
                "Desplazamiento que especifica cuán temprano o tarde comienza la transición, relativo al amanecer y al atardecer",
            [nameof(ManualLabel)] = "Manual",
            [nameof(ManualTooltip)] = "Configurar el amanecer y el atardecer manualmente",
            [nameof(LocationBasedLabel)] = "Basado en ubicación",
            [nameof(LocationBasedTooltip)] =
                "Configurar tu ubicación para calcular automáticamente las horas de amanecer y atardecer",
            [nameof(YourLocationLabel)] = "Tu ubicación:",
            [nameof(AutoDetectLocationTooltip)] =
                "Intentar detectar la ubicación automáticamente según tu dirección IP",
            [nameof(LocationQueryTooltip)] = """
                Especifica tu ubicación con coordenadas geográficas o una consulta de búsqueda

                Ejemplos de entradas válidas:
                **41.25, -120.9762**
                **41.25°N, 120.9762°W**
                **New York, USA**
                **Germany**
                """,
            [nameof(SetLocationTooltip)] = "Establecer ubicación",
            [nameof(LocationErrorText)] = "Error al resolver la ubicación, inténtalo de nuevo",
            [nameof(ToggleAfterglowHotkeyLabel)] = "Alternar Afterglow",
            [nameof(ToggleAfterglowHotkeyTooltip)] =
                "Atajo global para activar/desactivar Afterglow",
            [nameof(ToggleWindowLabel)] = "Alternar ventana",
            [nameof(ToggleWindowHotkeyTooltip)] =
                "Atajo global para mostrar/ocultar la ventana principal de Afterglow",
            [nameof(IncreaseTemperatureOffsetLabel)] = "Desplazamiento de temperatura ↑",
            [nameof(IncreaseTemperatureOffsetTooltip)] =
                "Atajo global para aumentar el desplazamiento de temperatura actual",
            [nameof(DecreaseTemperatureOffsetLabel)] = "Desplazamiento de temperatura ↓",
            [nameof(DecreaseTemperatureOffsetTooltip)] =
                "Atajo global para disminuir el desplazamiento de temperatura actual",
            [nameof(IncreaseBrightnessOffsetLabel)] = "Desplazamiento de brillo ↑",
            [nameof(IncreaseBrightnessOffsetTooltip)] =
                "Atajo global para aumentar el desplazamiento de brillo actual",
            [nameof(DecreaseBrightnessOffsetLabel)] = "Desplazamiento de brillo ↓",
            [nameof(DecreaseBrightnessOffsetTooltip)] =
                "Atajo global para disminuir el desplazamiento de brillo actual",
            [nameof(ResetOffsetLabel)] = "Restablecer desplazamiento",
            [nameof(ResetOffsetHotkeyTooltip)] =
                "Atajo global para restablecer los desplazamientos de temperatura y brillo actuales",
            [nameof(AppWhitelistLabel)] = "Lista blanca de aplicaciones",
            [nameof(RefreshAppsTooltip)] = "Actualizar aplicaciones en ejecución",
            [nameof(PauseForWhitelistedTooltip)] =
                "Pausar Afterglow cuando una de las aplicaciones o títulos de ventana seleccionados esté en primer plano",
            [nameof(TraySettingsMenuItem)] = "Ajustes",
            [nameof(TrayDisableUntilSunriseMenuItem)] = "Hasta el amanecer",
            [nameof(TrayExitMenuItem)] = "Salir",
            [nameof(TrayTooltipDisabled)] = "Desactivado",
            [nameof(CloseButton)] = "CERRAR",
            [nameof(UnstableBuildTitle)] = "Advertencia de compilación inestable",
            [nameof(UnstableBuildMessage)] = """
                Estás usando una compilación de desarrollo de {0}. Estas compilaciones no han sido probadas exhaustivamente y pueden contener errores.

                Si quieres cambiar a una versión estable, puedes descargarla desde la página de versiones.
                """,
            [nameof(SeeReleasesButton)] = "VER VERSIONES",
            [nameof(LimitedGammaRangeTitle)] = "Rango gamma limitado",
            [nameof(LimitedGammaRangeMessage)] = """
                {0} ha detectado que los controles de rango gamma extendido no están habilitados en este sistema.
                Esto puede hacer que algunas configuraciones de color no funcionen correctamente.

                Presiona CORREGIR para desbloquear el rango gamma. Pueden requerirse privilegios de administrador.
                """,
            [nameof(FixButton)] = "CORREGIR",
            [nameof(WelcomeTitle)] = "¡Bienvenido!",
            [nameof(WelcomeMessage)] = """
                ¡Gracias por instalar {0}!
                Para una experiencia más personalizada, establece tu configuración solar preferida.

                Presiona OK para abrir los ajustes.
                """,
            [nameof(OkButton)] = "OK",
            [nameof(AlreadyRunningTitle)] = "Ya se está ejecutando",
            [nameof(AlreadyRunningMessage)] =
                "Otra copia de {0}, o el {1} original, ya se está ejecutando. Ciérrala desde su icono en la bandeja del sistema y vuelve a iniciar {0}.",
            [nameof(TemporaryLocationNote)] =
                "{0} se está ejecutando desde una carpeta temporal (como Descargas), así que no se iniciará con Windows desde ahí. Para conservarlo, usa el instalador, o mueve {1} a una carpeta permanente y ejecútalo desde allí.",
            // Dashboard
            [nameof(BedtimeLabel)] = "Hora de dormir",
            [nameof(DayStateLabel)] = "Día",
            [nameof(EveningStateLabel)] = "Tarde",
            [nameof(NightStateLabel)] = "Noche",
            [nameof(SunriseTransitionPhaseLabel)] = "Transición del amanecer",
            [nameof(SunsetTransitionPhaseLabel)] = "Transición del atardecer",
            [nameof(NightTransitionPhaseLabel)] = "Transición a la noche",
            [nameof(StatusDayScheduleText)] = "Horario de día activo a {0} K y {1} % de brillo.",
            [nameof(StatusEveningScheduleText)] =
                "Horario de tarde activo a {0} K y {1} % de brillo.",
            [nameof(StatusNightScheduleText)] =
                "Horario nocturno activo a {0} K y {1} % de brillo.",
            [nameof(StatusWarmingText)] = "Calentando a {0} K {2}.",
            [nameof(StatusCoolingText)] = "Enfriando a {0} K {2}.",
            [nameof(StatusDimmingText)] = "Bajando el brillo a {1} % {2}.",
            [nameof(StatusBrighteningText)] = "Subiendo el brillo a {1} % {2}.",
            [nameof(StatusWarmingDimmingText)] =
                "Calentando a {0} K y bajando el brillo a {1} % {2}.",
            [nameof(StatusWarmingBrighteningText)] =
                "Calentando a {0} K y subiendo el brillo a {1} % {2}.",
            [nameof(StatusCoolingDimmingText)] =
                "Enfriando a {0} K y bajando el brillo a {1} % {2}.",
            [nameof(StatusCoolingBrighteningText)] =
                "Enfriando a {0} K y subiendo el brillo a {1} % {2}.",
            [nameof(StatusForDayText)] = "para el día",
            [nameof(StatusForEveningText)] = "para la tarde",
            [nameof(StatusForNightText)] = "para la noche",
            [nameof(StatusManualOverrideText)] = "Ajuste manual activo a {0} K y {1} % de brillo.",
            [nameof(StatusPausedRemoteStreamingText)] =
                "En pausa por transmisión remota, con el color nativo restaurado.",
            [nameof(StatusPausedFullScreenText)] =
                "En pausa mientras una aplicación está en pantalla completa.",
            [nameof(StatusPausedScreenLockedText)] =
                "En pausa mientras la pantalla está bloqueada.",
            [nameof(StatusPausedManuallyText)] =
                "En pausa manual, con el horario automático suspendido.",
            [nameof(StatusPausedAutomaticallyText)] =
                "En pausa automática, con el color nativo restaurado.",
            [nameof(StatusPausedRemoteStreamingDayText)] =
                "En pausa por transmisión, con la configuración diurna restaurada.",
            [nameof(StatusPausedAutomaticallyDayText)] =
                "En pausa automática, con la configuración diurna restaurada.",
            [nameof(StatusGrayscaleText)] = "Escala de grises activa a {0} K y {1} % de brillo.",
            [nameof(StatusGrayscaleNeutralText)] =
                "Escala de grises activa con punto blanco neutro.",
            [nameof(StatusGrayscaleSnoozedText)] = "Escala de grises desactivada durante {0}.",
            [nameof(StatusGrayscaleHeldText)] =
                "Escala de grises desactivada hasta el próximo cambio de horario.",
            [nameof(StatusPreviewText)] = "Vista previa de las próximas 24 horas…",
            [nameof(PausedRemoteStreamingLabel)] = "En pausa (transmisión remota)",
            [nameof(PausedScreenLockedLabel)] = "En pausa (pantalla bloqueada)",
            [nameof(PausedFullScreenLabel)] = "En pausa (aplicación en pantalla completa)",
            [nameof(PausedWhitelistLabel)] = "En pausa (aplicación de la lista blanca)",
            [nameof(PausedCaption)] = "{0} · {1} K (brillo {2})",
            [nameof(SolarEventPastText)] = "{0}: hace {1}",
            [nameof(SolarEventFutureText)] = "{0}: en {1}",
            [nameof(SolarEventNowText)] = "{0}: ahora",
            [nameof(DurationMinuteText)] = "1 minuto",
            [nameof(DurationMinutesText)] = "{0} minutos",
            [nameof(DurationHourText)] = "1 hora",
            [nameof(DurationHoursText)] = "{0} horas",
            [nameof(ActivePhaseCaption)] = "Fase activa: {0} · {1} K (brillo {2})",
            [nameof(ActiveStageTemperatureTooltip)] =
                "Arrastra para ajustar la temperatura de color de la etapa actual ({0})",
            [nameof(ManualScheduleText)] = "Horario manual",
            [nameof(LocationNotSetText)] = "Ubicación: sin definir",
            [nameof(LocationCoordinatesText)] = "Ubicación: {0}",
            [nameof(OpenLocationSettingsTooltip)] = "Cambiar los ajustes de ubicación",
            // Cycle
            [nameof(EveningStageTooltip)] =
                "Usar una configuración intermedia de tarde desde la puesta de sol hasta el inicio de la noche, en lugar de cambiar directamente a la configuración nocturna al ponerse el sol",
            [nameof(EveningTemperatureTooltip)] =
                "Temperatura de color entre la puesta de sol y el inicio de la noche",
            [nameof(EveningBrightnessTooltip)] = """
                Brillo entre la puesta de sol y el inicio de la noche

                Ten en cuenta que este ajuste de brillo se aplica a la gamma de color, no al brillo real del monitor.
                """,
            [nameof(NightStartTimeTooltip)] = """
                Hora en la que comienza la transición de la configuración de tarde a la nocturna (ajustada por el desplazamiento de transición, igual que la puesta de sol)

                Si la noche empieza antes de que termine la transición de la puesta de sol, la etapa de tarde se omite ese día.
                """,
            // Settings page
            [nameof(BackTooltip)] = "Volver al panel (los cambios se guardan)",
            [nameof(ProjectPageTooltip)] = "Afterglow en GitHub",
            [nameof(CycleTabName)] = "Ciclo y colores",
            [nameof(ScheduleTabName)] = "Horario y ubicación",
            [nameof(RulesTabName)] = "Pausas",
            [nameof(ControlsTabName)] = "Aplicación y controles",
            [nameof(DayCardSubtitle)] = "Del amanecer a la puesta de sol",
            [nameof(EveningCardSubtitle)] = "De la puesta de sol a la hora de dormir",
            [nameof(NightCardSubtitle)] = "De la hora de dormir al amanecer",
            [nameof(TemperatureLabel)] = "Temperatura",
            [nameof(BrightnessLabel)] = "Brillo",
            [nameof(BedtimeNightStartLabel)] = "Hora de dormir (inicio de la noche)",
            [nameof(TransitionsCardTitle)] = "Transiciones",
            [nameof(DurationLabel)] = "Duración",
            [nameof(TransitionOffsetShortLabel)] = "Desplazamiento",
            [nameof(LocationCardTitle)] = "Ubicación y coordenadas",
            [nameof(LocationCardSubtitle)] =
                "Se usa para calcular el amanecer y la puesta de sol. Busca un lugar, introduce coordenadas o detéctalo automáticamente.",
            [nameof(LocationInUseText)] = "Usando {0}, {1}",
            [nameof(SolarScheduleCardTitle)] = "Horario solar",
            [nameof(SolarScheduleCardSubtitle)] =
                "Calcula el amanecer y la puesta de sol a partir de tu ubicación, o establécelos manualmente.",
            [nameof(OffLabel)] = "Desactivado",
            // Rules & whitelist
            [nameof(AutoPauseCardTitle)] = "Pausa automática inteligente",
            [nameof(AutoPauseCardSubtitle)] =
                "Restablecer temporalmente los colores normales en estas situaciones.",
            [nameof(PauseWhenScreenLockedLabel)] = "Pausar cuando la pantalla esté bloqueada",
            [nameof(PauseWhenScreenLockedTooltip)] =
                "Pausar Afterglow mientras el equipo esté bloqueado",
            [nameof(WhitelistCardSubtitle)] =
                "Pausar Afterglow mientras un programa seleccionado o una ventana coincidente esté en primer plano.",
            [nameof(UpdateAvailableButton)] = "Actualización disponible",
            [nameof(UpdateAvailableTooltip)] = "{0} {1} está disponible. Haz clic para actualizar.",
            [nameof(UpdateAvailableTitle)] = "{0} {1} está disponible",
            [nameof(UpdateNotificationText)] = "Haz clic para ver las novedades y actualizar.",
            [nameof(UpdateNoNotesText)] = "Esta versión no indica qué ha cambiado.",
            [nameof(UpdateChangesHeading)] = "Novedades de la {0}",
            [nameof(UpdateNowButton)] = "Actualizar",
            [nameof(LaterButton)] = "Más tarde",
            [nameof(UpdateDownloadingText)] = "Descargando actualización… {0}",
            [nameof(UpdateInstallingText)] = "Instalando actualización…",
            [nameof(UpdateFailedButton)] = "Actualizar manualmente",
            [nameof(UpdateFailedTooltip)] =
                "{0} no pudo actualizarse. Haz clic para abrir la página de descarga.",
            [nameof(CheckForUpdatesLabel)] = "Buscar actualizaciones",
            [nameof(CheckForUpdatesTooltip)] =
                "Buscar de vez en cuando una versión más reciente en GitHub. No se descarga nada automáticamente.",
            [nameof(CheckForUpdatesNowTooltip)] = "Buscar actualizaciones ahora",
            [nameof(CheckNowButton)] = "Comprobar ahora",
            [nameof(UpdateCheckNeverOption)] = "Nunca",
            [nameof(UpdateCheckDailyOption)] = "Cada día",
            [nameof(UpdateCheckWeeklyOption)] = "Cada semana",
            [nameof(UpdateCheckEveryTwoWeeksOption)] = "Cada dos semanas",
            [nameof(CurrentVersionText)] = "Versión {0}",
            [nameof(LastUpdateCheckText)] = "Última comprobación: {0}",
            [nameof(HueCardTitle)] = "Philips Hue",
            [nameof(HueCardSubtitle)] =
                "Haz que las luces Hue sigan el ciclo de color mientras este PC está encendido y se vean tan cálidas como la pantalla.",
            [nameof(HueEnabledLabel)] = "Controlar las luces Hue",
            [nameof(HueEnabledTooltip)] =
                "Las luces encendidas de las habitaciones elegidas siguen el ciclo de color. Nunca se encienden solas, y vuelven a como estaban cuando Afterglow se detiene (también cuando el PC se apaga o entra en suspensión). Una luz que cambies tú se deja como está hasta que se apague y se vuelva a encender.",
            [nameof(HueBridgeLabel)] = "Dirección del puente",
            [nameof(HueBridgeTooltip)] =
                "La dirección IP de tu puente Hue (aparece en la app Hue, en Ajustes, Puentes). Déjala vacía para encontrar el puente automáticamente.",
            [nameof(HueBridgePlaceholder)] = "Buscar automáticamente",
            [nameof(HueConnectButton)] = "Conectar",
            [nameof(HueReconnectButton)] = "Volver a conectar",
            [nameof(HueDisconnectButton)] = "Desconectar",
            [nameof(HueDisconnectTooltip)] =
                "Devolver las luces a como estaban y olvidar este puente (para volver a conectarse hay que pulsar de nuevo su botón)",
            [nameof(HueForgetBridgeButton)] = "Olvidar puente",
            [nameof(HueForgetBridgeTooltip)] =
                "Desconectar y olvidar también la dirección del puente y las habitaciones y luces elegidas, para empezar de nuevo",
            [nameof(HueCancelButton)] = "Cancelar",
            [nameof(HueConnectTooltip)] =
                "Conectar con el puente: después de hacer clic, pulsa el botón de la parte superior del puente antes de 30 segundos",
            [nameof(HueRoomsLabel)] = "Habitaciones",
            [nameof(HueFollowTemperatureLabel)] = "Seguir la temperatura",
            [nameof(HueFollowTemperatureTooltip)] =
                "Las luces adoptan la temperatura de color del ciclo (hasta donde pueden, de unos 2000 K a 6500 K)",
            [nameof(LightWarmthLabel)] = "Calidez",
            [nameof(LightWarmthTooltip)] =
                "Cuánto más cálidas que la pantalla se ajustan las luces, para que parezcan tan cálidas como ella. Las luces que muestran colores pasan a ámbar y naranja cuando se quedan sin blancos cálidos.",
            [nameof(HueFollowBrightnessLabel)] = "Seguir el brillo",
            [nameof(HueFollowBrightnessTooltip)] =
                "Las luces adoptan el brillo del ciclo (el 100 % de Afterglow es el brillo máximo)",
            [nameof(HuePauseWithScreenLabel)] = "Pausar con la pantalla",
            [nameof(HuePauseWithScreenTooltip)] =
                "Las luces también vuelven a su estado anterior mientras la pantalla está en pausa automáticamente (aplicaciones a pantalla completa, aplicaciones de la lista blanca, transmisión remota, pantalla bloqueada)",
            [nameof(LightDimmingLabel)] = "Atenuación",
            [nameof(LightDimmingTooltip)] =
                "Cuánto se atenúan las luces cuando la pantalla se atenúa. Con ×2.2 emiten tanta luz como la pantalla (una pantalla atenuada y cálida emite mucha menos luz de lo que sugiere su brillo); los valores más bajos las mantienen más brillantes y los más altos las atenúan más. El brillo máximo de una pantalla sin tinte siempre se mantiene.",
            [nameof(DynamicWallpaperCardTitle)] = "Fondo de pantalla dinámico",
            [nameof(DynamicWallpaperCardSubtitle)] =
                "Un cielo que sigue al sol a lo largo del día, teñido por la pantalla como todo lo demás.",
            [nameof(DynamicWallpaperEnabledLabel)] = "Dibujar el fondo de pantalla",
            [nameof(DynamicWallpaperEnabledTooltip)] =
                "Reemplaza el fondo de pantalla en cada pantalla (las pantallas verticales reciben una imagen vertical). Tu propio fondo de pantalla vuelve cuando se desactiva (una presentación o Contenido destacado de Windows vuelve como su imagen actual: vuelve a activarlo en Configuración › Personalización › Fondo).",
            [nameof(AccentColorMatchedLabel)] = "Ajustar el color de énfasis",
            [nameof(AccentColorMatchedTooltip)] =
                "Ajusta el color de énfasis de Windows al del cielo: azul de día, dorado al amanecer, al atardecer y por la tarde, y azul profundo de noche. Solo cambia cuando nadie puede verlo (pantalla bloqueada, pantalla apagada o protector de pantalla activo), porque Windows congela brevemente todas las aplicaciones cada vez que cambia. Tu propio color de énfasis vuelve cuando se desactiva.",
            [nameof(SkyWeatherEnabledLabel)] = "Mostrar el tiempo en el cielo",
            [nameof(SkyWeatherEnabledTooltip)] =
                "Muestra en el cielo el tiempo donde estás: nubes, lluvia, tormentas, nieve, niebla, calima, humo y polvo, y el azul más intenso de los lugares altos. El pronóstico llega de Open-Meteo cada hora, con tu ubicación redondeada a unos 10 km. Sin conexión a Internet, el último tiempo se desvanece poco a poco hasta un cielo despejado.",
            [nameof(SkyWeatherLocationMissingHint)] =
                "Establece tu ubicación en la pestaña «Horario y ubicación» para mostrar el tiempo.",
            [nameof(DynamicWallpaperStepButton)] = "Siguiente hora del día",
            [nameof(DynamicWallpaperStepTooltip)] =
                "Para probarlo: avanza 90 minutos cada vez y ajusta la pantalla, las luces, el fondo de pantalla y el color de énfasis a esa hora del día",
            [nameof(DynamicWallpaperStopButton)] = "Detener",
            [nameof(DynamicWallpaperPreviewingText)] = "Vista previa de {0}",
            [nameof(HueNotConnectedText)] =
                "Haz clic en Conectar y pulsa el botón de tu puente Hue.",
            [nameof(HueSearchingText)] = "Buscando tu puente Hue…",
            [nameof(HueNoBridgeFoundText)] =
                "No se encontró ningún puente Hue. Escribe su dirección IP y haz clic en Conectar.",
            [nameof(HueWaitingForButtonText)] =
                "Pulsa el botón de la parte superior de tu puente Hue…",
            [nameof(HueButtonNotPressedText)] =
                "No se pulsó el botón del puente a tiempo. Haz clic en Conectar para volver a intentarlo.",
            [nameof(HueConnectedText)] = "Conectado al puente Hue.",
            [nameof(HueNoRoomsText)] =
                "Conectado. Elige las habitaciones cuyas luces siguen a Afterglow.",
            [nameof(HuePausedText)] =
                "Conectado. Las luces volverán a seguir a Afterglow en cuanto se vuelva a activar.",
            [nameof(HuePausedWithScreenText)] =
                "Conectado. Las luces están en pausa junto con la pantalla y volverán a seguir a Afterglow cuando se reanude.",
            [nameof(HueFollowingText)] = "Luces que siguen a Afterglow: {0}",
            [nameof(HueFollowingUnresponsiveText)] =
                "Luces que siguen a Afterglow: {0} ({1} no responden; se vuelve a intentar de vez en cuando)",
            [nameof(HueShowLightsTooltip)] =
                "Mostrar u ocultar las luces de las habitaciones elegidas, para elegir cuáles siguen a Afterglow",
            [nameof(HueUnnamedLightText)] = "Luz sin nombre",
            [nameof(HueLightFollowingText)] = "Siguiendo",
            [nameof(HueLightNotRespondingText)] =
                "No responde; se vuelve a intentar de vez en cuando",
            [nameof(HueLightLeftAloneText)] =
                "Cambiada en otro lugar; se deja así hasta que se apague y se vuelva a encender",
            [nameof(HueLightOffText)] = "Apagada",
            [nameof(HueLightExcludedText)] = "Excluida",
            [nameof(HueLightWaitingText)] = "Aún no sigue",
            [nameof(HueFollowAgainButton)] = "Volver a seguir",
            [nameof(HueFollowAgainTooltip)] = "Hacer que esta luz vuelva a seguir a Afterglow",
            [nameof(HuePausedUntilText)] =
                "Las luces están en pausa hasta las {0} y vuelven a como estaban mientras tanto.",
            [nameof(HuePausedUntilResumedText)] =
                "Las luces están en pausa hasta que se reanuden desde la bandeja, y vuelven a como estaban mientras tanto.",
            [nameof(TrayPauseLightsMenuItem)] = "Pausar luces",
            [nameof(TrayResumeLightsMenuItem)] = "Reanudar luces",
            [nameof(TrayTurnOnLightsMenuItem)] = "Encender luces ({0})",
            [nameof(HueLightNotAdjustableText)] = "No se puede ajustar (solo encender y apagar)",
            [nameof(HueLightNothingFollowedText)] =
                "Nada que seguir (solo puede hacer lo que no se sigue)",
            [nameof(HueLightPausedText)] = "En pausa",
            [nameof(HueFollowingNoneText)] =
                "Ninguna luz está siguiendo ahora mismo. Las luces de las habitaciones elegidas siguen a Afterglow en cuanto se encienden.",
            [nameof(HueShutdownLabel)] = "Al apagar o suspender el PC",
            [nameof(HueShutdownTooltip)] =
                "Qué pasa con las luces que siguen a Afterglow cuando este PC se apaga, se reinicia, cierra la sesión o se suspende. Las luces que encendió Afterglow se apagan (salvo con un ajuste personalizado), y las que cambiaste tú se dejan como están.",
            [nameof(HueShutdownLeaveOption)] = "Dejar las luces como están",
            [nameof(HueShutdownRestoreOption)] = "Devolver las luces a como estaban",
            [nameof(HueShutdownTurnOffOption)] = "Apagar las luces",
            [nameof(HuePauseActionLabel)] = "Cuando las luces están en pausa",
            [nameof(HuePauseActionTooltip)] =
                "Qué hacen las luces que siguen a Afterglow mientras están en pausa: desde la bandeja o con un atajo, mientras Afterglow está en pausa, o junto con la pantalla. Las luces apagadas para una pausa se vuelven a encender después.",
            [nameof(HueCustomOption)] = "Personalizado",
            [nameof(HueLookTemperatureLabel)] = "Temperatura",
            [nameof(HueLookTemperatureTooltip)] =
                "La temperatura de color de las propias luces (las luces de color llegan un poco más cálidas que las blancas). Mantén pulsado el botón del ratón sobre el control para verla en las luces; vuelven en cuanto lo sueltas.",
            [nameof(HueLookBrightnessLabel)] = "Brillo",
            [nameof(HueLookBrightnessTooltip)] =
                "Qué tan brillantes son las luces. Mantén pulsado el botón del ratón sobre el control para verlo en las luces; vuelven en cuanto lo sueltas.",
            [nameof(HueTurnOnAtStartLabel)] = "Encender las luces al iniciar Afterglow",
            [nameof(HueTurnOnAtStartTooltip)] =
                "Enciende todas las luces de las habitaciones elegidas (excepto las excluidas) al iniciar Afterglow, como al encender el PC o iniciar sesión, y al despertar de la suspensión",
            [nameof(HueReclaimLabel)] = "Cuando las luces se cambian en otro lugar",
            [nameof(HueReclaimTooltip)] =
                "Cuando una luz se cambia fuera de Afterglow (en la app Hue, con un interruptor, una escena o un asistente de voz), se deja como está; este es el tiempo, contado desde su último cambio. «Volver a seguir» en la lista de luces la sigue de nuevo al instante.",
            [nameof(HueReclaimNeverOption)] = "Dejarlas así hasta apagarlas y encenderlas",
            [nameof(HueLightLeftAloneUntilText)] =
                "Cambiada en otro lugar; se sigue de nuevo a las {0}",
            [nameof(SmartLightsTabName)] = "Luces y fondo",
            [nameof(HueReclaimAfterOption)] = "Volver a seguir tras un tiempo fijado",
            [nameof(HueReclaimAfterLabel)] = "Tras",
            [nameof(MinutesValueText)] = "{0} min",
            [nameof(HoursValueText)] = "{0} h",
            [nameof(HoursMinutesValueText)] = "{0} h {1} min",
            [nameof(HueFollowingCardTitle)] = "Seguimiento",
            [nameof(HueFollowingCardSubtitle)] =
                "Qué toman las luces de la pantalla, y cuánto más cálidas y tenues son a su lado.",
            [nameof(HueReclaimCardTitle)] = "Cambios en otro lugar",
            [nameof(HueReclaimCardSubtitle)] =
                "Las luces cambiadas en la app Hue, con un interruptor, una escena o un asistente de voz se dejan como están un rato.",
            [nameof(HuePausingCardTitle)] = "Pausa",
            [nameof(HuePausingCardSubtitle)] = "Qué hacen las luces mientras están en pausa.",
            [nameof(HueStartupCardTitle)] = "Inicio y apagado",
            [nameof(HueStartupCardSubtitle)] =
                "Qué hacen las luces al iniciar Afterglow, y cuando el PC se apaga o se suspende.",
            [nameof(HueUnreachableText)] =
                "No se pudo conectar con el puente Hue. Se volverá a intentar en breve.",
            [nameof(HueUnauthorizedText)] =
                "El puente Hue ya no acepta Afterglow. Haz clic en Volver a conectar y pulsa el botón del puente.",
            [nameof(HueCertificateChangedText)] =
                "En esta dirección ha respondido algo distinto de tu puente Hue. Si has cambiado de puente, haz clic en Volver a conectar.",
            [nameof(NeverCheckedText)] = "Aún no se ha comprobado",
            [nameof(UpdateCheckingText)] = "Buscando actualizaciones…",
            [nameof(UpToDateText)] = "{0} {1} es la versión más reciente.",
            [nameof(UpdateCheckFailedText)] =
                "No se pudieron buscar actualizaciones. Comprueba tu conexión a Internet e inténtalo de nuevo.",
            [nameof(UpdateCheckUnavailableText)] =
                "Las versiones de desarrollo no buscan actualizaciones.",
            [nameof(WhitelistedApplicationsPauseLabel)] =
                "Pausar con aplicaciones de la lista blanca",
            [nameof(WhitelistedApplicationsPauseTooltip)] =
                "Pausar mientras la ventana en primer plano pertenezca a una de las aplicaciones seleccionadas",
            [nameof(WhitelistedWindowTitlesPauseLabel)] =
                "Pausar con títulos de ventana de la lista blanca",
            [nameof(WhitelistedWindowTitlesPauseTooltip)] =
                "Pausar mientras el título de la ventana en primer plano contenga una de las palabras clave (sin distinguir mayúsculas)",
            [nameof(WindowTitleKeywordPlaceholder)] = "Palabra clave del título, p. ej. Netflix",
            [nameof(AddButton)] = "AÑADIR",
            [nameof(RemoveWindowTitleTooltip)] = "Quitar palabra clave",
            [nameof(PerMonitorWhitelistPauseLabel)] =
                "Pausar solo el monitor con la ventana coincidente",
            [nameof(PerMonitorWhitelistPauseTooltip)] =
                "Los demás monitores siguen ajustados. Si está desactivado, se pausan todos.",
            [nameof(PerMonitorFullscreenPauseLabel)] =
                "Pausar solo el monitor en pantalla completa",
            [nameof(PerMonitorFullscreenPauseTooltip)] =
                "Cuando una aplicación en pantalla completa esté en primer plano, restablecer los colores normales solo en la pantalla que la muestra y mantener las demás pantallas en el ciclo",
            [nameof(StreamingAutoPauseLabel)] =
                "Pausar durante la transmisión remota (Sunshine / RDP)",
            [nameof(StreamingAutoPauseTooltip)] =
                "Restablecer los colores normales en todas las pantallas mientras este equipo se transmite con Sunshine o se usa mediante Escritorio remoto",
            [nameof(StreamingPortsLabel)] = "Puertos TCP de transmisión",
            [nameof(StreamingPortsTooltip)] =
                "Escritorio remoto se detecta automáticamente, en cualquier puerto. Sunshine y otras aplicaciones de transmisión se detectan por una conexión activa en uno de estos puertos TCP locales, separados por comas (p. ej., 48010 para Sunshine, o 48010, 27036 para incluir también Steam Remote Play).",
            [nameof(DisplaysCardTitle)] = "Pantallas conectadas",
            [nameof(DisplaysCardSubtitle)] =
                "Excluye pantallas o ajusta su calidez de forma individual. Las pantallas se reconocen de nuevo al volver a conectarlas.",
            [nameof(IdentifyButton)] = "IDENTIFICAR",
            [nameof(IdentifyTooltip)] = "Mostrar el número de cada pantalla en ella",
            [nameof(OffsetScalingLabel)] = "Escalado del desplazamiento",
            [nameof(OffsetScalingTooltip)] =
                "Cómo se aplica el desplazamiento de temperatura de cada pantalla a medida que el ciclo pasa del día a la noche",
            [nameof(PerceptualOffsetScalingOption)] = "Mireds (perceptual)",
            [nameof(FixedOffsetScalingOption)] = "Kelvin (fijo)",
            [nameof(PerceptualOffsetScalingDescription)] =
                "Los desplazamientos de temperatura son exactos a tu temperatura de día y se escalan de forma perceptual hacia la tarde y la noche, para que distintos tipos de pantalla sigan visualmente igualados.",
            [nameof(FixedOffsetScalingDescription)] =
                "Los desplazamientos de temperatura se aplican siempre como el mismo número fijo de kelvin, sin escalado entre el día y la noche.",
            [nameof(NoDisplaysText)] = "No se detectaron pantallas.",
            [nameof(PrimaryBadge)] = "PRINCIPAL",
            [nameof(DisplayEnabledTooltip)] =
                "Aplicar Afterglow a esta pantalla. Una pantalla excluida se mantiene con colores neutros.",
            [nameof(MonitorTemperatureOffsetLabel)] = "Desplazamiento de temperatura",
            [nameof(MonitorTemperatureOffsetTooltip)] =
                "Desplaza la temperatura de color solo en esta pantalla. Con el escalado perceptual, es el desplazamiento a tu temperatura de día.",
            [nameof(MonitorBrightnessOffsetLabel)] = "Desplazamiento de brillo",
            [nameof(MonitorBrightnessOffsetTooltip)] =
                "Se suma al brillo actual solo en esta pantalla",
            [nameof(ResetOffsetsTooltip)] = "Restablecer desplazamientos",
            [nameof(ReferenceDisplayNote)] =
                "Pantalla de referencia · Sigue el horario principal de «Ciclo y colores»",
            [nameof(EffectiveNowText)] = "Ahora: {0} K · {1}",
            [nameof(ExcludedNeutralText)] = "Excluida · Neutra 6600 K",
            [nameof(DisconnectedDisplaysLabel)] = "Pantallas desconectadas guardadas ({0})",
            [nameof(ForgetButton)] = "OLVIDAR",
            [nameof(ForgetDisplayTooltip)] = "Eliminar las preferencias guardadas de esta pantalla",
            // Controls & app
            [nameof(HotkeysCardTitle)] = "Atajos globales",
            [nameof(HotkeysCardSubtitle)] =
                "Haz clic en un campo y pulsa una combinación de teclas para asignarla.",
            [nameof(AppCardTitle)] = "Aplicación",
            [nameof(AdvancedCardTitle)] = "Avanzado",
            [nameof(BedtimeSummaryTooltip)] =
                "La hora de dormir se ajusta junto con los colores de la tarde, en la pestaña Ciclo y colores.",
            [nameof(StartMinimizedLabel)] = "Iniciar minimizado",
            [nameof(StartMinimizedTooltip)] =
                "Ocultar Afterglow en la bandeja del sistema al iniciarse, incluso cuando se abre manualmente",
            [nameof(ExtendedGammaRangeLabel)] = "Rango de gamma ampliado",
            [nameof(ExtendedGammaRangeTooltip)] =
                "Desbloquear el rango completo de gamma en Windows, lo que permite temperaturas de color más cálidas y un brillo más bajo. Este cambio requiere privilegios de administrador.",
            [nameof(GammaPollingIntervalLabel)] = "Actualizar cada",
            [nameof(GammaPollingIntervalTooltip)] =
                "Con qué frecuencia se fuerza la actualización de la gamma cuando el sondeo de gamma está activado",
            [nameof(SecondsValueText)] = "{0} s",
            // Tray menu
            [nameof(TrayOpenMenuItem)] = "Abrir Afterglow",
            [nameof(TrayPauseMenuItem)] = "Pausar",
            [nameof(TrayResumeMenuItem)] = "Reanudar",
            [nameof(TrayPreviewMenuItem)] = "Vista previa del ciclo de 24 h",
            [nameof(PauseFor15MinutesChip)] = "15 min",
            [nameof(PauseFor1HourChip)] = "1 h",
            [nameof(PauseFor3HoursChip)] = "3 h",
            [nameof(PausedStateLabel)] = "En pausa",
            // Grayscale
            [nameof(TrayGrayscaleMenuItem)] = "Escala de grises",
            [nameof(ToggleGrayscaleHotkeyLabel)] = "Alternar escala de grises",
            [nameof(ToggleGrayscaleHotkeyTooltip)] =
                "Atajo global para activar/desactivar la escala de grises",
            [nameof(ToggleLightsPauseHotkeyLabel)] = "Pausar/reanudar luces Hue",
            [nameof(ToggleLightsPauseHotkeyTooltip)] =
                "Atajo global para pausar las luces Hue (de vuelta a como estaban) o reanudarlas",
            [nameof(GrayscaleCardTitle)] = "Escala de grises",
            [nameof(GrayscaleCardSubtitle)] =
                "Quita los colores de la pantalla, manualmente o según un horario.",
            [nameof(GrayscaleScheduleLabel)] = "Escala de grises automática",
            [nameof(GrayscaleScheduleTooltip)] =
                "Cuándo se activa la escala de grises por sí sola. Siempre se puede alternar manualmente, y una elección manual dura hasta el siguiente cambio del horario.",
            [nameof(GrayscaleScheduleOffOption)] = "Desactivada",
            [nameof(GrayscaleScheduleNightOption)] = "Solo de noche",
            [nameof(GrayscaleScheduleEveningAndNightOption)] = "Tarde y noche",
            [nameof(GrayscaleTemperatureShiftLabel)] =
                "Aplicar la temperatura de color en escala de grises",
            [nameof(GrayscaleTemperatureShiftTooltip)] =
                "Seguir aplicando la temperatura de color sobre la escala de grises, para un aspecto monocromo ámbar y cálido. Si está desactivado, la escala de grises usa un punto blanco neutro y el brillo se sigue aplicando.",
            [nameof(GrayscaleScheduleOnOption)] = "Activada",
            [nameof(TrayResumeGrayscaleMenuItem)] = "Reanudar escala de grises",
        };
}
