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

    

    public DialogCloser(string dialog_caption, string button_text)
    {
        //Console.WriteLine( dialog_caption );
        //Console.WriteLine( button_text );
        m_dialog_caption = dialog_caption;
        m_button_text = button_text;
        _interval = Observable.Timer(TimeSpan.FromSeconds(1))
            .Subscribe(Timer_Elapsed_jan);
    }

    // private void Timer_Elapsed(long e)
    // {
    //     int hwnd = WinApi.FindWindow("", m_dialog_caption);
    //     Console.WriteLine(hwnd.ToString());
    // }

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