using System.Collections.Generic;

namespace Afterglow.Localization;

public partial class LocalizationManager
{
    private static readonly IReadOnlyDictionary<string, string> ChineseTraditionalLocalization =
        new Dictionary<string, string>
        {
            // Dashboard
            [nameof(SunsetLabel)] = "日落",
            [nameof(SunriseLabel)] = "日出",
            [nameof(OffsetTooltipHeader)] = "目前的色溫與亮度值已透過微調值進行調整：",
            [nameof(TemperatureOffsetLabel)] = "色溫微調：",
            [nameof(BrightnessOffsetLabel)] = "亮度微調：",
            [nameof(ClickToResetLabel)] = "點擊重設",
            [nameof(OffsetLabel)] = "微調",
            // Main window
            [nameof(ToggleAfterglowTooltip)] = "切換 Afterglow 開啟/關閉",
            [nameof(HideToTrayTooltip)] = "將 Afterglow 隱藏至系統工作列",
            [nameof(PreviewText)] = "預覽",
            [nameof(StopPreviewTooltip)] = "停止預覽",
            [nameof(StartPreviewTooltip)] = "預覽 24 小時週期",
            [nameof(OpenSettingsTooltip)] = "開啟設定",
            // Settings dialog
            [nameof(ResetTooltip)] = "將所有設定重設為預設值",
            // Settings tabs
            // Advanced settings tab
            [nameof(LanguageLabel)] = "語言",
            [nameof(LanguageTooltip)] = "偏好的使用者介面語言",
            [nameof(StartWithWindowsLabel)] = "隨 Windows 啟動",
            [nameof(StartWithWindowsTooltip)] = "在 Windows 啟動時自動執行 Afterglow",
            [nameof(DefaultToDayConfigLabel)] = "預設採用白天設定",
            [nameof(DefaultToDayConfigTooltip)] =
                "當 Afterglow 停用或暫停時，還原為設定的白天色溫與亮度，而非預設的顯示器 Gamma 值",
            [nameof(PauseWhenFullscreenLabel)] = "全螢幕時暫停",
            [nameof(PauseWhenFullscreenTooltip)] = "當前景有任何全螢幕視窗時暫停 Afterglow",
            [nameof(GammaSmoothingLabel)] = "Gamma 平滑化",
            [nameof(GammaSmoothingTooltip)] =
                "在啟用或停用 Afterglow 時緩慢過渡，以便讓眼睛有時間適應",
            [nameof(GammaPollingLabel)] = "Gamma 輪詢",
            [nameof(GammaPollingTooltip)] =
                "定期強制重新整理顯示器 Gamma 值，以防止其他程式覆蓋此設定",
            // General settings tab
            [nameof(DayTemperatureTooltip)] = "白天時的色溫",
            [nameof(NightTemperatureTooltip)] = "夜晚時的色溫",
            [nameof(DayBrightnessTooltip)] = """
                白天時的亮度

                請注意，此亮度設定適用於色彩 Gamma 值，而非顯示器的實際硬體亮度。
                如果您的電腦已經能夠根據光線條件自動調整螢幕亮度（常見於筆記型電腦），建議將兩種亮度設定都保持在 100% 以停用 Afterglow 的亮度控制。
                """,
            [nameof(NightBrightnessTooltip)] = """
                夜晚時的亮度

                請注意，此亮度設定適用於色彩 Gamma 值，而非顯示器的實際硬體亮度。
                如果您的電腦已經能夠根據光線條件自動調整螢幕亮度（常見於筆記型電腦），建議將兩種亮度設定都保持在 100% 以停用 Afterglow 的亮度控制。
                """,
            [nameof(TransitionDurationTooltip)] = "在白天和夜晚設定之間切換所需的時間長度",
            [nameof(TransitionOffsetTooltip)] =
                "相對於日出和日落，指定過渡效果提前或延後開始的偏移時間",
            // Location settings tab
            [nameof(ManualLabel)] = "手動",
            [nameof(ManualTooltip)] = "手動設定日出與日落時間",
            [nameof(LocationBasedLabel)] = "基於位置",
            [nameof(LocationBasedTooltip)] = "設定您的位置並自動計算日出與日落時間",
            [nameof(YourLocationLabel)] = "您的位置：",
            [nameof(AutoDetectLocationTooltip)] = "嘗試根據您的 IP 位址自動偵測位置",
            [nameof(LocationQueryTooltip)] = """
                使用地理座標或搜尋關鍵字來指定您的位置

                有效輸入範例：
                41.25, -120.9762
                41.25°N, 120.9762°W
                New York, USA
                Germany
                """,
            [nameof(SetLocationTooltip)] = "設定位置",
            [nameof(LocationErrorText)] = "解析位置時發生錯誤，請再試一次",
            // Hot key settings tab
            [nameof(ToggleAfterglowHotkeyLabel)] = "切換 Afterglow",
            [nameof(ToggleAfterglowHotkeyTooltip)] = "全域快捷鍵，用於開啟/關閉 Afterglow",
            [nameof(ToggleWindowLabel)] = "切換視窗",
            [nameof(ToggleWindowHotkeyTooltip)] = "全域快捷鍵，用於顯示/隱藏 Afterglow 主視窗",
            [nameof(IncreaseTemperatureOffsetLabel)] = "色溫微調 ↑",
            [nameof(IncreaseTemperatureOffsetTooltip)] = "全域快捷鍵，用於增加目前的色溫微調值",
            [nameof(DecreaseTemperatureOffsetLabel)] = "色溫微調 ↓",
            [nameof(DecreaseTemperatureOffsetTooltip)] = "全域快捷鍵，用於減少目前的色溫微調值",
            [nameof(IncreaseBrightnessOffsetLabel)] = "亮度微調 ↑",
            [nameof(IncreaseBrightnessOffsetTooltip)] = "全域快捷鍵，用於增加目前的亮度微調值",
            [nameof(DecreaseBrightnessOffsetLabel)] = "亮度微調 ↓",
            [nameof(DecreaseBrightnessOffsetTooltip)] = "全域快捷鍵，用於減少目前的亮度微調值",
            [nameof(ResetOffsetLabel)] = "重設微調",
            [nameof(ResetOffsetHotkeyTooltip)] = "全域快捷鍵，用於重設目前的色溫與亮度微調值",
            // Application whitelist settings tab
            [nameof(AppWhitelistLabel)] = "應用程式白名單",
            [nameof(RefreshAppsTooltip)] = "重新整理執行中的應用程式",
            [nameof(PauseForWhitelistedTooltip)] =
                "當選定的應用程式或視窗標題之一位於前景時暫停 Afterglow",
            // Tray icon context menu
            [nameof(TraySettingsMenuItem)] = "設定",
            [nameof(TrayDisableUntilSunriseMenuItem)] = "直到日出",
            [nameof(TrayExitMenuItem)] = "結束",
            [nameof(TrayTooltipDisabled)] = "已停用",
            // Dialog messages
            [nameof(CloseButton)] = "關閉",
            [nameof(UnstableBuildTitle)] = "不穩定版本警告",
            [nameof(UnstableBuildMessage)] = """
                您正在使用 {0} 的開發版本。這些版本尚未經過充分測試，可能包含 Bug。

                如果您想切換至穩定發行版，可以從發行頁面下載。
                """,
            [nameof(SeeReleasesButton)] = "查看發行版本",
            [nameof(LimitedGammaRangeTitle)] = "Gamma 範圍受限",
            [nameof(LimitedGammaRangeMessage)] = """
                {0} 偵測到此系統上未啟用延伸 Gamma 範圍控制。
                這可能會導致某些色彩設定無法正常運作。

                按一下修復以解鎖 Gamma 範圍。可能需要系統管理員權限。
                """,
            [nameof(FixButton)] = "修復",
            [nameof(WelcomeTitle)] = "歡迎！",
            [nameof(WelcomeMessage)] = """
                感謝您安裝 {0}！
                為了獲得最個人化的體驗，請設定您偏好的太陽週期設定。

                按一下確定以開啟設定。
                """,
            [nameof(OkButton)] = "確定",
            [nameof(AlreadyRunningTitle)] = "已在執行",
            [nameof(AlreadyRunningMessage)] =
                "{0} 的另一個副本或原版 {1} 已在執行。請透過系統匣中的圖示將其關閉，然後重新啟動 {0}。",
            [nameof(TemporaryLocationNote)] =
                "{0} 正從暫存資料夾（例如「下載」）執行，因此不會從該位置隨 Windows 啟動。若要保留它，請使用安裝程式，或將 {1} 移至固定的資料夾並從那裡執行。",
            // Dashboard
            [nameof(BedtimeLabel)] = "就寢時間",
            [nameof(DayStateLabel)] = "白天",
            [nameof(EveningStateLabel)] = "傍晚",
            [nameof(NightStateLabel)] = "夜間",
            [nameof(SunriseTransitionPhaseLabel)] = "日出過渡",
            [nameof(SunsetTransitionPhaseLabel)] = "日落過渡",
            [nameof(NightTransitionPhaseLabel)] = "就寢過渡",
            [nameof(StatusDayScheduleText)] = "白天排程已啟用，色溫 {0} K，亮度 {1}%。",
            [nameof(StatusEveningScheduleText)] = "傍晚排程已啟用，色溫 {0} K，亮度 {1}%。",
            [nameof(StatusNightScheduleText)] = "夜間排程已啟用，色溫 {0} K，亮度 {1}%。",
            [nameof(StatusWarmingText)] = "正在調暖至 {0} K，{2}。",
            [nameof(StatusCoolingText)] = "正在調冷至 {0} K，{2}。",
            [nameof(StatusDimmingText)] = "正在調暗至 {1}%，{2}。",
            [nameof(StatusBrighteningText)] = "正在調亮至 {1}%，{2}。",
            [nameof(StatusWarmingDimmingText)] = "正在調暖至 {0} K 並調暗至 {1}%，{2}。",
            [nameof(StatusWarmingBrighteningText)] = "正在調暖至 {0} K 並調亮至 {1}%，{2}。",
            [nameof(StatusCoolingDimmingText)] = "正在調冷至 {0} K 並調暗至 {1}%，{2}。",
            [nameof(StatusCoolingBrighteningText)] = "正在調冷至 {0} K 並調亮至 {1}%，{2}。",
            [nameof(StatusForDayText)] = "迎接白天",
            [nameof(StatusForEveningText)] = "迎接傍晚",
            [nameof(StatusForNightText)] = "迎接夜間",
            [nameof(StatusManualOverrideText)] = "手動調整已生效，色溫 {0} K，亮度 {1}%。",
            [nameof(StatusPausedRemoteStreamingText)] = "因遠端串流已暫停，已恢復原生色彩。",
            [nameof(StatusPausedFullScreenText)] = "全螢幕應用程式執行期間已暫停。",
            [nameof(StatusPausedScreenLockedText)] = "螢幕鎖定期間已暫停。",
            [nameof(StatusPausedManuallyText)] = "已手動暫停，自動排程已中止。",
            [nameof(StatusPausedAutomaticallyText)] = "已自動暫停，已恢復原生色彩。",
            [nameof(StatusPausedRemoteStreamingDayText)] = "因遠端串流已暫停，已恢復白天設定。",
            [nameof(StatusPausedAutomaticallyDayText)] = "已自動暫停，已恢復白天設定。",
            [nameof(StatusGrayscaleText)] = "灰階模式已啟用，色溫 {0} K，亮度 {1}%。",
            [nameof(StatusGrayscaleNeutralText)] = "灰階模式已啟用，使用中性白點。",
            [nameof(StatusGrayscaleSnoozedText)] = "灰階已暫時停用，剩餘 {0}。",
            [nameof(StatusGrayscaleHeldText)] = "灰階已停用，直到下次排程變化。",
            [nameof(StatusPreviewText)] = "正在預覽接下來的 24 小時…",
            [nameof(PausedRemoteStreamingLabel)] = "已暫停（遠端串流）",
            [nameof(PausedScreenLockedLabel)] = "已暫停（螢幕已鎖定）",
            [nameof(PausedFullScreenLabel)] = "已暫停（全螢幕應用程式）",
            [nameof(PausedWhitelistLabel)] = "已暫停（白名單應用程式）",
            [nameof(PausedCaption)] = "{0} · {1} K（亮度 {2}）",
            [nameof(SolarEventPastText)] = "{0}：{1}前",
            [nameof(SolarEventFutureText)] = "{0}：{1}後",
            [nameof(SolarEventNowText)] = "{0}：現在",
            [nameof(DurationMinuteText)] = "1 分鐘",
            [nameof(DurationMinutesText)] = "{0} 分鐘",
            [nameof(DurationHourText)] = "1 小時",
            [nameof(DurationHoursText)] = "{0} 小時",
            [nameof(ActivePhaseCaption)] = "目前階段：{0} · {1} K（亮度 {2}）",
            [nameof(ActiveStageTemperatureTooltip)] = "拖曳以調整目前階段的色溫（{0}）",
            [nameof(ManualScheduleText)] = "手動排程",
            [nameof(LocationNotSetText)] = "位置：未設定",
            [nameof(LocationCoordinatesText)] = "位置：{0}",
            [nameof(OpenLocationSettingsTooltip)] = "變更位置設定",
            // Cycle
            [nameof(EveningStageTooltip)] =
                "從日落到夜間開始時間使用一個中間的傍晚設定，而不是在日落時直接切換到夜間設定",
            [nameof(EveningTemperatureTooltip)] = "日落到夜間開始時間之間的色溫",
            [nameof(EveningBrightnessTooltip)] = """
                日落到夜間開始時間之間的亮度

                請注意，此亮度設定作用於色彩 Gamma，而非顯示器的實際亮度。
                """,
            [nameof(NightStartTimeTooltip)] = """
                從傍晚設定過渡到夜間設定的開始時間（與日落相同，會依過渡微調值調整）

                如果夜間在日落過渡結束之前就開始，當天將略過傍晚階段。
                """,
            // Settings page
            [nameof(BackTooltip)] = "返回儀表板（變更已儲存）",
            [nameof(CycleTabName)] = "週期與色彩",
            [nameof(ScheduleTabName)] = "排程與位置",
            [nameof(RulesTabName)] = "規則與白名單",
            [nameof(ControlsTabName)] = "應用程式與控制",
            [nameof(DayCardSubtitle)] = "日出到日落",
            [nameof(EveningCardSubtitle)] = "日落到就寢時間",
            [nameof(NightCardSubtitle)] = "就寢時間到日出",
            [nameof(TemperatureLabel)] = "色溫",
            [nameof(BrightnessLabel)] = "亮度",
            [nameof(BedtimeNightStartLabel)] = "就寢時間（夜間開始）",
            [nameof(TransitionsCardTitle)] = "過渡",
            [nameof(DurationLabel)] = "時長",
            [nameof(TransitionOffsetShortLabel)] = "微調",
            [nameof(LocationCardTitle)] = "位置與座標",
            [nameof(LocationCardSubtitle)] =
                "用於計算日出與日落時間。可搜尋地點、輸入座標，或自動偵測。",
            [nameof(LocationInUseText)] = "正在使用 {0}, {1}",
            [nameof(SolarScheduleCardTitle)] = "日照排程",
            [nameof(SolarScheduleCardSubtitle)] = "根據您的位置計算日出與日落時間，或手動設定。",
            [nameof(OffLabel)] = "關閉",
            // Rules & whitelist
            [nameof(AutoPauseCardTitle)] = "智慧自動暫停",
            [nameof(AutoPauseCardSubtitle)] = "在以下情況下暫時恢復正常色彩。",
            [nameof(PauseWhenScreenLockedLabel)] = "螢幕鎖定時暫停",
            [nameof(PauseWhenScreenLockedTooltip)] = "在電腦鎖定期間暫停 Afterglow",
            [nameof(WhitelistCardSubtitle)] =
                "當任一選取的程式或相符的視窗位於前景時暫停 Afterglow。",
            [nameof(UpdateAvailableButton)] = "有可用的更新",
            [nameof(UpdateAvailableTooltip)] = "{0} {1} 已發布。按一下即可更新。",
            [nameof(UpdateDownloadingText)] = "正在下載更新… {0}",
            [nameof(UpdateInstallingText)] = "正在安裝更新…",
            [nameof(UpdateFailedButton)] = "手動更新",
            [nameof(UpdateFailedTooltip)] = "{0} 無法自動更新。按一下以開啟下載頁面。",
            [nameof(CheckForUpdatesLabel)] = "檢查更新",
            [nameof(CheckForUpdatesTooltip)] =
                "不時在 GitHub 上檢查是否有新版本。不會自動下載任何內容。",
            [nameof(CheckForUpdatesNowTooltip)] = "立即檢查更新",
            [nameof(CheckNowButton)] = "立即檢查",
            [nameof(UpdateCheckNeverOption)] = "從不",
            [nameof(UpdateCheckDailyOption)] = "每天",
            [nameof(UpdateCheckWeeklyOption)] = "每週",
            [nameof(UpdateCheckEveryTwoWeeksOption)] = "每兩週",
            [nameof(CurrentVersionText)] = "版本 {0}",
            [nameof(LastUpdateCheckText)] = "上次檢查：{0}",
            [nameof(NeverCheckedText)] = "尚未檢查",
            [nameof(UpdateCheckingText)] = "正在檢查更新…",
            [nameof(UpToDateText)] = "{0} {1} 已是最新版本。",
            [nameof(UpdateCheckFailedText)] = "無法檢查更新。請檢查網路連線後再試一次。",
            [nameof(UpdateCheckUnavailableText)] = "開發版本不會檢查更新。",
            [nameof(WhitelistedApplicationsPauseLabel)] = "為白名單應用程式暫停",
            [nameof(WhitelistedApplicationsPauseTooltip)] = "當前景視窗屬於所選應用程式之一時暫停",
            [nameof(WhitelistedWindowTitlesPauseLabel)] = "為白名單視窗標題暫停",
            [nameof(WhitelistedWindowTitlesPauseTooltip)] =
                "當前景視窗的標題包含任一關鍵字時暫停（不區分大小寫）",
            [nameof(WindowTitleKeywordPlaceholder)] = "標題關鍵字，例如 Netflix",
            [nameof(AddButton)] = "新增",
            [nameof(RemoveWindowTitleTooltip)] = "移除關鍵字",
            [nameof(PerMonitorWhitelistPauseLabel)] = "僅暫停顯示相符視窗的顯示器",
            [nameof(PerMonitorWhitelistPauseTooltip)] =
                "其他顯示器保持調整。關閉時將暫停所有顯示器。",
            [nameof(PerMonitorFullscreenPauseLabel)] = "僅暫停執行全螢幕應用程式的顯示器",
            [nameof(PerMonitorFullscreenPauseTooltip)] =
                "當全螢幕應用程式位於前景時，僅在執行該應用程式的顯示器上恢復正常色彩，其他顯示器繼續跟隨週期",
            [nameof(StreamingAutoPauseLabel)] = "遠端串流時暫停（Sunshine / RDP）",
            [nameof(StreamingAutoPauseTooltip)] =
                "當這台電腦正透過 Sunshine 串流或透過遠端桌面存取時，在所有顯示器上恢復正常色彩",
            [nameof(StreamingPortsLabel)] = "串流 TCP 連接埠",
            [nameof(StreamingPortsTooltip)] =
                "遠端桌面會在任何連接埠上自動偵測。Sunshine 與其他串流應用程式則透過這些本機 TCP 連接埠之一上的作用中連線來偵測，連接埠之間以逗號分隔（例如 Sunshine 為 48010，或填入 48010, 27036 以同時包含 Steam Remote Play）。",
            [nameof(DisplaysCardTitle)] = "已連接的顯示器",
            [nameof(DisplaysCardSubtitle)] =
                "排除顯示器或個別微調其暖色。顯示器重新連接後會再次被辨識。",
            [nameof(IdentifyButton)] = "識別",
            [nameof(IdentifyTooltip)] = "在每台顯示器的螢幕上顯示其編號",
            [nameof(OffsetScalingLabel)] = "微調縮放",
            [nameof(OffsetScalingTooltip)] = "隨著週期從白天進入夜間，每台顯示器的色溫微調如何套用",
            [nameof(PerceptualOffsetScalingOption)] = "Mired（感知）",
            [nameof(FixedOffsetScalingOption)] = "克耳文（固定）",
            [nameof(PerceptualOffsetScalingDescription)] =
                "色溫微調在您的白天色溫下精確生效，並在傍晚與夜間依感知比例縮放，讓不同類型的面板在視覺上保持一致。",
            [nameof(FixedOffsetScalingDescription)] =
                "色溫微調一律以相同的固定克耳文值套用，不隨白天到夜間縮放。",
            [nameof(NoDisplaysText)] = "未偵測到顯示器。",
            [nameof(PrimaryBadge)] = "主顯示器",
            [nameof(DisplayEnabledTooltip)] =
                "對此顯示器套用 Afterglow。排除後，該顯示器將維持中性色彩。",
            [nameof(MonitorTemperatureOffsetLabel)] = "色溫微調",
            [nameof(MonitorTemperatureOffsetTooltip)] =
                "僅調整此顯示器的色溫。使用感知縮放時，這是在您的白天色溫下的微調量。",
            [nameof(MonitorBrightnessOffsetLabel)] = "亮度微調",
            [nameof(MonitorBrightnessOffsetTooltip)] = "僅在此顯示器上加到目前亮度",
            [nameof(ResetOffsetsTooltip)] = "重設微調",
            [nameof(ReferenceDisplayNote)] = "參考顯示器 · 跟隨「週期與色彩」主排程",
            [nameof(EffectiveNowText)] = "目前生效：{0} K · {1}",
            [nameof(ExcludedNeutralText)] = "已排除 · 中性 6600 K",
            [nameof(DisconnectedDisplaysLabel)] = "已儲存的中斷連線顯示器（{0}）",
            [nameof(ForgetButton)] = "忘記",
            [nameof(ForgetDisplayTooltip)] = "移除此顯示器已儲存的偏好設定",
            // Controls & app
            [nameof(HotkeysCardTitle)] = "全域快捷鍵",
            [nameof(HotkeysCardSubtitle)] = "點擊欄位並按下按鍵組合即可指派。",
            [nameof(SystemPreferencesCardTitle)] = "系統偏好設定",
            [nameof(StartMinimizedLabel)] = "啟動時最小化",
            [nameof(StartMinimizedTooltip)] = "啟動時將 Afterglow 隱藏到系統匣，即使是手動啟動",
            [nameof(ExtendedGammaRangeLabel)] = "延伸 Gamma 範圍",
            [nameof(ExtendedGammaRangeTooltip)] =
                "解除 Windows 中的完整 Gamma 範圍，以支援更暖的色溫與更低的亮度。變更此項需要系統管理員權限。",
            [nameof(GammaPollingIntervalLabel)] = "重新整理間隔",
            [nameof(GammaPollingIntervalTooltip)] = "啟用 Gamma 輪詢時，強制重新整理 Gamma 的頻率",
            [nameof(SecondsValueText)] = "{0} 秒",
            // Tray menu
            [nameof(TrayOpenMenuItem)] = "開啟 Afterglow",
            [nameof(TrayPauseMenuItem)] = "暫停",
            [nameof(TrayResumeMenuItem)] = "繼續",
            [nameof(TrayPreviewMenuItem)] = "預覽 24 小時週期",
            [nameof(PauseFor15MinutesChip)] = "15 分鐘",
            [nameof(PauseFor1HourChip)] = "1 小時",
            [nameof(PauseFor3HoursChip)] = "3 小時",
            [nameof(PausedStateLabel)] = "已暫停",
            // Grayscale
            [nameof(TrayGrayscaleMenuItem)] = "灰階模式",
            [nameof(ToggleGrayscaleHotkeyLabel)] = "切換灰階模式",
            [nameof(ToggleGrayscaleHotkeyTooltip)] = "用於開啟/關閉灰階模式的全域快捷鍵",
            [nameof(GrayscaleCardTitle)] = "灰階模式",
            [nameof(GrayscaleCardSubtitle)] = "手動或依排程移除螢幕色彩。",
            [nameof(GrayscaleScheduleLabel)] = "自動灰階",
            [nameof(GrayscaleScheduleTooltip)] =
                "灰階模式何時自動開啟。它隨時都能手動切換，手動選擇會持續到排程下一次變化為止。",
            [nameof(GrayscaleScheduleOffOption)] = "關閉",
            [nameof(GrayscaleScheduleNightOption)] = "僅夜間",
            [nameof(GrayscaleScheduleEveningAndNightOption)] = "傍晚與夜間",
            [nameof(GrayscaleTemperatureShiftLabel)] = "灰階模式下套用色溫",
            [nameof(GrayscaleTemperatureShiftTooltip)] =
                "在灰階之上繼續套用色溫，呈現溫暖的琥珀色單色效果。關閉後，灰階模式使用中性白點，亮度設定仍然生效。",
            [nameof(GrayscaleScheduleOnOption)] = "開啟",
            [nameof(TrayResumeGrayscaleMenuItem)] = "恢復灰階",
        };
}
