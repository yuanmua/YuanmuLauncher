using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

public static class HotkeyManager
{
    private const int WM_HOTKEY = 0x0312;
    private static Dictionary<int, HotkeyInfo> _hotkeyDictionary = new Dictionary<int, HotkeyInfo>();
    private static HwndSource _source;
    private static IntPtr _windowHandle;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public class HotkeyInfo
    {
        public Key Key { get; set; }
        public ModifierKeys Modifier { get; set; }
        public Action Callback { get; set; }
        public string Name { get; set; }
    }

    public static void Initialize(Window window)
    {
        WindowInteropHelper helper = new WindowInteropHelper(window);
        _windowHandle = helper.Handle;
        _source = HwndSource.FromHwnd(_windowHandle);
        _source.AddHook(HwndHook);
    }

    private static IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            if (_hotkeyDictionary.ContainsKey(hotkeyId))
            {
                _hotkeyDictionary[hotkeyId].Callback?.Invoke();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    public static bool RegisterHotkey(string name, Key key, ModifierKeys modifier, Action callback)
    {
        int id = GetHotkeyId(key, modifier);
        
        // 如果已存在相同的快捷键，先注销
        if (_hotkeyDictionary.ContainsKey(id))
        {
            UnregisterHotKey(_windowHandle, id);
            _hotkeyDictionary.Remove(id);
        }

        // 注册新的快捷键
        if (RegisterHotKey(_windowHandle, id, (uint)modifier, (uint)KeyInterop.VirtualKeyFromKey(key)))
        {
            _hotkeyDictionary[id] = new HotkeyInfo
            {
                Key = key,
                Modifier = modifier,
                Callback = callback,
                Name = name
            };
            return true;
        }
        return false;
    }

    public static bool UnregisterHotkey(string name)
    {
        var hotkey = _hotkeyDictionary.FirstOrDefault(x => x.Value.Name == name);
        if (hotkey.Value != null)
        {
            UnregisterHotKey(_windowHandle, hotkey.Key);
            return _hotkeyDictionary.Remove(hotkey.Key);
        }
        return false;
    }

    public static bool UpdateHotkey(string name, Key newKey, ModifierKeys newModifier)
    {
        var hotkey = _hotkeyDictionary.FirstOrDefault(x => x.Value.Name == name);
        if (hotkey.Value != null)
        {
            var callback = hotkey.Value.Callback;
            UnregisterHotkey(name);
            return RegisterHotkey(name, newKey, newModifier, callback);
        }
        return false;
    }

    private static int GetHotkeyId(Key key, ModifierKeys modifier)
    {
        return (int)key + ((int)modifier * 0x10000);
    }

    public static List<HotkeyInfo> GetAllHotkeys()
    {
        return _hotkeyDictionary.Values.ToList();
    }
}
