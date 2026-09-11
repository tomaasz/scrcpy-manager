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
        public string initialDiscovery { get; set; }

        public UserPreferences()
        {
            theme = "dark";
            lang = "PL";
            layout = 2;
            navBar = true;
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
            ConnectionState = DeviceConnectionState.None;
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
        public Color BtnHeroText { get; set; }
        public Color BtnMode { get; set; }
        public Color BtnModeText { get; set; }
        public Color BtnModeBorder { get; set; }
        public Color BtnReboot { get; set; }
        public Color BtnRebootText { get; set; }
        public Color BtnRebootBorder { get; set; }
        public Color BtnTool { get; set; }
        public Color BtnToolText { get; set; }
        public Color BtnToolBorder { get; set; }
        public Color BtnApp { get; set; }
        public Color BtnAppText { get; set; }
        public Color BtnAppBorder { get; set; }
        public Color InputBg { get; set; }
        public Color InputText { get; set; }
        public Color ToggleBg { get; set; }
        public Color ToggleChecked { get; set; }
        public Color ToggleText { get; set; }
        public Color BadgeUpdate { get; set; }
        public Color BadgeUpdateText { get; set; }
        public Color BadgeUpdateBorder { get; set; }

        public static readonly ThemeColors Dark = new ThemeColors
        {
            Bg               = Color.FromArgb(24, 25, 32),
            Card             = Color.FromArgb(33, 35, 45),
            CardBorder       = Color.FromArgb(52, 56, 70),
            Text             = Color.FromArgb(240, 242, 248),
            TextMuted        = Color.FromArgb(150, 155, 175),
            StatusDotOnline  = Color.FromArgb(46, 204, 113),
            StatusDotOffline = Color.FromArgb(127, 140, 141),
            BtnHero          = Color.FromArgb(35, 145, 75),
            BtnHeroText      = Color.White,
            BtnMode          = Color.FromArgb(44, 48, 62),
            BtnModeText      = Color.FromArgb(230, 235, 245),
            BtnModeBorder    = Color.FromArgb(66, 71, 92),
            BtnReboot        = Color.FromArgb(62, 35, 40),
            BtnRebootText    = Color.FromArgb(255, 138, 138),
            BtnRebootBorder  = Color.FromArgb(110, 46, 53),
            BtnTool          = Color.FromArgb(39, 58, 94),
            BtnToolText      = Color.FromArgb(214, 228, 255),
            BtnToolBorder    = Color.FromArgb(58, 80, 126),
            BtnApp           = Color.FromArgb(37, 40, 52),
            BtnAppText       = Color.FromArgb(235, 240, 250),
            BtnAppBorder     = Color.FromArgb(56, 61, 80),
            InputBg          = Color.FromArgb(28, 30, 38),
            InputText        = Color.FromArgb(240, 242, 248),
            ToggleBg         = Color.FromArgb(44, 48, 62),
            ToggleChecked    = Color.FromArgb(48, 70, 104),
            ToggleText       = Color.FromArgb(230, 235, 245),
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
            BtnHeroText      = Color.White,
            BtnMode          = Color.FromArgb(240, 242, 248),
            BtnModeText      = Color.FromArgb(36, 40, 56),
            BtnModeBorder    = Color.FromArgb(212, 216, 230),
            BtnReboot        = Color.FromArgb(253, 238, 239),
            BtnRebootText    = Color.FromArgb(192, 34, 47),
            BtnRebootBorder  = Color.FromArgb(246, 193, 197),
            BtnTool          = Color.FromArgb(238, 243, 252),
            BtnToolText      = Color.FromArgb(27, 79, 155),
            BtnToolBorder    = Color.FromArgb(202, 217, 244),
            BtnApp           = Color.FromArgb(255, 255, 255),
            BtnAppText       = Color.FromArgb(33, 36, 48),
            BtnAppBorder     = Color.FromArgb(216, 220, 230),
            InputBg          = Color.White,
            InputText        = Color.FromArgb(25, 28, 36),
            ToggleBg         = Color.FromArgb(232, 235, 245),
            ToggleChecked    = Color.FromArgb(214, 225, 246),
            ToggleText       = Color.FromArgb(35, 40, 55),
            BadgeUpdate      = Color.FromArgb(209, 250, 229),
            BadgeUpdateText  = Color.FromArgb(6, 95, 70),
            BadgeUpdateBorder= Color.FromArgb(52, 211, 153)
        };
    }
}
