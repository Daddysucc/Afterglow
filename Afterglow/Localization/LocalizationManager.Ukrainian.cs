using System.Collections.Generic;

namespace Afterglow.Localization;

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
            [nameof(ToggleAfterglowTooltip)] = "Увімкнути/вимкнути Afterglow",
            [nameof(HideToTrayTooltip)] = "Сховати Afterglow у системний трей",
            [nameof(PreviewText)] = "ДЕМОНСТРАЦІЯ",
            [nameof(StopPreviewTooltip)] = "Зупинити демонстрацію",
            [nameof(StartPreviewTooltip)] = "Запустити демонстрацію 24-годинного циклу",
            [nameof(OpenSettingsTooltip)] = "Відкрити налаштування",
            [nameof(ResetTooltip)] =
                "Скинути налаштування на цій вкладці до типових (інші вкладки зберігають свої)",
            [nameof(ResetTabSettingsButton)] = "Скинути налаштування «{0}»",
            [nameof(LanguageLabel)] = "Мова",
            [nameof(LanguageTooltip)] = "Бажана мова інтерфейсу",
            [nameof(StartWithWindowsLabel)] = "Запускати з Windows",
            [nameof(StartWithWindowsTooltip)] = "Запускати Afterglow при старті Windows",
            [nameof(DefaultToDayConfigLabel)] = "Повертатись до денної конфігурації",
            [nameof(DefaultToDayConfigTooltip)] =
                "Коли Afterglow вимкнено або призупинено, відновлювати налаштовану денну температуру та яскравість замість гами монітора за замовчуванням",
            [nameof(PauseWhenFullscreenLabel)] = "Призупиняти при повноекранному режимі",
            [nameof(PauseWhenFullscreenTooltip)] =
                "Призупиняти Afterglow, коли будь-яке повноекранне вікно знаходиться на передньому плані",
            [nameof(GammaSmoothingLabel)] = "Згладжування гами",
            [nameof(GammaSmoothingTooltip)] =
                "Повільно перемикати гаму при вмиканні або вимиканні Afterglow, щоб очі мали час адаптуватись",
            [nameof(GammaPollingLabel)] = "Оновлення гами",
            [nameof(GammaPollingTooltip)] =
                "Примусово оновлювати гаму монітора через регулярні проміжки часу, щоб інші програми не могли її перевизначити",
            [nameof(DayTemperatureTooltip)] = "Колірна температура протягом дня",
            [nameof(NightTemperatureTooltip)] = "Колірна температура протягом ночі",
            [nameof(DayBrightnessTooltip)] = """
                Яскравість протягом дня

                Зауважте, що це налаштування яскравості застосовується до колірної гами, а не до фактичної яскравості монітора.
                Якщо ваш комп'ютер вже здатний автоматично регулювати яскравість екрана залежно від умов освітлення (як правило, ноутбуки), то рекомендується вимкнути контроль яскравості Afterglow, залишивши обидва параметри яскравості на рівні 100%.
                """,
            [nameof(NightBrightnessTooltip)] = """
                Яскравість протягом ночі

                Зауважте, що це налаштування яскравості застосовується до колірної гами, а не до фактичної яскравості монітора.
                Якщо ваш комп'ютер вже здатний автоматично регулювати яскравість екрана залежно від умов освітлення (як правило, ноутбуки), то рекомендується вимкнути контроль яскравості Afterglow, залишивши обидва параметри яскравості на рівні 100%.
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
            [nameof(ToggleAfterglowHotkeyLabel)] = "Перемкнути Afterglow",
            [nameof(ToggleAfterglowHotkeyTooltip)] =
                "Глобальна гаряча клавіша для увімкнення/вимкнення Afterglow",
            [nameof(ToggleWindowLabel)] = "Перемкнути вікно",
            [nameof(ToggleWindowHotkeyTooltip)] =
                "Глобальна гаряча клавіша для показу/приховання головного вікна Afterglow",
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
                "Призупиняти Afterglow, коли одна з вибраних програм або заголовків вікон на передньому плані",
            [nameof(TraySettingsMenuItem)] = "Налаштування",
            [nameof(TrayDisableUntilSunriseMenuItem)] = "До сходу сонця",
            [nameof(TrayExitMenuItem)] = "Вийти",
            [nameof(TrayTooltipDisabled)] = "Вимкнено",
            [nameof(CloseButton)] = "ЗАКРИТИ",
            [nameof(UnstableBuildTitle)] = "Попередження про нестабільну збірку",
            [nameof(UnstableBuildMessage)] = """
                Ви використовуєте збірку розробника {0}. Ці збірки не пройшли ретельного тестування і можуть містити помилки.

                Якщо ви хочете перейти на стабільний реліз, завантажте його зі сторінки релізів.
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
            [nameof(AlreadyRunningTitle)] = "Уже запущено",
            [nameof(AlreadyRunningMessage)] =
                "Інша копія {0} або оригінальний {1} уже запущені. Закрийте їх через значок у системному треї, а потім знову запустіть {0}.",
            [nameof(TemporaryLocationNote)] =
                "{0} запущено з тимчасової папки (наприклад, «Завантаження»), тому звідти він не запускатиметься разом із Windows. Щоб залишити його, скористайтеся інсталятором або перемістіть {1} до постійної папки й запускайте звідти.",
            // Dashboard
            [nameof(BedtimeLabel)] = "Час сну",
            [nameof(DayStateLabel)] = "День",
            [nameof(EveningStateLabel)] = "Вечір",
            [nameof(NightStateLabel)] = "Ніч",
            [nameof(SunriseTransitionPhaseLabel)] = "Ранковий перехід",
            [nameof(SunsetTransitionPhaseLabel)] = "Вечірній перехід",
            [nameof(NightTransitionPhaseLabel)] = "Нічний перехід",
            [nameof(StatusDayScheduleText)] = "Діє денний розклад з {0} K і яскравістю {1}%.",
            [nameof(StatusEveningScheduleText)] = "Діє вечірній розклад з {0} K і яскравістю {1}%.",
            [nameof(StatusNightScheduleText)] = "Діє нічний розклад з {0} K і яскравістю {1}%.",
            [nameof(StatusWarmingText)] = "Потеплення до {0} K {2}.",
            [nameof(StatusCoolingText)] = "Охолодження до {0} K {2}.",
            [nameof(StatusDimmingText)] = "Зменшення яскравості до {1}% {2}.",
            [nameof(StatusBrighteningText)] = "Збільшення яскравості до {1}% {2}.",
            [nameof(StatusWarmingDimmingText)] =
                "Потеплення до {0} K і зменшення яскравості до {1}% {2}.",
            [nameof(StatusWarmingBrighteningText)] =
                "Потеплення до {0} K і збільшення яскравості до {1}% {2}.",
            [nameof(StatusCoolingDimmingText)] =
                "Охолодження до {0} K і зменшення яскравості до {1}% {2}.",
            [nameof(StatusCoolingBrighteningText)] =
                "Охолодження до {0} K і збільшення яскравості до {1}% {2}.",
            [nameof(StatusForDayText)] = "на день",
            [nameof(StatusForEveningText)] = "на вечір",
            [nameof(StatusForNightText)] = "на ніч",
            [nameof(StatusManualOverrideText)] =
                "Активне ручне коригування з {0} K і яскравістю {1}%.",
            [nameof(StatusPausedRemoteStreamingText)] =
                "Призупинено через віддалену трансляцію, відновлено рідні кольори.",
            [nameof(StatusPausedFullScreenText)] =
                "Призупинено, поки активна повноекранна програма.",
            [nameof(StatusPausedScreenLockedText)] = "Призупинено, поки екран заблоковано.",
            [nameof(StatusPausedManuallyText)] =
                "Призупинено вручну, автоматичний розклад вимкнено.",
            [nameof(StatusPausedAutomaticallyText)] =
                "Призупинено автоматично, відновлено рідні кольори.",
            [nameof(StatusPausedRemoteStreamingDayText)] =
                "Призупинено через трансляцію, відновлено денну конфігурацію.",
            [nameof(StatusPausedAutomaticallyDayText)] =
                "Призупинено автоматично, відновлено денну конфігурацію.",
            [nameof(StatusGrayscaleText)] = "Відтінки сірого активні з {0} K і яскравістю {1}%.",
            [nameof(StatusGrayscaleNeutralText)] =
                "Відтінки сірого активні з нейтральною точкою білого.",
            [nameof(StatusGrayscaleSnoozedText)] = "Відтінки сірого тимчасово вимкнено на {0}.",
            [nameof(StatusGrayscaleHeldText)] =
                "Відтінки сірого вимкнено до наступної зміни розкладу.",
            [nameof(StatusPreviewText)] = "Демонстрація наступних 24 годин…",
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
            [nameof(ProjectPageTooltip)] = "Afterglow на GitHub",
            [nameof(CycleTabName)] = "Цикл і кольори",
            [nameof(ScheduleTabName)] = "Розклад і місце",
            [nameof(RulesTabName)] = "Правила і білий список",
            [nameof(ControlsTabName)] = "Програма і керування",
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
                "Призупиняти Afterglow, поки комп'ютер заблоковано",
            [nameof(WhitelistCardSubtitle)] =
                "Призупиняти Afterglow, поки вибрана програма або відповідне вікно на передньому плані.",
            [nameof(UpdateAvailableButton)] = "Доступне оновлення",
            [nameof(UpdateAvailableTooltip)] = "Доступна версія {0} {1}. Натисніть, щоб оновити.",
            [nameof(UpdateDownloadingText)] = "Завантаження оновлення… {0}",
            [nameof(UpdateInstallingText)] = "Встановлення оновлення…",
            [nameof(UpdateFailedButton)] = "Оновити вручну",
            [nameof(UpdateFailedTooltip)] =
                "{0} не вдалося оновитися. Натисніть, щоб відкрити сторінку завантаження.",
            [nameof(CheckForUpdatesLabel)] = "Перевіряти оновлення",
            [nameof(CheckForUpdatesTooltip)] =
                "Час від часу шукати новішу версію на GitHub. Нічого не завантажується автоматично.",
            [nameof(CheckForUpdatesNowTooltip)] = "Перевірити оновлення зараз",
            [nameof(CheckNowButton)] = "Перевірити зараз",
            [nameof(UpdateCheckNeverOption)] = "Ніколи",
            [nameof(UpdateCheckDailyOption)] = "Щодня",
            [nameof(UpdateCheckWeeklyOption)] = "Щотижня",
            [nameof(UpdateCheckEveryTwoWeeksOption)] = "Раз на два тижні",
            [nameof(CurrentVersionText)] = "Версія {0}",
            [nameof(LastUpdateCheckText)] = "Остання перевірка: {0}",
            [nameof(HueCardTitle)] = "Philips Hue",
            [nameof(HueCardSubtitle)] =
                "Лампи Hue слідують колірному циклу, поки цей ПК увімкнено і виглядають так само тепло, як екран.",
            [nameof(HueEnabledLabel)] = "Керувати лампами Hue",
            [nameof(HueEnabledTooltip)] =
                "Увімкнені лампи у вибраних кімнатах слідують колірному циклу. Лампи ніколи не вмикаються самі й повертаються до попереднього стану, коли Afterglow зупиняється (зокрема коли ПК вимикається або переходить у сон). Лампу, яку ви змінили самі, буде залишено як є, доки її не вимкнуть і не ввімкнуть знову.",
            [nameof(HueBridgeLabel)] = "Адреса мосту",
            [nameof(HueBridgeTooltip)] =
                "IP-адреса вашого мосту Hue (див. у застосунку Hue, у розділі «Налаштування», «Мости»). Залиште порожнім, щоб знайти міст автоматично.",
            [nameof(HueBridgePlaceholder)] = "Знайти автоматично",
            [nameof(HueConnectButton)] = "Під’єднати",
            [nameof(HueReconnectButton)] = "Під’єднати знову",
            [nameof(HueDisconnectButton)] = "Від'єднати",
            [nameof(HueDisconnectTooltip)] =
                "Повернути лампи як було й забути цей міст (щоб під'єднатися знову, потрібно ще раз натиснути його кнопку)",
            [nameof(HueForgetBridgeButton)] = "Забути міст",
            [nameof(HueForgetBridgeTooltip)] =
                "Від'єднатися, а також забути адресу моста й вибрані кімнати та лампи, щоб почати спочатку",
            [nameof(HueCancelButton)] = "Скасувати",
            [nameof(HueConnectTooltip)] =
                "Під’єднатися до мосту: після натискання натисніть кнопку зверху на мосту протягом 30 секунд",
            [nameof(HueRoomsLabel)] = "Кімнати",
            [nameof(HueFollowTemperatureLabel)] = "Слідувати за температурою",
            [nameof(HueFollowTemperatureTooltip)] =
                "Лампи набувають колірної температури циклу (наскільки можуть, приблизно від 2000 K до 6500 K)",
            [nameof(LightWarmthLabel)] = "Теплота",
            [nameof(LightWarmthTooltip)] =
                "Наскільки теплішим за екран налаштовується освітлення, щоб воно здавалося таким самим теплим. Кольорові лампи переходять на бурштиновий і помаранчевий, коли теплі відтінки білого закінчуються.",
            [nameof(HueFollowBrightnessLabel)] = "Слідувати за яскравістю",
            [nameof(HueFollowBrightnessTooltip)] =
                "Лампи набувають яскравості циклу (100 % в Afterglow — це повна яскравість)",
            [nameof(HuePauseWithScreenLabel)] = "Призупиняти разом з екраном",
            [nameof(HuePauseWithScreenTooltip)] =
                "Освітлення також повертається до попереднього стану, поки екран призупинено автоматично (повноекранні застосунки, застосунки з білого списку, віддалена трансляція, заблокований екран)",
            [nameof(LightDimmingLabel)] = "Приглушення",
            [nameof(LightDimmingTooltip)] =
                "Наскільки сильно приглушується освітлення, коли екран тьмянішає. При ×2.2 лампи дають стільки ж світла, скільки й екран (приглушений теплий екран дає набагато менше світла, ніж здається з його яскравості); менші значення лишають їх яскравішими, більші приглушують сильніше. Повна яскравість екрана без відтінку завжди лишається повною.",
            [nameof(DynamicWallpaperCardTitle)] = "Динамічні шпалери",
            [nameof(DynamicWallpaperCardSubtitle)] =
                "Небо в колірній температурі та яскравості екрана, крізь схід і захід сонця. Якщо динамічне освітлення Windows відповідає кольору акценту, підсвічування монітора теж слідує за ним.",
            [nameof(DynamicWallpaperEnabledLabel)] = "Малювати шпалери",
            [nameof(DynamicWallpaperEnabledTooltip)] =
                "Замінює шпалери на кожному екрані (вертикальні екрани отримують вертикальне зображення). Ваші власні шпалери повертаються, коли це вимкнено.",
            [nameof(AccentColorMatchedLabel)] = "Підбирати колір акценту",
            [nameof(AccentColorMatchedTooltip)] =
                "Встановлює колір акценту Windows точно в колір і яскравість світла, для динамічного освітлення (увімкніть «Відповідати кольору акценту Windows» у Параметри › Персоналізація › Динамічне освітлення). Автоматичний колір акценту, взятий зі шпалер, не врахував би приглушення. Ваш власний колір акценту повертається, коли це вимкнено.",
            [nameof(DynamicWallpaperStepButton)] = "Наступний час доби",
            [nameof(DynamicWallpaperStepTooltip)] =
                "Для перевірки: щоразу переходить на 90 хвилин уперед і встановлює екран, освітлення та шпалери на цей час доби",
            [nameof(DynamicWallpaperStopButton)] = "Зупинити",
            [nameof(DynamicWallpaperPreviewingText)] = "Перегляд {0}",
            [nameof(HueNotConnectedText)] =
                "Натисніть «Під’єднати», а потім кнопку на вашому мосту Hue.",
            [nameof(HueSearchingText)] = "Пошук мосту Hue…",
            [nameof(HueNoBridgeFoundText)] =
                "Міст Hue не знайдено. Введіть його IP-адресу й натисніть «Під’єднати».",
            [nameof(HueWaitingForButtonText)] = "Натисніть кнопку зверху на вашому мосту Hue…",
            [nameof(HueButtonNotPressedText)] =
                "Кнопку мосту не натиснули вчасно. Натисніть «Під’єднати», щоб спробувати знову.",
            [nameof(HueConnectedText)] = "Під’єднано до мосту Hue.",
            [nameof(HueNoRoomsText)] =
                "Під’єднано. Виберіть кімнати, лампи яких слідують Afterglow.",
            [nameof(HuePausedText)] =
                "Під’єднано. Лампи знову слідуватимуть Afterglow, щойно його буде ввімкнено.",
            [nameof(HuePausedWithScreenText)] =
                "Підключено. Освітлення призупинено разом з екраном, і воно знову слідуватиме за Afterglow, щойно той відновить роботу.",
            [nameof(HueFollowingText)] = "Лампи, що слідують Afterglow: {0}",
            [nameof(HueFollowingUnresponsiveText)] =
                "Лампи, що слідують Afterglow: {0} ({1} не відповідають, спроби повторюються час від часу)",
            [nameof(HueShowLightsTooltip)] =
                "Показати або сховати лампи у вибраних кімнатах, щоб вибрати, які з них слідують Afterglow",
            [nameof(HueUnnamedLightText)] = "Лампа без назви",
            [nameof(HueLightFollowingText)] = "Слідує",
            [nameof(HueLightNotRespondingText)] = "Не відповідає, спроби повторюються час від часу",
            [nameof(HueLightLeftAloneText)] =
                "Змінено деінде, лишається як є, доки її не вимкнуть і не ввімкнуть",
            [nameof(HueLightOffText)] = "Вимкнено",
            [nameof(HueLightExcludedText)] = "Виключено",
            [nameof(HueLightWaitingText)] = "Ще не слідує",
            [nameof(HueFollowAgainButton)] = "Слідувати знову",
            [nameof(HueFollowAgainTooltip)] = "Знову змусити цю лампу слідувати Afterglow",
            [nameof(HuePausedUntilText)] =
                "Освітлення призупинено до {0}, і доти воно повернуто до попереднього стану.",
            [nameof(HuePausedUntilResumedText)] =
                "Освітлення призупинено, доки його не відновлять із системного лотка, і доти воно повернуто до попереднього стану.",
            [nameof(TrayPauseLightsMenuItem)] = "Призупинити освітлення",
            [nameof(TrayResumeLightsMenuItem)] = "Відновити освітлення",
            [nameof(TrayTurnOnLightsMenuItem)] = "Увімкнути освітлення ({0})",
            [nameof(HueLightNotAdjustableText)] =
                "Не налаштовується (лише вмикається й вимикається)",
            [nameof(HueLightNothingFollowedText)] =
                "Нема чого відстежувати (вона вміє лише те, що не відстежується)",
            [nameof(HueLightPausedText)] = "Призупинено",
            [nameof(HueFollowingNoneText)] =
                "Зараз жодна лампа не слідує. Лампи у вибраних кімнатах слідують Afterglow, щойно їх увімкнуть.",
            [nameof(HueShutdownLabel)] = "Коли ПК вимикається або засинає",
            [nameof(HueShutdownTooltip)] =
                "Що відбувається з лампами, які слідують Afterglow, коли цей ПК вимикається, перезапускається, виходить із сеансу або засинає. Лампи, які увімкнув Afterglow, вимикаються (якщо не вибрано власне налаштування), а лампи, які ви змінили самі, лишаються як є.",
            [nameof(HueShutdownLeaveOption)] = "Лишити лампи як є",
            [nameof(HueShutdownRestoreOption)] = "Повернути лампи як було",
            [nameof(HueShutdownTurnOffOption)] = "Вимкнути лампи",
            [nameof(HuePauseActionLabel)] = "Коли освітлення призупинено",
            [nameof(HuePauseActionTooltip)] =
                "Що роблять лампи, які слідують Afterglow, поки призупинені: із системного лотка чи гарячою клавішею, поки Afterglow призупинено або разом з екраном. Лампи, вимкнені на час паузи, потім вмикаються знову.",
            [nameof(HueCustomOption)] = "Власне налаштування",
            [nameof(HueLookTemperatureLabel)] = "Температура",
            [nameof(HueLookTemperatureTooltip)] =
                "Колірна температура самих ламп (кольорові лампи можуть бути трохи теплішими за білі). Утримуйте кнопку миші на повзунку, щоб побачити її на лампах; вони повернуться, щойно ви відпустите.",
            [nameof(HueLookBrightnessLabel)] = "Яскравість",
            [nameof(HueLookBrightnessTooltip)] =
                "Наскільки яскраві лампи. Утримуйте кнопку миші на повзунку, щоб побачити це на лампах; вони повернуться, щойно ви відпустите.",
            [nameof(HueTurnOnAtStartLabel)] = "Вмикати освітлення під час запуску Afterglow",
            [nameof(HueTurnOnAtStartTooltip)] =
                "Вмикає всі лампи у вибраних кімнатах (крім виключених), коли запускається Afterglow, як-от під час запуску ПК чи входу в систему, і коли ПК прокидається зі сну",
            [nameof(HueReclaimLabel)] = "Коли лампи змінено деінде",
            [nameof(HueReclaimTooltip)] =
                "Коли лампу змінено поза Afterglow (у застосунку Hue, вимикачем, сценою чи голосовим помічником), її лишають як є; ось як довго, рахуючи від останньої зміни. «Слідувати знову» у списку ламп одразу знову вмикає слідування.",
            [nameof(HueReclaimNeverOption)] = "Лишати як є, доки не вимкнуть і не ввімкнуть",
            [nameof(HueLightLeftAloneUntilText)] = "Змінено деінде, знову слідуватиме о {0}",
            [nameof(SmartLightsTabName)] = "Розумне освітлення",
            [nameof(HueReclaimAfterOption)] = "Знову слідувати через заданий час",
            [nameof(HueReclaimAfterLabel)] = "Через",
            [nameof(MinutesValueText)] = "{0} хв",
            [nameof(HoursValueText)] = "{0} год",
            [nameof(HoursMinutesValueText)] = "{0} год {1} хв",
            [nameof(HueFollowingCardTitle)] = "Слідування",
            [nameof(HueFollowingCardSubtitle)] =
                "Що лампи переймають від екрана, і наскільки вони тепліші й тьмяніші поруч із ним.",
            [nameof(HueReclaimCardTitle)] = "Зміни деінде",
            [nameof(HueReclaimCardSubtitle)] =
                "Лампи, змінені в застосунку Hue, вимикачем, сценою чи голосовим помічником, деякий час лишаються як є.",
            [nameof(HuePausingCardTitle)] = "Пауза",
            [nameof(HuePausingCardSubtitle)] = "Що роблять лампи, поки призупинені.",
            [nameof(HueStartupCardTitle)] = "Запуск і вимкнення",
            [nameof(HueStartupCardSubtitle)] =
                "Що роблять лампи під час запуску Afterglow, і коли ПК вимикається або засинає.",
            [nameof(HueUnreachableText)] =
                "Не вдалося з’єднатися з мостом Hue. Нова спроба незабаром.",
            [nameof(HueUnauthorizedText)] =
                "Міст Hue більше не приймає Afterglow. Натисніть «Під’єднати знову», а потім кнопку на мосту.",
            [nameof(HueCertificateChangedText)] =
                "За цією адресою відповів не ваш міст Hue. Якщо ви замінили міст, натисніть «Під’єднати знову».",
            [nameof(NeverCheckedText)] = "Ще не перевірялося",
            [nameof(UpdateCheckingText)] = "Перевірка оновлень…",
            [nameof(UpToDateText)] = "{0} {1} — найновіша версія.",
            [nameof(UpdateCheckFailedText)] =
                "Не вдалося перевірити оновлення. Перевірте підключення до інтернету та спробуйте ще раз.",
            [nameof(UpdateCheckUnavailableText)] = "Збірки для розробки не перевіряють оновлення.",
            [nameof(WhitelistedApplicationsPauseLabel)] = "Призупиняти для програм з білого списку",
            [nameof(WhitelistedApplicationsPauseTooltip)] =
                "Призупиняти, поки вікно на передньому плані належить одній з вибраних програм",
            [nameof(WhitelistedWindowTitlesPauseLabel)] =
                "Призупиняти для заголовків вікон з білого списку",
            [nameof(WhitelistedWindowTitlesPauseTooltip)] =
                "Призупиняти, поки заголовок вікна на передньому плані містить одне з ключових слів (без урахування регістру)",
            [nameof(WindowTitleKeywordPlaceholder)] = "Ключове слово заголовка, напр. Netflix",
            [nameof(AddButton)] = "ДОДАТИ",
            [nameof(RemoveWindowTitleTooltip)] = "Видалити ключове слово",
            [nameof(PerMonitorWhitelistPauseLabel)] =
                "Призупиняти лише монітор із відповідним вікном",
            [nameof(PerMonitorWhitelistPauseTooltip)] =
                "Інші монітори залишаються налаштованими. Якщо вимкнено, призупиняються всі.",
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
                "Застосовувати Afterglow до цього дисплея. Виключений дисплей залишається з нейтральними кольорами.",
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
                "Ховати Afterglow в системний трей під час запуску, навіть якщо його запущено вручну",
            [nameof(ExtendedGammaRangeLabel)] = "Розширений діапазон гами",
            [nameof(ExtendedGammaRangeTooltip)] =
                "Розблокувати повний діапазон гами в Windows, що дозволяє тепліші колірні температури та нижчу яскравість. Для зміни потрібні права адміністратора.",
            [nameof(GammaPollingIntervalLabel)] = "Оновлювати кожні",
            [nameof(GammaPollingIntervalTooltip)] =
                "Як часто гама примусово оновлюється, коли увімкнено оновлення гами",
            [nameof(SecondsValueText)] = "{0} с",
            // Tray menu
            [nameof(TrayOpenMenuItem)] = "Відкрити Afterglow",
            [nameof(TrayPauseMenuItem)] = "Призупинити",
            [nameof(TrayResumeMenuItem)] = "Продовжити",
            [nameof(TrayPreviewMenuItem)] = "Демонстрація 24-годинного циклу",
            [nameof(PauseFor15MinutesChip)] = "15 хв",
            [nameof(PauseFor1HourChip)] = "1 год",
            [nameof(PauseFor3HoursChip)] = "3 год",
            [nameof(PausedStateLabel)] = "Призупинено",
            // Grayscale
            [nameof(TrayGrayscaleMenuItem)] = "Відтінки сірого",
            [nameof(ToggleGrayscaleHotkeyLabel)] = "Відтінки сірого",
            [nameof(ToggleGrayscaleHotkeyTooltip)] =
                "Глобальна гаряча клавіша для ввімкнення/вимкнення відтінків сірого",
            [nameof(ToggleLightsPauseHotkeyLabel)] = "Призупинити/відновити лампи Hue",
            [nameof(ToggleLightsPauseHotkeyTooltip)] =
                "Глобальна гаряча клавіша, щоб призупинити лампи Hue (повернувши їх до попереднього стану) або відновити їх",
            [nameof(GrayscaleCardTitle)] = "Відтінки сірого",
            [nameof(GrayscaleCardSubtitle)] = "Прибрати кольори з екрана вручну або за розкладом.",
            [nameof(GrayscaleScheduleLabel)] = "Автоматичні відтінки сірого",
            [nameof(GrayscaleScheduleTooltip)] =
                "Коли відтінки сірого вмикаються самі. Їх завжди можна перемкнути вручну, і ручний вибір діє до наступної зміни за розкладом.",
            [nameof(GrayscaleScheduleOffOption)] = "Вимк.",
            [nameof(GrayscaleScheduleNightOption)] = "Лише вночі",
            [nameof(GrayscaleScheduleEveningAndNightOption)] = "Увечері та вночі",
            [nameof(GrayscaleTemperatureShiftLabel)] =
                "Змінювати колірну температуру у відтінках сірого",
            [nameof(GrayscaleTemperatureShiftTooltip)] =
                "Продовжувати застосовувати колірну температуру поверх відтінків сірого для теплого бурштинового монохромного вигляду. Якщо вимкнено, використовується нейтральна точка білого, а яскравість і далі застосовується.",
            [nameof(GrayscaleScheduleOnOption)] = "Увімк.",
            [nameof(TrayResumeGrayscaleMenuItem)] = "Відновити відтінки сірого",
        };
}
