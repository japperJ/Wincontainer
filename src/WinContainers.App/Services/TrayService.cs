using System.Runtime.InteropServices;
using System.Text;

namespace WinContainers_App.Services;

public static class TrayService
{
    public static event Action? ShowWindowRequested;
    public static event Action? ExitRequested;
    public static event Action? ExitRequestedTrayThread;

    private static nint _hWnd;
    private static nint _hMenu;
    private static uint _msgId;
    private static WndProcDelegate? _wndProc;
    private static Thread? _trayThread;
    private static bool _running;

    private delegate nint WndProcDelegate(nint hWnd, uint msg, nint wParam, nint lParam);

    private const uint WM_APP = 0x8000;
    private const uint WM_COMMAND = 0x0111;

    private const int ID_OPEN = 1;
    private const int ID_EXIT = 2;

    public static void Start()
    {
        if (_running) return;

        _running = true;
        _trayThread = new Thread(TrayThreadProc);
        _trayThread.SetApartmentState(ApartmentState.STA);
        _trayThread.Start();
    }

    private static void TrayThreadProc()
    {
        _msgId = RegisterWindowMessageW("WinContainersTrayMsg");

        _wndProc = WndProc;
        var hInstance = Marshal.GetHINSTANCE(typeof(TrayService).Module);

        var wc = new WNDCLASSEXW
        {
            cbSize = (uint)Marshal.SizeOf<WNDCLASSEXW>(),
            lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProc),
            hInstance = hInstance,
            lpszClassName = "WinContainersTrayWnd"
        };
        RegisterClassExW(ref wc);

        _hWnd = CreateWindowExW(
            0, "WinContainersTrayWnd", "",
            0, 0, 0, 0, 0,
            nint.Zero, nint.Zero, hInstance, nint.Zero);

        if (_hWnd == 0)
            return;

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        var hIcon = LoadImageW(nint.Zero, iconPath, 1, 16, 16, 0x00000010);
        if (hIcon == 0)
            hIcon = LoadIconW(nint.Zero, 32512);

        var tipBytes = Encoding.Unicode.GetBytes("WinContainers\0");
        var nid = new NOTIFYICONDATAW
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
            hWnd = _hWnd,
            uID = 0,
            uFlags = 0x00000001 | 0x00000002 | 0x00000004 | 0x00000080,
            uCallbackMessage = _msgId,
            hIcon = hIcon,
            szTip = new byte[128]
        };
        if (tipBytes.Length <= 128)
            Buffer.BlockCopy(tipBytes, 0, nid.szTip, 0, tipBytes.Length);

        Shell_NotifyIconW(0, ref nid);

        nid.uVersion = 4;
        Shell_NotifyIconW(4, ref nid);

        _hMenu = CreatePopupMenu();
        AppendMenuW(_hMenu, 0, ID_OPEN, "Open WinContainers");
        AppendMenuW(_hMenu, 0x800, 0, null);
        AppendMenuW(_hMenu, 0, ID_EXIT, "Exit");

        // Message pump
        while (_running && GetMessageW(out var msg, nint.Zero, 0, 0))
        {
            TranslateMessage(ref msg);
            DispatchMessageW(ref msg);
        }
    }

    public static void Stop()
    {
        _running = false;
        if (_hWnd != 0)
        {
            var nid = new NOTIFYICONDATAW
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATAW>(),
                hWnd = _hWnd,
                uID = 0
            };
            Shell_NotifyIconW(2, ref nid);
            DestroyMenu(_hMenu);
            PostMessageW(_hWnd, 0x0012, 0, 0);
            _hWnd = 0;
        }
    }

    private static nint WndProc(nint hWnd, uint msg, nint wParam, nint lParam)
    {
        if (msg == _msgId)
        {
            var evt = lParam.ToInt32() & 0xFFFF;
            switch (evt)
            {
                case 0x0201:
                case 0x0202:
                    ShowWindowRequested?.Invoke();
                    return 0;
                case 0x0204:
                    ShowContextMenu();
                    return 0;
            }
        }

        if (msg == WM_COMMAND)
        {
            var cmd = wParam.ToInt32() & 0xFFFF;
            if (cmd == ID_OPEN)
            {
                ShowWindowRequested?.Invoke();
                return 0;
            }
            if (cmd == ID_EXIT)
            {
                ExitRequestedTrayThread?.Invoke();
                ExitRequested?.Invoke();
                return 0;
            }
        }

        return DefWindowProcW(hWnd, msg, wParam, lParam);
    }

    private static void ShowContextMenu()
    {
        GetCursorPos(out var pt);
        SetForegroundWindow(_hWnd);
        TrackPopupMenu(_hMenu, 0, pt.x, pt.y, 0, _hWnd, nint.Zero);
        PostMessageW(_hWnd, 0x0100, 0, 0);
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint RegisterWindowMessageW(string lpString);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClassExW(ref WNDCLASSEXW lpWndClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowExW(uint dwExStyle, string lpClassName, string lpWindowName,
        uint dwStyle, int x, int y, int nWidth, int nHeight, nint hWndParent, nint hMenu, nint hInstance, nint lpParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint DefWindowProcW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyWindow(nint hWnd);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadIconW(nint hInstance, nint lpIconName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenuW(nint hMenu, uint uFlags, uint uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TrackPopupMenu(nint hMenu, uint uFlags, int x, int y, int nReserved, nint hWnd, nint prcRect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(nint hMenu);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetForegroundWindow(nint hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMessageW(out MSG lpMsg, nint hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool TranslateMessage(ref MSG lpMsg);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DispatchMessageW(ref MSG lpMsg);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint LoadImageW(nint hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIconW(uint dwMessage, ref NOTIFYICONDATAW lpData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WNDCLASSEXW
    {
        public uint cbSize;
        public uint style;
        public nint lpfnWndProc;
        public int cbClsExtra;
        public int cbWndExtra;
        public nint hInstance;
        public nint hIcon;
        public nint hCursor;
        public nint hbrBackground;
        public nint lpszMenuName;
        public string lpszClassName;
        public nint hIconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSG
    {
        public nint hwnd;
        public uint message;
        public nint wParam;
        public nint lParam;
        public uint time;
        public POINT pt;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATAW
    {
        public uint cbSize;
        public nint hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public nint hIcon;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] szInfo;

        public uint uVersion;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public nint hBalloonIcon;
    }
}
