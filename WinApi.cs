using System.Reactive.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace RevitServerViewer;

public class DialogCloser
{
    const string m_appTitle = "Mechanical Desktop";
    const string m_popTitle = "Bosch Reorg";
    const int timer_interval = 1000;
    string m_dialog_caption;
    string m_button_text;
    private IDisposable? _interval;
    private List<WindowInfo> windowList = new();

    private string[] _windows = new string[]
    {
        ".NET - BroadcastEventWindow .1cc2e16 .0"
        , ".NET-BroadcastEventWindow.39c82f0.0"
        , ".NET-BroadcastEventWindow.4.0.0.0.6a55ba.0"
        , "(18) So, what is the BEST Spectre in Necropolis 3.24? - FULL GUIDE - YouTube \nPlaying audio"
        , "[PoE] I'm back to playing my favorite type of build, AGAIN - Stream Highlights #827"
        , "{1274D398-C3C8-422E-87DD-2FAFFD5A7F2F}"
        , "{2A335767-FC94-417F-ABC4-B4122ADBEE60}"
        , "{5AEA657D-F3F5-4BD8-BFE9-A4B537FA24C3}"
        , "{BBCDB34C-0BB1-4B5A-BD91-86C0E4F86510}_PnPNotificatorWndTitle_16708_26664"
        , "{BBCDB34C-0BB1-4B5A-BD91-86C0E4F86510}_PnPNotificatorWndTitle_24600_20612"
        , "@Guren - Discord"
        , "*new 2 - Notepad++"
        , "💤 (13) [POE 3.24] Divination Card Farming in Tier 16 maps | 22 divines per hour | Path of Exile Necropolis - YouTube"
        , "about:blank"
        , "AMD EEU Client"
        , "Autodesk Access"
        , "Awakened PoE Trade"
        , "Battery Meter"
        , "Bitsum Session Agent"
        , "BluetoothNotificationAreaIconWindowClass"
        , "BroadcastListenerWindow"
        , @"C:\Program Files (x86)\Cisco\Cisco AnyConnect Secure Mobility Client\aciseposture.exe"
        , "californication - Google Search"
        , "Canceling - 254 items recycled"
        , "Chat images"
        , "CiceroUIWndFrame"
        , "Cisco AnyConnect Secure Mobility Client"
        , "Connect"
        , "ContentLeftPanel"
        , "cptmsg"
        , "DDE Server Window"
        , "DesktopChildSiteBridge"
        , "DesktopWindowXamlSource"
        , "DWM Notification Window"
        , "EVR Fullscreen Window"
        , "Exilence CE"
        , "Fixing Male Insecurities (AND KICKING OFF MENTAL HEALTH MAY) - YouTube"
        , "FolderView"
        , "GDI+ Window (explorer.exe)"
        , "GDI+ Window (FanControl.EXE)"
        , "GDI+ Window (IBS.AddInManager.GUI.exe)"
        , "GDI+ Window (PowerToys.exe)"
        , "GDI+ Window (PowerToys.PowerLauncher.exe)"
        , "GDI+ Window (processlasso.exe)"
        , "GDI+ Window (rider64.exe)"
        , "GDI+ Window (ShareX.exe)"
        , "GDI+ Window (Spotify.exe)"
        , "GDI+ Window (vpnui.exe)"
        , "GDI+ Window (Zoom.exe)"
        , "Guitar Lesson: Marty Friedman - Japanese style guitar improv - YouTube"
        , "gvrBackgroundWindow3"
        , "Hidden Window"
        , "Hide graph"
        , "JUCEWindow"
        , "Logi_Devio_MainWindow"
        , "Logitech G HUB"
        , "macaroom - 電車かもしれない"
        , "MainWindow"
        , "Media viewer"
        , "MediaContextNotificationWindow"
        , "Menu"
        , "more menu"
        , "MS_WebcheckMonitor"
        , "Network Flyout"
        , "NotificationWindowHelper"
        , "NvContainerWindowClass00000B50"
        , "NvContainerWindowClass0000223C"
        , "NvContainerWindowClass000026E8"
        , "NvContainerWindowClass000026F0"
        , "NVIDIA GeForce Overlay"
        , "NVIDIA GeForce Overlay DT"
        , "NVIDIA NodeJS Share Window"
        , "NvSvc"
        , "Outline"
        , "Overflow Notification Area"
        , "Poe 3.19 (Lake of Kalandra) - Energy Shield based Minion Gear Crafting Guide"
        , "PopupMessageWindow"
        , "PowerToys Mouse Highlighter"
        , "PowerToys.PowerLauncher"
        , "Process Lasso"
        , "ProcessLasso_Notification_Window"
        , "Profile Card of (ИБС) Александр Степанов - BIM-инженер"
        , "profilecard.zoom.us"
        , "Program Manager"
        , "PToyTrayIconWindow"
        , "QTrayIconMessageWindow"
        , "Reactions"
        , "retrace history menu"
        , "RxDiag Message Pump 3.1 Mar 10 2023 08:28:10"
        , "Screen share viewing options"
        , "Search"
        , "SecurityHealthSystray"
        , "Selected Tab"
        , "ShareX 16.0.1"
        , "Show"
        , "SmartDC"
        , "SpeedCrunch"
        , "Start"
        , "Steam"
        , "System Scan"
        , "System tray overflow window."
        , "SystemResourceNotifyWindow"
        , "Systray Dialog - SSL VPN-Plus Client"
        , "Task Host Window"
        , "Task Switching"
        , "TelegramDesktop"
        , "Temp Window"
        , "theAwtToolkitWindow"
        , "TrayMessageWindow"
        , "us05web.zoom.us/pricing/client/upgrade/"
        , "UxdService"
        , "Video layouts"
        , "VideoFrameWnd"
        , "VideoPortContainer"
        , "Voicemeeter Banana"
        , "VPN"
        , "Widgets"
        , "Windows Input Experience"
        , "Windows Push Notifications Platform"
        , "WinEventHub"
        , "wpfui_th_81662624_1"
        , "zFloatContentParentWndCls"
        , "zFloatSizableParentWndCls"
        , "Zoom Calendar"
        , "zoom email"
        , "Zoom Share Container"
        , "Zoom Video Container"
        , "ZPCustomizedMenu"
        , "ZPTNewTooltip"
        , "ZPToolBarParentWnd"
        , "ZPVideoScrollBtnClass"
    };

    public DialogCloser(string dialog_caption, string button_text)
    {
        //Console.WriteLine( dialog_caption );
        //Console.WriteLine( button_text );
        m_dialog_caption = dialog_caption;
        m_button_text = button_text;
        _interval = Observable.Timer(TimeSpan.FromSeconds(1))
            .Subscribe(Timer_Elapsed_jan);
    }

    private void Timer_Elapsed(long e)
    {
        int hwnd = WinApi.FindWindow("", m_dialog_caption);
        Console.WriteLine(hwnd.ToString());
    }

    private void Timer_Elapsed_jan(long n)
    {
        int ret = WinApi.EnumWindows(EnumerateWindows, 0);
        windowList = windowList.Where(wi =>
                wi.WindowText != "Default IME"
                && wi.WindowText != "MSCTFIME UI"
                && wi.WindowText != "ActiveMovie Window"
                && wi.WindowText.Length > 0
                && wi.WindowText != "PopupHost"
                && wi.WindowText != "Zoom"
                && wi.WindowText != "Chrome Legacy Window"
                && !wi.WindowText.Contains("Firefox")
            )
            .OrderBy(wi => wi.WindowText)
            .ToList();
        var gr = string.Join("\"\r\n, \"", windowList
            .GroupBy(wi => wi.WindowText)
            .ToDictionary(x => x.Key, x => x.Count())
            .Select(x => (x.Key, x.Value))
            .OrderBy(x => x.Key)
            .Select(x => x.Key));
    }

    private bool EnumerateWindows(int hwnd, int lParam)
    {
        StringBuilder sbTitle = new StringBuilder(256);
        WinApi.GetWindowText(hwnd, sbTitle, sbTitle.Capacity);
        var wi = new WindowInfo()
        {
            WindowHwnd = hwnd, WindowText = sbTitle.ToString()
        };
        int pos = wi.WindowText.IndexOf('-');
        // if (0 == wi.WindowText.Length || -1 == pos)
        // {
        //     return true;
        // }
        // if (0 < wi.WindowText.Length && pos != -1)
        // wi.WindowText = wi.WindowText[..(pos - 1)];

        var popHwnd = WinApi.GetLastActivePopup(hwnd);
        WinApi.GetWindowText(popHwnd, sbTitle, sbTitle.Capacity);
        var popText = sbTitle.ToString();


        // if (m_popTitle != title)
        // {
        //     return true;
        // }

        //
        // we found the dialogue, now click the button:
        //
        // Console.WriteLine("JtClicker found it!");
        // _interval?.Dispose();
        // _interval = null;

        if (windowList.Any(w => w.WindowHwnd == hwnd)) return true;
        windowList.Add(wi);
        return true;
    }


    public class WindowInfo
    {
        public string WindowText { get; set; }

        public int WindowHwnd { get; set; }
        // public string PopupText { get; set; }
        // public int PopupHwnd { get; set; }

        public List<WindowInfo> PopupWindows { get; set; } = new();

        public override string ToString()
        {
            var txt = $"[{WindowHwnd}] {WindowText}: {PopupWindows.Count}";
            // if (PopupHwnd != WindowHwnd && PopupHwnd != 0) txt += $", [{PopupHwnd}], {PopupText}";
            return txt;
        }
    }

    private bool EnumWindowsProc(int hwnd, int lParam)
    {
        StringBuilder sbTitle = new StringBuilder(256);

        WinApi.GetWindowText(hwnd, sbTitle, sbTitle.Capacity);
        string title = sbTitle.ToString();

        if (0 == title.Length)
        {
            return true;
        }

        int pos = title.IndexOf('-');

        if (-1 == pos) return true;

        title = title.Substring(0, pos - 1);
        if (0 != string.Compare(m_appTitle, title))
        {
            return true;
        }

        int hwndPopup = WinApi.GetLastActivePopup(hwnd);

        WinApi.GetWindowText(hwndPopup, sbTitle, sbTitle.Capacity);
        title = sbTitle.ToString();
        if (m_popTitle != title)
        {
            return true;
        }

        //
        // we found the dialogue, now click the button:
        //
        Console.WriteLine("JtClicker found it!");
        _interval?.Dispose();
        _interval = null;

        int ret = WinApi.EnumChildWindows(hwnd, new WinApi.EnumWindowsProc(EnumChildProc), 0);

        return false;
    }

    private bool EnumChildProc(int hwnd, int lParam)
    {
        StringBuilder sbTitle = new StringBuilder(256);
        WinApi.GetWindowText(hwnd, sbTitle, sbTitle.Capacity);
        var wi = new WindowInfo()
        {
            WindowText = sbTitle.ToString()
        };

        // if (windowList.All(w => w.WindowHwnd != hwnd))
        if (0 == wi.WindowText.Length) return true;
        windowList.Last().PopupWindows.Add(wi);
        int ret = WinApi.EnumChildWindows(hwnd, EnumChildProc, 0);

        //Debug.WriteLine( title );
        // if (title != m_button_text)
        // {
        //     return true;
        // }

        // Console.WriteLine(string.Format("\nJtClicker found {0}\n", title));
        // WinApi.SendMessage(hwnd, WinApi.BM_SETSTATE, 1, 0);
        // WinApi.SendMessage(hwnd, WinApi.WM_LBUTTONDOWN, 0, 0);
        // WinApi.SendMessage(hwnd, WinApi.WM_LBUTTONUP, 0, 0);
        // WinApi.SendMessage(hwnd, WinApi.BM_SETSTATE, 1, 0);
        return true;
    }
}

public class WinApi
{
    public delegate bool EnumWindowsProc(int hWnd, int lParam);

    [DllImport("user32.Dll", CharSet = CharSet.Unicode)]
    public static extern int FindWindow(string className, string windowName);

    [DllImport("user32.Dll")]
    public static extern int EnumWindows(EnumWindowsProc callbackFunc, int lParam);

    [DllImport("user32.Dll")]
    public static extern int EnumChildWindows(int hwnd, EnumWindowsProc callbackFunc, int lParam);

    [DllImport("user32.Dll", CharSet = CharSet.Unicode)]
    public static extern int GetWindowText(int hwnd, StringBuilder buff, int maxCount);

    [DllImport("user32.Dll")]
    public static extern int GetLastActivePopup(int hwnd);

    [DllImport("user32.Dll")]
    public static extern int SendMessage(int hwnd, int Msg, int wParam, int lParam);

    public const int BM_SETSTATE = 0x00F3;
    public const int WM_LBUTTONDOWN = 0x0201;
    public const int WM_LBUTTONUP = 0x0202;
}