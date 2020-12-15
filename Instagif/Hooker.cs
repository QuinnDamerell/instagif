using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;

namespace Instagif
{
    class Hooker
    {
        public delegate void GlobalHotKeyInvokedHandler();
        public event GlobalHotKeyInvokedHandler GlobalHotKeyInvoked;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private bool m_isCtlDown = false;
        private bool m_isShiftDown = false;
        private static Hooker s_instance;

        public Hooker()
        {
            s_instance = this;
        }

        private void OnKeyChange(Key key, bool isDown)
        {
            if(key == Key.LeftCtrl || key== Key.RightCtrl)
            {
                m_isCtlDown = isDown;
            }
            else if(key == Key.LeftShift)
            {
                m_isShiftDown = isDown;
            }
            else if(key == Key.G)
            {
                if(isDown && m_isCtlDown && m_isShiftDown)
                {
                    GlobalHotKeyInvoked?.Invoke();
                }
            }
        }

        public IntPtr SetHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_KEYUP)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if(vkCode == 162 || vkCode == 163)
                {
                    s_instance.OnKeyChange(Key.LeftCtrl, wParam == (IntPtr)WM_KEYDOWN);
                }
                else if(vkCode == 160 || vkCode == 161)
                {
                    s_instance.OnKeyChange(Key.LeftShift, wParam == (IntPtr)WM_KEYDOWN);
                }
                else if(vkCode == 71)
                {
                    s_instance.OnKeyChange(Key.G, wParam == (IntPtr)WM_KEYDOWN);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
