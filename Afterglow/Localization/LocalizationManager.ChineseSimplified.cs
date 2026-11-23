using System.Collections.Generic;

namespace Afterglow.Localization;

public partial class LocalizationManager
{
    private static readonly IReadOnlyDictionary<string, string> ChineseSimplifiedLocalization =
        new Dictionary<string, string>
        {
            // Dashboard (仪表盘)
            [nameof(SunsetLabel)] = "日落",
            [nameof(SunriseLabel)] = "日出",
            [nameof(OffsetTooltipHeader)] = "当前的色温和亮度值已应用偏移量：",
            [nameof(TemperatureOffsetLabel)] = "色温偏移：",
            [nameof(BrightnessOffsetLabel)] = "亮度偏移：",
            [nameof(ClickToResetLabel)] = "点击重置",
            [nameof(OffsetLabel)] = "偏移",

            // Main window (主窗口)
            [nameof(ToggleAfterglowTooltip)] = "开启/关闭 Afterglow",
            [nameof(HideToTrayTooltip)] = "隐藏 Afterglow 到系统托盘",
            [nameof(PreviewText)] = "预览",
            [nameof(StopPreviewTooltip)] = "停止预览",
            [nameof(StartPreviewTooltip)] = "预览 24 小时循环",
            [nameof(OpenSettingsTooltip)] = "打开设置",

            // Settings dialog (设置对话框)
            [nameof(ResetTooltip)] = "将此选项卡上的设置恢复为默认值（其他选项卡保持不变）",
            [nameof(ResetTabSettingsButton)] = "重置“{0}”设置",

            // Settings tabs (设置选项卡)

            // Advanced settings tab (高级设置选项卡)
            [nameof(LanguageLabel)] = "语言",
            [nameof(LanguageTooltip)] = "首选用户界面语言",
            [nameof(StartWithWindowsLabel)] = "随 Windows 启动",
            [nameof(StartWithWindowsTooltip)] = "在 Windows 启动时运行 Afterglow",
            [nameof(DefaultToDayConfigLabel)] = "默认为日间配置",
            [nameof(DefaultToDayConfigTooltip)] =
                "当 Afterglow 被禁用或暂停时，恢复配置的日间色温和亮度，而不是默认的显示器伽马值",
            [nameof(PauseWhenFullscreenLabel)] = "全屏时暂停",
            [nameof(PauseWhenFullscreenTooltip)] = "当任何全屏窗口处于前台时暂停 Afterglow",
            [nameof(GammaSmoothingLabel)] = "伽马平滑",
            [nameof(GammaSmoothingTooltip)] = "在启用或禁用 Afterglow 时缓慢过渡，给眼睛适应的时间",
            [nameof(GammaPollingLabel)] = "伽马轮询",
            [nameof(GammaPollingTooltip)] = "定期强制刷新显示器伽马值，防止其他程序覆盖它",

            // General settings tab (常规设置选项卡)
            [nameof(DayTemperatureTooltip)] = "白天的色温",
            [nameof(NightTemperatureTooltip)] = "夜晚的色温",
            [nameof(DayBrightnessTooltip)] = """
                白天的亮度

                注意：此亮度设置应用于颜色伽马，而非显示器的实际物理亮度。
                如果您的计算机已经能够根据光照条件自动调整屏幕亮度（笔记本电脑常见），建议将两个亮度设置都保持在 100%，从而禁用 Afterglow 的亮度控制。
                """,
            [nameof(NightBrightnessTooltip)] = """
                夜晚的亮度

                注意：此亮度设置应用于颜色伽马，而非显示器的实际物理亮度。
                如果您的计算机已经能够根据光照条件自动调整屏幕亮度（笔记本电脑常见），建议将两个亮度设置都保持在 100%，从而禁用 Afterglow 的亮度控制。
                """,
            [nameof(TransitionDurationTooltip)] = "从日间配置切换到夜间配置所需的时间",
            [nameof(TransitionOffsetTooltip)] = "指定过渡开始时间相对于日出和日落的提前或延后量",

            // Location settings tab (位置设置选项卡)
            [nameof(ManualLabel)] = "手动",
            [nameof(ManualTooltip)] = "手动配置日出和日落时间",
            [nameof(LocationBasedLabel)] = "基于位置",
            [nameof(LocationBasedTooltip)] = "配置您的位置，并以此自动计算日出和日落时间",
            [nameof(YourLocationLabel)] = "您的位置：",
            [nameof(AutoDetectLocationTooltip)] = "尝试根据您的 IP 地址自动检测位置",
            [nameof(LocationQueryTooltip)] = """
                使用地理坐标或搜索查询指定您的位置

                有效输入示例：
                **41.25, -120.9762**
                **41.25°N, 120.9762°W**
                **New York, USA (美国纽约)**
                **Germany (德国)**
                """,
            [nameof(SetLocationTooltip)] = "设置位置",
            [nameof(LocationErrorText)] = "解析位置出错，请重试",

            // Hot key settings tab (快捷键设置选项卡)
            [nameof(ToggleAfterglowHotkeyLabel)] = "切换 Afterglow",
            [nameof(ToggleAfterglowHotkeyTooltip)] = "用于开启/关闭 Afterglow 的全局快捷键",
            [nameof(ToggleWindowLabel)] = "切换窗口",
            [nameof(ToggleWindowHotkeyTooltip)] = "用于显示/隐藏 Afterglow 主窗口的全局快捷键",
            [nameof(IncreaseTemperatureOffsetLabel)] = "增加色温偏移 ↑",
            [nameof(IncreaseTemperatureOffsetTooltip)] = "用于增加当前色温偏移量的全局快捷键",
            [nameof(DecreaseTemperatureOffsetLabel)] = "减少色温偏移 ↓",
            [nameof(DecreaseTemperatureOffsetTooltip)] = "用于减少当前色温偏移量的全局快捷键",
            [nameof(IncreaseBrightnessOffsetLabel)] = "增加亮度偏移 ↑",
            [nameof(IncreaseBrightnessOffsetTooltip)] = "用于增加当前亮度偏移量的全局快捷键",
            [nameof(DecreaseBrightnessOffsetLabel)] = "减少亮度偏移 ↓",
            [nameof(DecreaseBrightnessOffsetTooltip)] = "用于减少当前亮度偏移量的全局快捷键",
            [nameof(ResetOffsetLabel)] = "重置偏移",
            [nameof(ResetOffsetHotkeyTooltip)] = "用于重置当前色温和亮度偏移量的全局快捷键",

            // Application whitelist settings tab (应用白名单设置选项卡)
            [nameof(AppWhitelistLabel)] = "应用白名单",
            [nameof(RefreshAppsTooltip)] = "刷新正在运行的应用程序",
            [nameof(PauseForWhitelistedTooltip)] =
                "当选定的应用程序或窗口标题之一处于前台时暂停 Afterglow",

            // Tray icon context menu (托盘图标上下文菜单)
            [nameof(TraySettingsMenuItem)] = "设置",
            [nameof(TrayDisableUntilSunriseMenuItem)] = "直到日出",
            [nameof(TrayExitMenuItem)] = "退出",
            [nameof(TrayTooltipDisabled)] = "已禁用",

            // Dialog messages (对话框消息)
            [nameof(CloseButton)] = "关闭",
            [nameof(UnstableBuildTitle)] = "不稳定版本警告",
            [nameof(UnstableBuildMessage)] = """
                您正在使用 {0} 的开发版本。这些版本未经过充分测试，可能包含错误。

                如果您想切换到稳定版本，可以从发布页面下载。
                """,
            [nameof(SeeReleasesButton)] = "查看发布版本",
            [nameof(LimitedGammaRangeTitle)] = "伽马范围受限",
            [nameof(LimitedGammaRangeMessage)] = """
                {0} 检测到该系统未启用扩展伽马范围控制。
                这可能导致某些颜色配置无法正常工作。

                按下“修复”以解锁伽马范围。可能需要管理员权限。
                """,
            [nameof(FixButton)] = "修复",
            [nameof(WelcomeTitle)] = "欢迎使用！",
            [nameof(WelcomeMessage)] = """
                感谢您安装 {0}！
                为了获得最个性化的体验，请设置您首选的太阳配置。

                按下“确定”打开设置。
                """,
            [nameof(OkButton)] = "确定",
            [nameof(AlreadyRunningTitle)] = "已在运行",
            [nameof(AlreadyRunningMessage)] =
                "{0} 的另一个副本或原版 {1} 已在运行。请通过系统托盘中的图标将其关闭，然后重新启动 {0}。",
            [nameof(TemporaryLocationNote)] =
                "{0} 正在从临时文件夹（例如“下载”）运行，因此不会从该位置随 Windows 启动。若要保留它，请使用安装程序，或将 {1} 移动到固定的文件夹并从那里运行。",
            // Dashboard
            [nameof(BedtimeLabel)] = "就寝时间",
            [nameof(DayStateLabel)] = "白天",
            [nameof(EveningStateLabel)] = "傍晚",
            [nameof(NightStateLabel)] = "夜间",
            [nameof(SunriseTransitionPhaseLabel)] = "日出过渡",
            [nameof(SunsetTransitionPhaseLabel)] = "日落过渡",
            [nameof(NightTransitionPhaseLabel)] = "就寝过渡",
            [nameof(StatusDayScheduleText)] = "白天方案已启用，色温 {0} K，亮度 {1}%。",
            [nameof(StatusEveningScheduleText)] = "傍晚方案已启用，色温 {0} K，亮度 {1}%。",
            [nameof(StatusNightScheduleText)] = "夜间方案已启用，色温 {0} K，亮度 {1}%。",
            [nameof(StatusWarmingText)] = "正在调暖至 {0} K，{2}。",
            [nameof(StatusCoolingText)] = "正在调冷至 {0} K，{2}。",
            [nameof(StatusDimmingText)] = "正在调暗至 {1}%，{2}。",
            [nameof(StatusBrighteningText)] = "正在调亮至 {1}%，{2}。",
            [nameof(StatusWarmingDimmingText)] = "正在调暖至 {0} K 并调暗至 {1}%，{2}。",
            [nameof(StatusWarmingBrighteningText)] = "正在调暖至 {0} K 并调亮至 {1}%，{2}。",
            [nameof(StatusCoolingDimmingText)] = "正在调冷至 {0} K 并调暗至 {1}%，{2}。",
            [nameof(StatusCoolingBrighteningText)] = "正在调冷至 {0} K 并调亮至 {1}%，{2}。",
            [nameof(StatusForDayText)] = "迎接白天",
            [nameof(StatusForEveningText)] = "迎接傍晚",
            [nameof(StatusForNightText)] = "迎接夜间",
            [nameof(StatusManualOverrideText)] = "手动调整已生效，色温 {0} K，亮度 {1}%。",
            [nameof(StatusPausedRemoteStreamingText)] = "因远程串流已暂停，已恢复原生色彩。",
            [nameof(StatusPausedFullScreenText)] = "全屏应用运行期间已暂停。",
            [nameof(StatusPausedScreenLockedText)] = "屏幕锁定期间已暂停。",
            [nameof(StatusPausedManuallyText)] = "已手动暂停，自动方案已中止。",
            [nameof(StatusPausedAutomaticallyText)] = "已自动暂停，已恢复原生色彩。",
            [nameof(StatusPausedRemoteStreamingDayText)] = "因远程串流已暂停，已恢复白天配置。",
            [nameof(StatusPausedAutomaticallyDayText)] = "已自动暂停，已恢复白天配置。",
            [nameof(StatusGrayscaleText)] = "灰度模式已启用，色温 {0} K，亮度 {1}%。",
            [nameof(StatusGrayscaleNeutralText)] = "灰度模式已启用，使用中性白点。",
            [nameof(StatusGrayscaleSnoozedText)] = "灰度已暂时停用，剩余 {0}。",
            [nameof(StatusGrayscaleHeldText)] = "灰度已停用，直到下次时间表变化。",
            [nameof(StatusPreviewText)] = "正在预览接下来的 24 小时…",
            [nameof(PausedRemoteStreamingLabel)] = "已暂停（远程串流）",
            [nameof(PausedScreenLockedLabel)] = "已暂停（屏幕已锁定）",
            [nameof(PausedFullScreenLabel)] = "已暂停（全屏应用）",
            [nameof(PausedWhitelistLabel)] = "已暂停（白名单应用）",
            [nameof(PausedCaption)] = "{0} · {1} K（亮度 {2}）",
            [nameof(SolarEventPastText)] = "{0}：{1}前",
            [nameof(SolarEventFutureText)] = "{0}：{1}后",
            [nameof(SolarEventNowText)] = "{0}：现在",
            [nameof(DurationMinuteText)] = "1 分钟",
            [nameof(DurationMinutesText)] = "{0} 分钟",
            [nameof(DurationHourText)] = "1 小时",
            [nameof(DurationHoursText)] = "{0} 小时",
            [nameof(ActivePhaseCaption)] = "当前阶段：{0} · {1} K（亮度 {2}）",
            [nameof(ActiveStageTemperatureTooltip)] = "拖动以调整当前阶段的色温（{0}）",
            [nameof(ManualScheduleText)] = "手动时间表",
            [nameof(LocationNotSetText)] = "位置：未设置",
            [nameof(LocationCoordinatesText)] = "位置：{0}",
            [nameof(OpenLocationSettingsTooltip)] = "更改位置设置",
            // Cycle
            [nameof(EveningStageTooltip)] =
                "从日落到夜间开始时间使用一个中间的傍晚配置，而不是在日落时直接切换到夜间配置",
            [nameof(EveningTemperatureTooltip)] = "日落到夜间开始时间之间的色温",
            [nameof(EveningBrightnessTooltip)] = """
                日落到夜间开始时间之间的亮度

                请注意，此亮度设置作用于色彩伽马，而不是显示器的实际亮度。
                """,
            [nameof(NightStartTimeTooltip)] = """
                从傍晚配置过渡到夜间配置的开始时间（与日落一样，会根据过渡偏移进行调整）

                如果夜间在日落过渡结束之前就开始，当天将跳过傍晚阶段。
                """,
            // Settings page
            [nameof(BackTooltip)] = "返回仪表板（更改已保存）",
            [nameof(CycleTabName)] = "周期与色彩",
            [nameof(ScheduleTabName)] = "时间表与位置",
            [nameof(RulesTabName)] = "规则与白名单",
            [nameof(ControlsTabName)] = "应用与控制",
            [nameof(DayCardSubtitle)] = "日出到日落",
            [nameof(EveningCardSubtitle)] = "日落到就寝时间",
            [nameof(NightCardSubtitle)] = "就寝时间到日出",
            [nameof(TemperatureLabel)] = "色温",
            [nameof(BrightnessLabel)] = "亮度",
            [nameof(BedtimeNightStartLabel)] = "就寝时间（夜间开始）",
            [nameof(TransitionsCardTitle)] = "过渡",
            [nameof(DurationLabel)] = "时长",
            [nameof(TransitionOffsetShortLabel)] = "偏移",
            [nameof(LocationCardTitle)] = "位置与坐标",
            [nameof(LocationCardSubtitle)] =
                "用于计算日出和日落时间。可搜索地点、输入坐标，或自动检测。",
            [nameof(LocationInUseText)] = "正在使用 {0}, {1}",
            [nameof(SolarScheduleCardTitle)] = "日照时间表",
            [nameof(SolarScheduleCardSubtitle)] = "根据您的位置计算日出和日落时间，或手动设置。",
            [nameof(OffLabel)] = "关闭",
            // Rules & whitelist
            [nameof(AutoPauseCardTitle)] = "智能自动暂停",
            [nameof(AutoPauseCardSubtitle)] = "在以下情况下临时恢复正常色彩。",
            [nameof(PauseWhenScreenLockedLabel)] = "屏幕锁定时暂停",
            [nameof(PauseWhenScreenLockedTooltip)] = "在电脑锁定期间暂停 Afterglow",
            [nameof(WhitelistCardSubtitle)] =
                "当任一所选程序或匹配的窗口处于前台时暂停 Afterglow。",
            [nameof(UpdateAvailableButton)] = "有可用更新",
            [nameof(UpdateAvailableTooltip)] = "{0} {1} 已发布。点击即可更新。",
            [nameof(UpdateDownloadingText)] = "正在下载更新… {0}",
            [nameof(UpdateInstallingText)] = "正在安装更新…",
            [nameof(UpdateFailedButton)] = "手动更新",
            [nameof(UpdateFailedTooltip)] = "{0} 无法自动更新。点击打开下载页面。",
            [nameof(CheckForUpdatesLabel)] = "检查更新",
            [nameof(CheckForUpdatesTooltip)] =
                "不时在 GitHub 上检查是否有新版本。不会自动下载任何内容。",
            [nameof(CheckForUpdatesNowTooltip)] = "立即检查更新",
            [nameof(CheckNowButton)] = "立即检查",
            [nameof(UpdateCheckNeverOption)] = "从不",
            [nameof(UpdateCheckDailyOption)] = "每天",
            [nameof(UpdateCheckWeeklyOption)] = "每周",
            [nameof(UpdateCheckEveryTwoWeeksOption)] = "每两周",
            [nameof(CurrentVersionText)] = "版本 {0}",
            [nameof(LastUpdateCheckText)] = "上次检查：{0}",
            [nameof(HueCardTitle)] = "Philips Hue",
            [nameof(HueCardSubtitle)] =
                "在这台电脑开启时，让 Hue 灯跟随色彩周期，并且看起来和屏幕一样暖。",
            [nameof(HueEnabledLabel)] = "控制 Hue 灯",
            [nameof(HueEnabledTooltip)] =
                "所选房间中开着的灯会跟随色彩周期。灯不会被打开，并会在 Afterglow 停止时（包括电脑关机或睡眠时）恢复原状。你自己调整过的灯会保持不变，直到它被关掉再打开。",
            [nameof(HueBridgeLabel)] = "桥接器地址",
            [nameof(HueBridgeTooltip)] =
                "Hue 桥接器的 IP 地址（可在 Hue 应用的“设置”›“桥接器”中找到）。留空即可自动查找桥接器。",
            [nameof(HueBridgePlaceholder)] = "自动查找",
            [nameof(HueConnectButton)] = "连接",
            [nameof(HueReconnectButton)] = "重新连接",
            [nameof(HueDisconnectButton)] = "断开连接",
            [nameof(HueDisconnectTooltip)] =
                "将灯恢复原状并忘记此桥接器（重新连接需要再次按下其按钮）",
            [nameof(HueCancelButton)] = "取消",
            [nameof(HueConnectTooltip)] = "连接到桥接器：点击后，请在 30 秒内按下桥接器顶部的按钮",
            [nameof(HueRoomsLabel)] = "房间",
            [nameof(HueFollowTemperatureLabel)] = "跟随色温",
            [nameof(HueFollowTemperatureTooltip)] =
                "灯会采用周期的色温（在其支持的范围内，约 2000 K 到 6500 K）",
            [nameof(LightWarmthLabel)] = "暖度",
            [nameof(LightWarmthTooltip)] =
                "灯比屏幕设得暖多少，以便看起来和屏幕一样暖。可显示颜色的灯在暖白色用尽后会变为琥珀色和橙色。",
            [nameof(HueFollowBrightnessLabel)] = "跟随亮度",
            [nameof(HueFollowBrightnessTooltip)] =
                "灯会采用周期的亮度（Afterglow 的 100% 即为最大亮度）",
            [nameof(HuePauseWithScreenLabel)] = "随屏幕暂停",
            [nameof(HuePauseWithScreenTooltip)] =
                "屏幕自动暂停时（全屏应用、白名单应用、远程串流、锁屏），灯也会恢复到之前的状态",
            [nameof(LightDimmingLabel)] = "调暗",
            [nameof(LightDimmingTooltip)] =
                "屏幕变暗时灯变暗的程度。×1.0 时与屏幕亮度一致；数值越高灯越暗，满亮度始终保持满亮度。",
            [nameof(HueNotConnectedText)] = "点击“连接”，然后按下 Hue 桥接器上的按钮。",
            [nameof(HueSearchingText)] = "正在查找 Hue 桥接器…",
            [nameof(HueNoBridgeFoundText)] =
                "未找到 Hue 桥接器。请输入其 IP 地址，然后点击“连接”。",
            [nameof(HueWaitingForButtonText)] = "请按下 Hue 桥接器顶部的按钮…",
            [nameof(HueButtonNotPressedText)] = "未及时按下桥接器的按钮。请点击“连接”重试。",
            [nameof(HueConnectedText)] = "已连接到 Hue 桥接器。",
            [nameof(HueNoRoomsText)] = "已连接。请选择灯光跟随 Afterglow 的房间。",
            [nameof(HuePausedText)] = "已连接。Afterglow 重新开启后，灯会再次跟随它。",
            [nameof(HuePausedWithScreenText)] =
                "已连接。灯已随屏幕暂停，屏幕恢复后会再次跟随 Afterglow。",
            [nameof(HueFollowingText)] = "跟随 Afterglow 的灯：{0}",
            [nameof(HueFollowingUnresponsiveText)] =
                "跟随 Afterglow 的灯：{0}（{1} 个无响应，会不时重试）",
            [nameof(HueShowLightsTooltip)] = "显示或隐藏所选房间中的灯，以选择哪些灯跟随 Afterglow",
            [nameof(HueUnnamedLightText)] = "未命名的灯",
            [nameof(HueLightFollowingText)] = "正在跟随",
            [nameof(HueLightNotRespondingText)] = "无响应，会不时重试",
            [nameof(HueLightLeftAloneText)] = "已在别处更改，在关闭并重新打开之前保持不变",
            [nameof(HueLightOffText)] = "已关闭",
            [nameof(HueLightExcludedText)] = "已排除",
            [nameof(HueLightWaitingText)] = "尚未跟随",
            [nameof(HueFollowAgainButton)] = "重新跟随",
            [nameof(HueFollowAgainTooltip)] = "让这盏灯重新跟随 Afterglow",
            [nameof(HuePausedUntilText)] = "灯已暂停至 {0}，在此期间恢复为原来的状态。",
            [nameof(HuePausedUntilResumedText)] =
                "灯已暂停，直到从托盘恢复，在此期间恢复为原来的状态。",
            [nameof(TrayPauseLightsMenuItem)] = "暂停灯",
            [nameof(TrayResumeLightsMenuItem)] = "恢复灯",
            [nameof(TrayTurnOnLightsMenuItem)] = "打开灯 ({0})",
            [nameof(HueLightNotAdjustableText)] = "无法调节（只能开关）",
            [nameof(HueLightNothingFollowedText)] = "无可跟随（它只能做未被跟随的事）",
            [nameof(HueLightPausedText)] = "已暂停",
            [nameof(HueFollowingNoneText)] =
                "目前没有灯在跟随。所选房间中的灯在打开后就会跟随 Afterglow。",
            [nameof(HueShutdownLabel)] = "电脑关机或睡眠时",
            [nameof(HueShutdownTooltip)] =
                "这台电脑关机、重启、注销或睡眠时，跟随 Afterglow 的灯会如何。由 Afterglow 打开的灯会关闭（除非选择自定义），你自己调整过的灯保持不变。",
            [nameof(HueShutdownLeaveOption)] = "保持灯的现状",
            [nameof(HueShutdownRestoreOption)] = "将灯恢复原状",
            [nameof(HueShutdownTurnOffOption)] = "关灯",
            [nameof(HuePauseActionLabel)] = "灯暂停时",
            [nameof(HuePauseActionTooltip)] =
                "跟随 Afterglow 的灯在暂停时会如何：从托盘或用快捷键暂停、Afterglow 暂停时，或随屏幕暂停时。为暂停而关闭的灯之后会重新打开。",
            [nameof(HueCustomOption)] = "自定义",
            [nameof(HueLookTemperatureLabel)] = "色温",
            [nameof(HueLookTemperatureTooltip)] =
                "灯本身的色温（彩色灯可以比白光灯更暖一些）。在滑块上按住鼠标按钮即可在灯上看到效果；松开后灯会立即恢复。",
            [nameof(HueLookBrightnessLabel)] = "亮度",
            [nameof(HueLookBrightnessTooltip)] =
                "灯的亮度。在滑块上按住鼠标按钮即可在灯上看到效果；松开后灯会立即恢复。",
            [nameof(HueTurnOnAtStartLabel)] = "Afterglow 启动时开灯",
            [nameof(HueTurnOnAtStartTooltip)] =
                "Afterglow 启动时（如电脑开机或登录时）以及电脑从睡眠中唤醒时，打开所选房间中的所有灯（已排除的除外）",
            [nameof(HueReclaimLabel)] = "灯在别处被更改时",
            [nameof(HueReclaimTooltip)] =
                "当灯在 Afterglow 之外被更改（在 Hue 应用中、用开关、场景或语音助手），会被保持不变；这是保持的时长，从最后一次更改算起。在灯列表中点击“重新跟随”可立即重新跟随。",
            [nameof(HueReclaimNeverOption)] = "保持不变，直到关闭并重新打开",
            [nameof(HueLightLeftAloneUntilText)] = "已在别处更改，将于 {0} 重新跟随",
            [nameof(SmartLightsTabName)] = "智能灯",
            [nameof(HueReclaimAfterOption)] = "在设定时间后重新跟随",
            [nameof(HueReclaimAfterLabel)] = "之后",
            [nameof(MinutesValueText)] = "{0} 分钟",
            [nameof(HoursValueText)] = "{0} 小时",
            [nameof(HoursMinutesValueText)] = "{0} 小时 {1} 分钟",
            [nameof(HueFollowingCardTitle)] = "跟随",
            [nameof(HueFollowingCardSubtitle)] =
                "灯从屏幕上跟随哪些内容，以及在屏幕旁边要暖多少、暗多少。",
            [nameof(HueReclaimCardTitle)] = "在别处更改",
            [nameof(HueReclaimCardSubtitle)] =
                "在 Hue 应用中、用开关、场景或语音助手更改的灯会保持一段时间不变。",
            [nameof(HuePausingCardTitle)] = "暂停",
            [nameof(HuePausingCardSubtitle)] = "灯在暂停时会如何。",
            [nameof(HueStartupCardTitle)] = "启动与关机",
            [nameof(HueStartupCardSubtitle)] = "Afterglow 启动时，以及电脑关机或睡眠时，灯会如何。",
            [nameof(HueUnreachableText)] = "无法连接到 Hue 桥接器。稍后会再试。",
            [nameof(HueUnauthorizedText)] =
                "Hue 桥接器不再接受 Afterglow。请点击“重新连接”，然后按下桥接器的按钮。",
            [nameof(HueCertificateChangedText)] =
                "在此地址应答的不是你的 Hue 桥接器。如果你更换了桥接器，请点击“重新连接”。",
            [nameof(NeverCheckedText)] = "尚未检查",
            [nameof(UpdateCheckingText)] = "正在检查更新…",
            [nameof(UpToDateText)] = "{0} {1} 已是最新版本。",
            [nameof(UpdateCheckFailedText)] = "无法检查更新。请检查网络连接后重试。",
            [nameof(UpdateCheckUnavailableText)] = "开发版本不会检查更新。",
            [nameof(WhitelistedApplicationsPauseLabel)] = "为白名单应用程序暂停",
            [nameof(WhitelistedApplicationsPauseTooltip)] = "当前台窗口属于所选应用程序之一时暂停",
            [nameof(WhitelistedWindowTitlesPauseLabel)] = "为白名单窗口标题暂停",
            [nameof(WhitelistedWindowTitlesPauseTooltip)] =
                "当前台窗口的标题包含任一关键字时暂停（不区分大小写）",
            [nameof(WindowTitleKeywordPlaceholder)] = "标题关键字，例如 Netflix",
            [nameof(AddButton)] = "添加",
            [nameof(RemoveWindowTitleTooltip)] = "删除关键字",
            [nameof(PerMonitorWhitelistPauseLabel)] = "仅暂停显示匹配窗口的显示器",
            [nameof(PerMonitorWhitelistPauseTooltip)] =
                "其他显示器保持调节。关闭时将暂停所有显示器。",
            [nameof(PerMonitorFullscreenPauseLabel)] = "仅暂停运行全屏应用的显示器",
            [nameof(PerMonitorFullscreenPauseTooltip)] =
                "当全屏应用处于前台时，仅在运行该应用的显示器上恢复正常色彩，其他显示器继续跟随周期",
            [nameof(StreamingAutoPauseLabel)] = "远程串流时暂停（Sunshine / RDP）",
            [nameof(StreamingAutoPauseTooltip)] =
                "当此电脑正通过 Sunshine 串流或通过远程桌面访问时，在所有显示器上恢复正常色彩",
            [nameof(StreamingPortsLabel)] = "串流 TCP 端口",
            [nameof(StreamingPortsTooltip)] =
                "远程桌面会在任何端口上被自动检测。Sunshine 和其他串流应用通过这些本地 TCP 端口之一上的活动连接来检测，端口之间用逗号分隔（例如 Sunshine 为 48010，或填写 48010, 27036 以同时包含 Steam Remote Play）。",
            [nameof(DisplaysCardTitle)] = "已连接的显示器",
            [nameof(DisplaysCardSubtitle)] =
                "排除显示器或单独微调其暖色。显示器重新连接后会被再次识别。",
            [nameof(IdentifyButton)] = "识别",
            [nameof(IdentifyTooltip)] = "在每台显示器的屏幕上显示其编号",
            [nameof(OffsetScalingLabel)] = "偏移缩放",
            [nameof(OffsetScalingTooltip)] = "随着周期从白天进入夜间，每台显示器的色温偏移如何应用",
            [nameof(PerceptualOffsetScalingOption)] = "Mired（感知）",
            [nameof(FixedOffsetScalingOption)] = "开尔文（固定）",
            [nameof(PerceptualOffsetScalingDescription)] =
                "色温偏移在您的白天色温下精确生效，并在傍晚和夜间按感知比例缩放，使不同类型的面板在视觉上保持一致。",
            [nameof(FixedOffsetScalingDescription)] =
                "色温偏移始终以相同的固定开尔文值应用，不随白天到夜间缩放。",
            [nameof(NoDisplaysText)] = "未检测到显示器。",
            [nameof(PrimaryBadge)] = "主显示器",
            [nameof(DisplayEnabledTooltip)] =
                "对此显示器应用 Afterglow。排除后，该显示器将保持中性色彩。",
            [nameof(MonitorTemperatureOffsetLabel)] = "色温偏移",
            [nameof(MonitorTemperatureOffsetTooltip)] =
                "仅调整此显示器的色温。使用感知缩放时，这是在您的白天色温下的偏移量。",
            [nameof(MonitorBrightnessOffsetLabel)] = "亮度偏移",
            [nameof(MonitorBrightnessOffsetTooltip)] = "仅在此显示器上叠加到当前亮度",
            [nameof(ResetOffsetsTooltip)] = "重置偏移",
            [nameof(ReferenceDisplayNote)] = "参考显示器 · 跟随“周期与色彩”主时间表",
            [nameof(EffectiveNowText)] = "当前生效：{0} K · {1}",
            [nameof(ExcludedNeutralText)] = "已排除 · 中性 6600 K",
            [nameof(DisconnectedDisplaysLabel)] = "已保存的断开显示器（{0}）",
            [nameof(ForgetButton)] = "忘记",
            [nameof(ForgetDisplayTooltip)] = "删除此显示器已保存的偏好设置",
            // Controls & app
            [nameof(HotkeysCardTitle)] = "全局快捷键",
            [nameof(HotkeysCardSubtitle)] = "点击一个字段并按下组合键即可分配。",
            [nameof(SystemPreferencesCardTitle)] = "系统偏好设置",
            [nameof(StartMinimizedLabel)] = "启动时最小化",
            [nameof(StartMinimizedTooltip)] = "启动时将 Afterglow 隐藏到系统托盘，即使是手动启动",
            [nameof(ExtendedGammaRangeLabel)] = "扩展伽马范围",
            [nameof(ExtendedGammaRangeTooltip)] =
                "解锁 Windows 中的完整伽马范围，以支持更暖的色温和更低的亮度。更改此项需要管理员权限。",
            [nameof(GammaPollingIntervalLabel)] = "刷新间隔",
            [nameof(GammaPollingIntervalTooltip)] = "启用伽马轮询时，强制刷新伽马的频率",
            [nameof(SecondsValueText)] = "{0} 秒",
            // Tray menu
            [nameof(TrayOpenMenuItem)] = "打开 Afterglow",
            [nameof(TrayPauseMenuItem)] = "暂停",
            [nameof(TrayResumeMenuItem)] = "继续",
            [nameof(TrayPreviewMenuItem)] = "预览 24 小时周期",
            [nameof(PauseFor15MinutesChip)] = "15 分钟",
            [nameof(PauseFor1HourChip)] = "1 小时",
            [nameof(PauseFor3HoursChip)] = "3 小时",
            [nameof(PausedStateLabel)] = "已暂停",
            // Grayscale
            [nameof(TrayGrayscaleMenuItem)] = "灰度模式",
            [nameof(ToggleGrayscaleHotkeyLabel)] = "切换灰度模式",
            [nameof(ToggleGrayscaleHotkeyTooltip)] = "用于开启/关闭灰度模式的全局快捷键",
            [nameof(ToggleLightsPauseHotkeyLabel)] = "暂停/恢复 Hue 灯",
            [nameof(ToggleLightsPauseHotkeyTooltip)] =
                "用于暂停 Hue 灯（恢复为原来的状态）或恢复它们的全局快捷键",
            [nameof(GrayscaleCardTitle)] = "灰度模式",
            [nameof(GrayscaleCardSubtitle)] = "手动或按时间表去除屏幕色彩。",
            [nameof(GrayscaleScheduleLabel)] = "自动灰度",
            [nameof(GrayscaleScheduleTooltip)] =
                "灰度模式何时自动开启。它随时都可以手动切换，手动选择会持续到时间表下一次变化为止。",
            [nameof(GrayscaleScheduleOffOption)] = "关闭",
            [nameof(GrayscaleScheduleNightOption)] = "仅夜间",
            [nameof(GrayscaleScheduleEveningAndNightOption)] = "傍晚和夜间",
            [nameof(GrayscaleTemperatureShiftLabel)] = "灰度模式下应用色温",
            [nameof(GrayscaleTemperatureShiftTooltip)] =
                "在灰度之上继续应用色温，呈现温暖的琥珀色单色效果。关闭后，灰度模式使用中性白点，亮度设置仍然生效。",
            [nameof(GrayscaleScheduleOnOption)] = "开启",
            [nameof(TrayResumeGrayscaleMenuItem)] = "恢复灰度",
        };
}
