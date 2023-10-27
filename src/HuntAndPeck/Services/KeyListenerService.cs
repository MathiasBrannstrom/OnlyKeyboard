using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using static HuntAndPeck.NativeMethods.User32;
using System.Windows.Forms;
using Utilities;
using System.Collections.Generic;
using System.Linq;

namespace HuntAndPeck.Services
{

    public enum Action
    {
        MouseMoveUp,
        MouseMoveDown,
        MouseMoveLeft,
        MouseMoveRight,
        MouseLeftButton,
        MouseRightButton,
        MouseMiddleButton,
        MouseDoubleClick,
        MouseScrollUp,
        MouseScrollDown,
        ShowUINavigationLabels,
        ShowGridNavigationLabels
    }

    public interface IMouseReplacementKeys
    {
        Keys GetKeyForAction(Action action);
    }


    public class DefaultKeyMappings : IMouseReplacementKeys
    {
        private readonly Dictionary<Action, Keys> _actionKeyMappings = new Dictionary<Action, Keys>
        {
            { Action.MouseMoveUp, Keys.F13 },
            { Action.MouseMoveDown, Keys.F14 },
            { Action.MouseMoveLeft, Keys.F15 },
            { Action.MouseMoveRight, Keys.F16 },
            { Action.MouseLeftButton, Keys.F17 },
            { Action.MouseRightButton, Keys.F18 },
            { Action.MouseMiddleButton, Keys.F19 },
            { Action.MouseDoubleClick, Keys.F20 },
            { Action.MouseScrollUp, Keys.F21 },
            { Action.MouseScrollDown, Keys.F22 },
            { Action.ShowUINavigationLabels, Keys.F23 },
            { Action.ShowGridNavigationLabels, Keys.F24 }
        };
        private Dictionary<Keys, Action> _reverseMapping;

        public DefaultKeyMappings()
        {
            _reverseMapping = _actionKeyMappings.ToDictionary(x => x.Value, x => x.Key);
        }

        public bool HasKeyMappingForKey(Keys key)
        {
            return _reverseMapping.ContainsKey(key);
        }

        public Action GetActionFromKey(Keys key)
        {
            if (_reverseMapping.TryGetValue(key, out var action))
            {
                return action;
            }

            throw new ArgumentOutOfRangeException(nameof(key), key, null);
        }

        public Keys GetKeyForAction(Action action)
        {
            if (_actionKeyMappings.TryGetValue(action, out var key))
            {
                return key;
            }

            throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }

    internal class KeysBeingHeld
    {
        private readonly KeyListenerService _listenerService;
        private DefaultKeyMappings _keyMappings;

        public KeysBeingHeld(KeyListenerService listenerService)
        {
            _listenerService = listenerService;
            _listenerService.KeyChanged += HandleKeyChanged;

            foreach(Action action in Enum.GetValues(typeof(Action)))
            {
                IsActionKeyHeld[action] = new ValueHolder<bool>(false);
            }

            _keyMappings = new DefaultKeyMappings();
        }

        private void HandleKeyChanged(Keys key, ButtonEvent buttonEvent)
        {
            if (!_keyMappings.HasKeyMappingForKey(key))
                return;

            IsActionKeyHeld[_keyMappings.GetActionFromKey(key)].Value = buttonEvent == ButtonEvent.Pressed;
        }

        public Dictionary<Action, ValueHolder<bool>> IsActionKeyHeld { get; } = new Dictionary<Action, ValueHolder<bool>>();
    }

    public enum ButtonEvent
    {
        Pressed,
        Released
    }

    internal class KeyListenerService : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private IntPtr _hookId = IntPtr.Zero;
        private LowLevelKeyboardProc _keyboardProc;

        public delegate void KeyEvent(Keys key, ButtonEvent buttonEvent);
        public event KeyEvent KeyChanged;


        public KeyListenerService()
        {
            _keyboardProc = KeyboardEvent;
            _hookId = StartListeningToKeyboard(_keyboardProc);
        }

        private IntPtr StartListeningToKeyboard(LowLevelKeyboardProc proc)
        {
            using (ProcessModule module = Process.GetCurrentProcess().MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName), 0);
            }
        }

        private IntPtr KeyboardEvent(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    KeyChanged?.Invoke((Keys)vkCode, ButtonEvent.Pressed);
                }
                else if (wParam == (IntPtr)WM_KEYUP)
                {
                    KeyChanged?.Invoke((Keys)vkCode, ButtonEvent.Released);
                }
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookId);
        }
    }
}
