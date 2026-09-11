using System;
using System.Collections.Generic;
using System.Drawing;

namespace ScrcpyManager
{
    public enum DeviceConnectionState
    {
        None,
        Online,
        Unauthorized,
        Offline,
        Recovery,
        Multiple
    }

    public class AppEntry
    {
        public string name { get; set; }
        public string package { get; set; }
        public List<string> flags { get; set; }
        public AppLaunchProfile profile { get; set; }

        public AppEntry()
        {
            flags = new List<string>();
            profile = new AppLaunchProfile();
        }

        public AppEntry(string name, string package, params string[] initialFlags)
        {
            this.name = name;
            this.package = package;
            this.flags = initialFlags != null ? new List<string>(initialFlags) : new List<string>();
            profile = new AppLaunchProfile();
        }

        public AppEntry(string name, string package, IEnumerable<string> initialFlags)
        {
            this.name = name;
            this.package = package;
            this.flags = initialFlags != null ? new List<string>(initialFlags) : new List<string>();
            profile = new AppLaunchProfile();
        }
    }

    public class AppLaunchProfile
    {
        public string preset { get; set; }
        public string displaySize { get; set; }
        public int maxFps { get; set; }
        public string videoBitRate { get; set; }
        public string videoCodec { get; set; }
        public string orientation { get; set; }
        public string keyboardMode { get; set; }
        public string mouseMode { get; set; }
        public string audioMode { get; set; }
        public bool alwaysOnTop { get; set; }
        public bool borderless { get; set; }
        public bool fullscreen { get; set; }
        public bool turnScreenOff { get; set; }
        public bool recordSession { get; set; }
        public bool forwardAllClicks { get; set; }
        public string taskbarMode { get; set; }

        public AppLaunchProfile()
        {
            preset = "default";
            displaySize = "2560x1440/160";
            maxFps = 60;
            videoBitRate = "8M";
            videoCodec = "h264";
            orientation = "auto";
            keyboardMode = "sdk";
            mouseMode = "sdk";
            audioMode = "global";
            taskbarMode = "global";
        }

        public AppLaunchProfile Clone()
        {
            return (AppLaunchProfile)MemberwiseClone();
        }
    }

    public class UserPreferences
    {
        public string theme { get; set; }
        public string lang { get; set; }
        public int layout { get; set; }
        public bool navBar { get; set; }
        public bool autoTaskbar { get; set; }
        public string initialDiscovery { get; set; }

        public UserPreferences()
        {
            theme = "dark";
            lang = "PL";
            layout = 2;
            navBar = true;
            autoTaskbar = false;
            initialDiscovery = "pending";
        }
    }

    public class DeviceInfo
    {
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public int BatteryLevel { get; set; }
        public bool IsCharging { get; set; }
        public bool IsWifiConnected { get; set; }
        public bool IsOnline { get; set; }
        public string Serial { get; set; }
        public DeviceConnectionState ConnectionState { get; set; }
        public string ConnectionError { get; set; }

        public DeviceInfo()
        {
            Model = "Android";
            Manufacturer = "";
            BatteryLevel = -1;
            IsCharging = false;
            IsWifiConnected = false;
            IsOnline = false;
            Serial = "";
            ConnectionError = "";
        }
    }

    public class ThemeColors
    {
        public Color Bg { get; set; }
        public Color Card { get; set; }
        public Color CardBorder { get; set; }
        public Color Text { get; set; }
        public Color TextMuted { get; set; }
        public Color StatusDotOnline { get; set; }
        public Color StatusDotOffline { get; set; }
        public Color BtnHero { get; set; }
        public Color BtnHeroHover { get; set; }
        public Color BtnHeroDown { get; set; }
        public Color BtnHeroText { get; set; }
        public Color BtnMode { get; set; }
        public Color BtnModeHover { get; set; }
        public Color BtnModeText { get; set; }
        public Color BtnModeBorder { get; set; }
        public Color BtnReboot { get; set; }
        public Color BtnRebootHover { get; set; }
        public Color BtnRebootText { get; set; }
        public Color BtnRebootBorder { get; set; }
        public Color BtnTool { get; set; }
        public Color BtnToolHover { get; set; }
        public Color BtnToolText { get; set; }
        public Color BtnToolBorder { get; set; }
        public Color BtnApp { get; set; }
        public Color BtnAppHover { get; set; }
        public Color BtnAppText { get; set; }
        public Color BtnAppBorder { get; set; }
        public Color InputBg { get; set; }
        public Color InputText { get; set; }
        public Color InputBorder { get; set; }
        public Color ToggleBg { get; set; }
        public Color ToggleHover { get; set; }
        public Color ToggleChecked { get; set; }
        public Color ToggleText { get; set; }
        public Color BadgeUpdate { get; set; }
        public Color BadgeUpdateText { get; set; }
        public Color BadgeUpdateBorder { get; set; }

        public static readonly ThemeColors Dark = new ThemeColors
        {
            Bg               = Color.FromArgb(22, 27, 36),       // chłodny, głęboki grafit (#161B24)
            Card             = Color.FromArgb(24, 31, 43),       // grafitowa karta (#181F2B)
            CardBorder       = Color.FromArgb(44, 57, 75),       // delikatne, spójne obramowanie (#2C394B)
            Text             = Color.FromArgb(248, 250, 252),    // wyrazisty, czysty biały (#F8FAFC)
            TextMuted        = Color.FromArgb(148, 163, 184),    // czytelny, jasny odcień pomocniczy slate (#94A3B8)
            StatusDotOnline  = Color.FromArgb(34, 197, 94),      // żywa zieleń (#22C55E)
            StatusDotOffline = Color.FromArgb(100, 116, 139),    // stonowany slate (#64748B)
            BtnHero          = Color.FromArgb(28, 144, 80),      // wyrazisty zielony przycisk (#1C9050)
            BtnHeroHover     = Color.FromArgb(34, 168, 94),      // rozjaśniony zielony przy najechaniu
            BtnHeroDown      = Color.FromArgb(22, 120, 66),
            BtnHeroText      = Color.White,
            BtnMode          = Color.FromArgb(26, 34, 48),       // stonowane przyciski trybu (#1A2230)
            BtnModeHover     = Color.FromArgb(36, 47, 66),
            BtnModeText      = Color.FromArgb(241, 245, 249),
            BtnModeBorder    = Color.FromArgb(48, 62, 82),       // (#303E52)
            BtnReboot        = Color.FromArgb(71, 40, 45),       // stonowany ciemnoczerwony (#47282D)
            BtnRebootHover   = Color.FromArgb(88, 48, 54),
            BtnRebootText    = Color.FromArgb(252, 165, 165),    // ciepły, łagodny czerwony tekst (#FCA5A5)
            BtnRebootBorder  = Color.FromArgb(122, 50, 61),      // (#7A323D)
            BtnTool          = Color.FromArgb(36, 62, 101),      // ciemnoniebieski grafit (#243E65)
            BtnToolHover     = Color.FromArgb(46, 78, 126),
            BtnToolText      = Color.FromArgb(241, 245, 249),
            BtnToolBorder    = Color.FromArgb(58, 92, 144),
            BtnApp           = Color.FromArgb(34, 43, 55),       // czytelne kafelki (#222B37)
            BtnAppHover      = Color.FromArgb(44, 55, 71),
            BtnAppText       = Color.FromArgb(241, 245, 249),
            BtnAppBorder     = Color.FromArgb(48, 62, 82),       // (#303E52)
            InputBg          = Color.FromArgb(22, 29, 39),       // (#161D27)
            InputText        = Color.FromArgb(248, 250, 252),
            InputBorder      = Color.FromArgb(48, 62, 82),
            ToggleBg         = Color.FromArgb(26, 34, 48),
            ToggleHover      = Color.FromArgb(36, 47, 66),
            ToggleChecked    = Color.FromArgb(37, 99, 235),
            ToggleText       = Color.FromArgb(241, 245, 249),
            BadgeUpdate      = Color.FromArgb(16, 185, 129),
            BadgeUpdateText  = Color.White,
            BadgeUpdateBorder= Color.FromArgb(52, 211, 153)
        };

        public static readonly ThemeColors Light = new ThemeColors
        {
            Bg               = Color.FromArgb(245, 246, 250),
            Card             = Color.FromArgb(255, 255, 255),
            CardBorder       = Color.FromArgb(216, 220, 230),
            Text             = Color.FromArgb(25, 28, 36),
            TextMuted        = Color.FromArgb(100, 105, 121),
            StatusDotOnline  = Color.FromArgb(39, 174, 96),
            StatusDotOffline = Color.FromArgb(189, 195, 199),
            BtnHero          = Color.FromArgb(27, 138, 70),
            BtnHeroHover     = Color.FromArgb(35, 160, 82),
            BtnHeroDown      = Color.FromArgb(20, 115, 58),
            BtnHeroText      = Color.White,
            BtnMode          = Color.FromArgb(240, 242, 248),
            BtnModeHover     = Color.FromArgb(228, 232, 242),
            BtnModeText      = Color.FromArgb(36, 40, 56),
            BtnModeBorder    = Color.FromArgb(212, 216, 230),
            BtnReboot        = Color.FromArgb(253, 238, 239),
            BtnRebootHover   = Color.FromArgb(248, 222, 224),
            BtnRebootText    = Color.FromArgb(192, 34, 47),
            BtnRebootBorder  = Color.FromArgb(246, 193, 197),
            BtnTool          = Color.FromArgb(238, 243, 252),
            BtnToolHover     = Color.FromArgb(225, 234, 248),
            BtnToolText      = Color.FromArgb(27, 79, 155),
            BtnToolBorder    = Color.FromArgb(202, 217, 244),
            BtnApp           = Color.FromArgb(255, 255, 255),
            BtnAppHover      = Color.FromArgb(245, 247, 252),
            BtnAppText       = Color.FromArgb(33, 36, 48),
            BtnAppBorder     = Color.FromArgb(216, 220, 230),
            InputBg          = Color.White,
            InputText        = Color.FromArgb(25, 28, 36),
            InputBorder      = Color.FromArgb(216, 220, 230),
            ToggleBg         = Color.FromArgb(232, 235, 245),
            ToggleHover      = Color.FromArgb(220, 225, 238),
            ToggleChecked    = Color.FromArgb(214, 225, 246),
            ToggleText       = Color.FromArgb(35, 40, 55),
            BadgeUpdate      = Color.FromArgb(209, 250, 229),
            BadgeUpdateText  = Color.FromArgb(6, 95, 70),
            BadgeUpdateBorder= Color.FromArgb(52, 211, 153)
        };
    }
}
