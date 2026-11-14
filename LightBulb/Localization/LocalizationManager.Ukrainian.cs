using System.Collections.Generic;

namespace LightBulb.Localization;

public partial class LocalizationManager
{
    private static readonly IReadOnlyDictionary<string, string> UkrainianLocalization =
        new Dictionary<string, string>
        {
            [nameof(SunsetLabel)] = "Захід",
            [nameof(SunriseLabel)] = "Схід",
            [nameof(OffsetTooltipHeader)] =
                "Поточні значення температури та яскравості скориговані зміщенням:",
            [nameof(TemperatureOffsetLabel)] = "Зміщення температури:",
            [nameof(BrightnessOffsetLabel)] = "Зміщення яскравості:",
            [nameof(ClickToResetLabel)] = "Натисніть для скидання",
            [nameof(OffsetLabel)] = "зміщення",
            [nameof(ToggleLightBulbTooltip)] = "Увімкнути/вимкнути LightBulb",
            [nameof(HideToTrayTooltip)] = "Сховати LightBulb у системний трей",
            [nameof(PreviewText)] = "ДЕМОНСТРАЦІЯ",
            [nameof(StopPreviewTooltip)] = "Зупинити демонстрацію",
            [nameof(StartPreviewTooltip)] = "Запустити демонстрацію 24-годинного циклу",
            [nameof(OpenSettingsTooltip)] = "Відкрити налаштування",
            [nameof(ResetTooltip)] = "Скинути всі налаштування до типових",
            [nameof(LanguageLabel)] = "Мова",
            [nameof(LanguageTooltip)] = "Бажана мова інтерфейсу",
            [nameof(StartWithWindowsLabel)] = "Запускати з Windows",
            [nameof(StartWithWindowsTooltip)] = "Запускати LightBulb при старті Windows",
            [nameof(DefaultToDayConfigLabel)] = "Повертатись до денної конфігурації",
            [nameof(DefaultToDayConfigTooltip)] =
                "Коли LightBulb вимкнено або призупинено, відновлювати налаштовану денну температуру та яскравість замість гами монітора за замовчуванням",
            [nameof(PauseWhenFullscreenLabel)] = "Призупиняти при повноекранному режимі",
            [nameof(PauseWhenFullscreenTooltip)] =
                "Призупиняти LightBulb, коли будь-яке повноекранне вікно знаходиться на передньому плані",
            [nameof(GammaSmoothingLabel)] = "Згладжування гами",
            [nameof(GammaSmoothingTooltip)] =
                "Повільно перемикати гаму при вмиканні або вимиканні LightBulb, щоб очі мали час адаптуватись",
            [nameof(GammaPollingLabel)] = "Оновлення гами",
            [nameof(GammaPollingTooltip)] =
                "Примусово оновлювати гаму монітора через регулярні проміжки часу, щоб інші програми не могли її перевизначити",
            [nameof(DayTemperatureTooltip)] = "Колірна температура протягом дня",
            [nameof(NightTemperatureTooltip)] = "Колірна температура протягом ночі",
            [nameof(DayBrightnessTooltip)] = """
                Яскравість протягом дня

                Зауважте, що це налаштування яскравості застосовується до колірної гами, а не до фактичної яскравості монітора.
                Якщо ваш комп'ютер вже здатний автоматично регулювати яскравість екрана залежно від умов освітлення (як правило, ноутбуки), то рекомендується вимкнути контроль яскравості LightBulb, залишивши обидва параметри яскравості на рівні 100%.
                """,
            [nameof(NightBrightnessTooltip)] = """
                Яскравість протягом ночі

                Зауважте, що це налаштування яскравості застосовується до колірної гами, а не до фактичної яскравості монітора.
                Якщо ваш комп'ютер вже здатний автоматично регулювати яскравість екрана залежно від умов освітлення (як правило, ноутбуки), то рекомендується вимкнути контроль яскравості LightBulb, залишивши обидва параметри яскравості на рівні 100%.
                """,
            [nameof(TransitionDurationTooltip)] =
                "Час, необхідний для перемикання між денною та нічною конфігураціями",
            [nameof(TransitionOffsetTooltip)] =
                "Зміщення, що визначає, наскільки раніше або пізніше починається перехід відносно сходу та заходу сонця",
            [nameof(ManualLabel)] = "Ручна",
            [nameof(ManualTooltip)] = "Налаштувати схід та захід сонця вручну",
            [nameof(LocationBasedLabel)] = "За місцезнаходженням",
            [nameof(LocationBasedTooltip)] =
                "Налаштуйте своє місцезнаходження для автоматичного обчислення часу сходу та заходу сонця",
            [nameof(YourLocationLabel)] = "Ваше місцезнаходження:",
            [nameof(AutoDetectLocationTooltip)] =
                "Спробувати автоматично визначити місцезнаходження за вашою IP-адресою",
            [nameof(LocationQueryTooltip)] = """
                Вкажіть своє місцезнаходження за географічними координатами або пошуковим запитом

                Приклади допустимих форматів:
                **41.25, -120.9762**
                **41.25°N, 120.9762°W**
                **New York, USA**
                **Germany**
                """,
            [nameof(SetLocationTooltip)] = "Встановити місцезнаходження",
            [nameof(LocationErrorText)] = "Помилка визначення місцезнаходження, спробуйте ще раз",
            [nameof(ToggleLightBulbHotkeyLabel)] = "Перемкнути LightBulb",
            [nameof(ToggleLightBulbHotkeyTooltip)] =
                "Глобальна гаряча клавіша для увімкнення/вимкнення LightBulb",
            [nameof(ToggleWindowLabel)] = "Перемкнути вікно",
            [nameof(ToggleWindowHotkeyTooltip)] =
                "Глобальна гаряча клавіша для показу/приховання головного вікна LightBulb",
            [nameof(IncreaseTemperatureOffsetLabel)] = "Зміщення температури ↑",
            [nameof(IncreaseTemperatureOffsetTooltip)] =
                "Глобальна гаряча клавіша для збільшення поточного зміщення температури",
            [nameof(DecreaseTemperatureOffsetLabel)] = "Зміщення температури ↓",
            [nameof(DecreaseTemperatureOffsetTooltip)] =
                "Глобальна гаряча клавіша для зменшення поточного зміщення температури",
            [nameof(IncreaseBrightnessOffsetLabel)] = "Зміщення яскравості ↑",
            [nameof(IncreaseBrightnessOffsetTooltip)] =
                "Глобальна гаряча клавіша для збільшення поточного зміщення яскравості",
            [nameof(DecreaseBrightnessOffsetLabel)] = "Зміщення яскравості ↓",
            [nameof(DecreaseBrightnessOffsetTooltip)] =
                "Глобальна гаряча клавіша для зменшення поточного зміщення яскравості",
            [nameof(ResetOffsetLabel)] = "Скинути зміщення",
            [nameof(ResetOffsetHotkeyTooltip)] =
                "Глобальна гаряча клавіша для скидання поточних зміщень температури та яскравості",
            [nameof(AppWhitelistLabel)] = "Білий список програм",
            [nameof(RefreshAppsTooltip)] = "Оновити список запущених програм",
            [nameof(PauseForWhitelistedTooltip)] =
                "Призупиняти LightBulb, коли одна з вибраних програм знаходиться на передньому плані",
            [nameof(TraySettingsMenuItem)] = "Налаштування",
            [nameof(TrayDisableUntilSunriseMenuItem)] = "До сходу сонця",
            [nameof(TrayExitMenuItem)] = "Вийти",
            [nameof(TrayTooltipDisabled)] = "Вимкнено",
            [nameof(CloseButton)] = "ЗАКРИТИ",
            [nameof(UnstableBuildTitle)] = "Попередження про нестабільну збірку",
            [nameof(UnstableBuildMessage)] = """
                Ви використовуєте збірку розробника {0}. Ці збірки не пройшли ретельного тестування і можуть містити помилки.

                Автоматичні оновлення вимкнені для збірок розробника. Якщо ви хочете перейти на стабільний реліз, завантажте його вручну.
                """,
            [nameof(SeeReleasesButton)] = "ПЕРЕГЛЯНУТИ РЕЛІЗИ",
            [nameof(LimitedGammaRangeTitle)] = "Обмежений діапазон гами",
            [nameof(LimitedGammaRangeMessage)] = """
                {0} виявив, що розширені засоби керування діапазоном гами не увімкнені в цій системі.
                Це може призвести до некоректної роботи деяких колірних конфігурацій.

                Натисніть ВИПРАВИТИ для розблокування діапазону гами. Можуть знадобитися права адміністратора.
                """,
            [nameof(FixButton)] = "ВИПРАВИТИ",
            [nameof(WelcomeTitle)] = "Ласкаво просимо!",
            [nameof(WelcomeMessage)] = """
                Дякуємо за встановлення {0}!
                Для найкращого персоналізованого досвіду, будь ласка, налаштуйте бажану сонячну конфігурацію.

                Натисніть ОК для відкриття налаштувань.
                """,
            [nameof(OkButton)] = "ОК",
            // Dashboard
            [nameof(BedtimeLabel)] = "Час сну",
            [nameof(DayStateLabel)] = "День",
            [nameof(EveningStateLabel)] = "Вечір",
            [nameof(NightStateLabel)] = "Ніч",
            [nameof(SunriseTransitionPhaseLabel)] = "Ранковий перехід",
            [nameof(SunsetTransitionPhaseLabel)] = "Вечірній перехід",
            [nameof(NightTransitionPhaseLabel)] = "Нічний перехід",
            [nameof(StatusDayPhrases)] = """
                Денні години. Кольори збалансовані для роботи та точного контрасту.
                Активний повний денний спектр для чіткого тексту та зображення.
                Нейтральний баланс білого відповідає яскравому денному освітленню.
                Активне стандартне денне калібрування для редагування та перегляду.
                Кольори екрана налаштовані на чисте денне світло без зсуву.
                Режим денної концентрації. Екран налаштовано на максимальну чіткість.
                Денний профіль працює з повною яскравістю та природним білим.
                """,
            [nameof(StatusEveningPhrases)] = """
                Сонце вже сіло. Відблиски екрана поступово пом'якшуються на вечір.
                Вечірні години. Різке синє світло приглушено, поки ви відпочиваєте.
                Настали сутінки. Екран переведено на комфортне вечірнє тепло.
                Після роботи. Тон екрана пом'якшено для відпочинку та перегляду.
                Вечірнє тепло знімає різкість яскравого білого.
                Після заходу сонця кольори стали теплішими для зручного перегляду.
                Активний сутінковий розклад. Збалансоване тепло без сильного відтінку.
                """,
            [nameof(StatusNightPhrases)] = """
                Пізні нічні години. Глибоке тепло активне для темних кімнат.
                Працює розклад перед сном. Синє світло зведено до мінімуму.
                Екран налаштовано на приглушений бурштиновий тон для нічного читання.
                Тихі години. Відблиски екрана залишаються низькими до ранку.
                Опівнічний режим береже ваші очі в темній кімнаті.
                Пізні години. Перед сном працює найтепліший колірний профіль.
                Активний нічний розклад. Різкий білий замінено м'яким бурштиновим.
                """,
            [nameof(StatusTransitionToDayText)] = "Перехід до денних кольорів...",
            [nameof(StatusTransitionToEveningText)] = "Перехід до вечірнього тепла...",
            [nameof(StatusTransitionToNightText)] = "Перехід до нічного тепла...",
            [nameof(StatusDisabledText)] =
                "LightBulb призупинено. Екран має нейтральні денні кольори.",
            [nameof(StatusDisabledDayConfigText)] =
                "LightBulb призупинено. Екран має ваші денні кольори.",
            [nameof(StatusPausedText)] =
                "Автоматично призупинено через повноекранну програму або програму з білого списку.",
            [nameof(StatusPreviewText)] = "Демонстрація наступних 24 годин…",
            [nameof(StatusRemoteStreamingPausedText)] =
                "Призупинено, доки триває сеанс віддаленої трансляції.",
            [nameof(PausedRemoteStreamingLabel)] = "Призупинено (віддалена трансляція)",
            [nameof(PausedScreenLockedLabel)] = "Призупинено (екран заблоковано)",
            [nameof(PausedFullScreenLabel)] = "Призупинено (повноекранна програма)",
            [nameof(PausedWhitelistLabel)] = "Призупинено (програма з білого списку)",
            [nameof(PausedCaption)] = "{0} · {1} K (яскравість {2})",
            [nameof(SolarEventPastText)] = "{0}: {1} тому",
            [nameof(SolarEventFutureText)] = "{0}: через {1}",
            [nameof(SolarEventNowText)] = "{0}: зараз",
            [nameof(DurationMinuteText)] = "1 хв",
            [nameof(DurationMinutesText)] = "{0} хв",
            [nameof(DurationHourText)] = "1 год",
            [nameof(DurationHoursText)] = "{0} год",
            [nameof(ActivePhaseCaption)] = "Активна фаза: {0} · {1} K (яскравість {2})",
            [nameof(ActiveStageTemperatureTooltip)] =
                "Перетягніть, щоб змінити колірну температуру поточного етапу ({0})",
            [nameof(ManualScheduleText)] = "Ручний розклад",
            [nameof(LocationNotSetText)] = "Місцезнаходження: не вказано",
            [nameof(LocationCoordinatesText)] = "Місцезнаходження: {0}",
            [nameof(OpenLocationSettingsTooltip)] = "Змінити налаштування місцезнаходження",
            // Cycle
            [nameof(EveningStageTooltip)] =
                "Використовувати проміжну вечірню конфігурацію від заходу сонця до початку ночі, замість того щоб одразу переходити до нічної конфігурації на заході сонця",
            [nameof(EveningTemperatureTooltip)] =
                "Колірна температура між заходом сонця та початком ночі",
            [nameof(EveningBrightnessTooltip)] = """
                Яскравість між заходом сонця та початком ночі

                Зверніть увагу, що це налаштування яскравості застосовується до колірної гами, а не до фактичної яскравості монітора.
                """,
            [nameof(NightStartTimeTooltip)] = """
                Час, коли починається перехід від вечірньої конфігурації до нічної (з урахуванням зміщення переходу, так само як і для заходу сонця)

                Якщо ніч починається раніше, ніж завершується перехід на заході сонця, вечірній етап того дня пропускається.
                """,
            // Settings page
            [nameof(BackTooltip)] = "Назад до панелі (зміни збережено)",
            [nameof(CycleTabName)] = "Цикл і кольори",
            [nameof(ScheduleTabName)] = "Розклад і місце",
            [nameof(RulesTabName)] = "Правила і білий список",
            [nameof(ControlsTabName)] = "Керування і програма",
            [nameof(DayCardSubtitle)] = "Від сходу до заходу сонця",
            [nameof(EveningCardSubtitle)] = "Від заходу сонця до часу сну",
            [nameof(NightCardSubtitle)] = "Від часу сну до сходу сонця",
            [nameof(TemperatureLabel)] = "Температура",
            [nameof(BrightnessLabel)] = "Яскравість",
            [nameof(BedtimeNightStartLabel)] = "Час сну (початок ночі)",
            [nameof(TransitionsCardTitle)] = "Переходи",
            [nameof(DurationLabel)] = "Тривалість",
            [nameof(TransitionOffsetShortLabel)] = "Зміщення",
            [nameof(LocationCardTitle)] = "Місцезнаходження і координати",
            [nameof(LocationCardSubtitle)] =
                "Використовується для розрахунку сходу та заходу сонця. Знайдіть місце, введіть координати або визначте їх автоматично.",
            [nameof(LocationInUseText)] = "Використовується {0}, {1}",
            [nameof(SolarScheduleCardTitle)] = "Сонячний розклад",
            [nameof(SolarScheduleCardSubtitle)] =
                "Розраховувати схід і захід сонця за вашим місцезнаходженням або задати їх вручну.",
            [nameof(OffLabel)] = "Вимк.",
            // Rules & whitelist
            [nameof(AutoPauseCardTitle)] = "Розумне автопризупинення",
            [nameof(AutoPauseCardSubtitle)] =
                "Тимчасово повертати звичайні кольори в цих ситуаціях.",
            [nameof(PauseWhenScreenLockedLabel)] = "Призупиняти, коли екран заблоковано",
            [nameof(PauseWhenScreenLockedTooltip)] =
                "Призупиняти LightBulb, поки комп'ютер заблоковано",
            [nameof(WhitelistCardSubtitle)] =
                "Призупиняти LightBulb, поки будь-яка з вибраних програм на передньому плані.",
            [nameof(PerMonitorFullscreenPauseLabel)] =
                "Призупиняти лише монітор із повноекранною програмою",
            [nameof(PerMonitorFullscreenPauseTooltip)] =
                "Коли повноекранна програма на передньому плані, повертати звичайні кольори лише на дисплеї, де вона працює, а інші дисплеї залишати в циклі",
            [nameof(StreamingAutoPauseLabel)] =
                "Призупиняти під час віддаленої трансляції (Sunshine / RDP)",
            [nameof(StreamingAutoPauseTooltip)] =
                "Повертати звичайні кольори на всіх дисплеях, поки цей комп'ютер транслюється через Sunshine або до нього підключено віддалений робочий стіл",
            [nameof(StreamingPortsLabel)] = "TCP-порти трансляції",
            [nameof(StreamingPortsTooltip)] =
                "Віддалений робочий стіл визначається автоматично, на будь-якому порту. Sunshine та інші програми трансляції визначаються за активним з'єднанням на одному з цих локальних TCP-портів, розділених комами (наприклад, 48010 для Sunshine або 48010, 27036, щоб також врахувати Steam Remote Play).",
            [nameof(DisplaysCardTitle)] = "Підключені дисплеї",
            [nameof(DisplaysCardSubtitle)] =
                "Виключайте дисплеї або окремо налаштовуйте їхнє тепло. Дисплеї розпізнаються знову після повторного підключення.",
            [nameof(IdentifyButton)] = "ВИЗНАЧИТИ",
            [nameof(IdentifyTooltip)] = "Показати номер кожного дисплея на його екрані",
            [nameof(OffsetScalingLabel)] = "Масштабування зміщення",
            [nameof(OffsetScalingTooltip)] =
                "Як застосовується зміщення температури кожного дисплея, коли цикл переходить від дня до ночі",
            [nameof(PerceptualOffsetScalingOption)] = "Міреди (перцептивно)",
            [nameof(FixedOffsetScalingOption)] = "Кельвіни (фіксовано)",
            [nameof(PerceptualOffsetScalingDescription)] =
                "Зміщення температури точні при вашій денній температурі та перцептивно масштабуються ввечері й уночі, тож різні типи панелей залишаються візуально узгодженими.",
            [nameof(FixedOffsetScalingDescription)] =
                "Зміщення температури завжди застосовуються як однакова фіксована кількість кельвінів, без масштабування від дня до ночі.",
            [nameof(NoDisplaysText)] = "Дисплеї не виявлено.",
            [nameof(PrimaryBadge)] = "ОСНОВНИЙ",
            [nameof(DisplayEnabledTooltip)] =
                "Застосовувати LightBulb до цього дисплея. Виключений дисплей залишається з нейтральними кольорами.",
            [nameof(MonitorTemperatureOffsetLabel)] = "Зміщення температури",
            [nameof(MonitorTemperatureOffsetTooltip)] =
                "Зміщує колірну температуру лише на цьому дисплеї. З перцептивним масштабуванням це зміщення при вашій денній температурі.",
            [nameof(MonitorBrightnessOffsetLabel)] = "Зміщення яскравості",
            [nameof(MonitorBrightnessOffsetTooltip)] =
                "Додається до поточної яскравості лише на цьому дисплеї",
            [nameof(ResetOffsetsTooltip)] = "Скинути зміщення",
            [nameof(ReferenceDisplayNote)] =
                "Еталонний дисплей · Дотримується основного розкладу «Цикл і кольори»",
            [nameof(EffectiveNowText)] = "Зараз діє: {0} K · {1}",
            [nameof(ExcludedNeutralText)] = "Виключено · Нейтральні 6600 K",
            [nameof(DisconnectedDisplaysLabel)] = "Збережені відключені дисплеї ({0})",
            [nameof(ForgetButton)] = "ЗАБУТИ",
            [nameof(ForgetDisplayTooltip)] = "Видалити збережені налаштування цього дисплея",
            // Controls & app
            [nameof(HotkeysCardTitle)] = "Глобальні гарячі клавіші",
            [nameof(HotkeysCardSubtitle)] =
                "Клацніть поле та натисніть комбінацію клавіш, щоб призначити її.",
            [nameof(SystemPreferencesCardTitle)] = "Системні параметри",
            [nameof(StartMinimizedLabel)] = "Запускати згорнутим",
            [nameof(StartMinimizedTooltip)] =
                "Ховати LightBulb в системний трей під час запуску, навіть якщо його запущено вручну",
            [nameof(ExtendedGammaRangeLabel)] = "Розширений діапазон гами",
            [nameof(ExtendedGammaRangeTooltip)] =
                "Розблокувати повний діапазон гами в Windows, що дозволяє тепліші колірні температури та нижчу яскравість. Для зміни потрібні права адміністратора.",
            [nameof(GammaPollingIntervalLabel)] = "Оновлювати кожні",
            [nameof(GammaPollingIntervalTooltip)] =
                "Як часто гама примусово оновлюється, коли увімкнено оновлення гами",
            [nameof(SecondsValueText)] = "{0} с",
            // Tray menu
            [nameof(TrayOpenMenuItem)] = "Відкрити LightBulb",
            [nameof(TrayPauseMenuItem)] = "Призупинити",
            [nameof(TrayResumeMenuItem)] = "Продовжити",
            [nameof(TrayPreviewMenuItem)] = "Демонстрація 24-годинного циклу",
            [nameof(PauseFor15MinutesChip)] = "15 хв",
            [nameof(PauseFor1HourChip)] = "1 год",
            [nameof(PauseFor3HoursChip)] = "3 год",
            [nameof(PausedStateLabel)] = "Призупинено",
        };
}
