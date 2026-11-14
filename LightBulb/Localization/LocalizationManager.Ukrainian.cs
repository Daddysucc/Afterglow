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
            [nameof(CancelButton)] = "СКАСУВАТИ",
            [nameof(LanguageLabel)] = "Мова",
            [nameof(LanguageTooltip)] = "Бажана мова інтерфейсу",
            [nameof(StartWithWindowsLabel)] = "Запускати з Windows",
            [nameof(StartWithWindowsTooltip)] = "Запускати LightBulb при старті Windows",
            [nameof(AutoUpdateLabel)] = "Авто-оновлення",
            [nameof(AutoUpdateTooltip)] = "Автоматично оновлювати LightBulb до останньої версії",
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
            [nameof(UpdateAvailableTitle)] = "Доступне оновлення",
            [nameof(UpdateAvailableMessage)] = """
                Оновлення до {0} v{1} завантажено.
                Встановити зараз?
                """,
            [nameof(InstallButton)] = "ВСТАНОВИТИ",
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
        };
}
