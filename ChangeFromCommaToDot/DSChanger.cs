using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace ChangeFromCommaToDot
{
    internal class DSChanger
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const string CONFIG_FILE_NAME = "DSChanger.config";
        private static  LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private IntPtr handle;

#if DEBUG
        private const int SW_HIDE = 1;
#else
        private const int SW_HIDE = 0;
#endif

        private static ushort fromKey;
        private static ushort toKey;

        public DSChanger()
        {
            ReadSettings();
            handle = GetCWindow();
            SW();
        }

        private void ReadSettings()
        {
            if (!File.Exists(CONFIG_FILE_NAME))
            {
                throw new FileNotFoundException($"Can NOT find {CONFIG_FILE_NAME}!");
            }
            string[] configLines = File.ReadAllLines(CONFIG_FILE_NAME);
            if (configLines.Length != 2)
            {
                throw new ArgumentOutOfRangeException($"In {CONFIG_FILE_NAME} are expected 2 lines but there is {configLines.Length} lines!");
            }
            try
            {
                fromKey = Convert.ToUInt16(configLines[0]);
                toKey = Convert.ToUInt16(configLines[1]);
                CheckValuesFromConfig(fromKey, toKey);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Configure strings in {CONFIG_FILE_NAME} are not suitable! {ex.Message}");
            }
        }

        private void CheckValuesFromConfig(ushort fromKey, ushort toKey)
        {
            if (!Enum.IsDefined(typeof(Keys), (Keys)fromKey))
            {
                throw new ArgumentOutOfRangeException($"FromKey {fromKey} in {CONFIG_FILE_NAME} is not proper");
            }
            if (!Enum.IsDefined(typeof(Keys), (Keys)toKey))
            {
                throw new ArgumentOutOfRangeException($"FromKey {toKey} in {CONFIG_FILE_NAME} is not proper");
            }
        }

        private void CreateConfigFile()
        {
            File.Create(CONFIG_FILE_NAME);
        }

        internal void SetHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        internal delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {

            if (nCode >= 0 && wParam == (IntPtr) WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Console.WriteLine($"{vkCode} {(Keys)vkCode}");
                if ((Keys) fromKey == (Keys) vkCode)
                {
                    SEND_INPUT_FOR_32_BIT singleInput = new SEND_INPUT_FOR_32_BIT();
                    singleInput.InputType = 1;
                    singleInput.KeyboardInputStruct = new KEYBOARD_INPUT_FOR_32_BIT();
                    singleInput.KeyboardInputStruct.VirtualKeyCode = toKey;

                    SEND_INPUT_FOR_32_BIT[] theinput = new SEND_INPUT_FOR_32_BIT[]
                    {
                        singleInput
                    };

                    int sizeOfInputVariable = Marshal.SizeOf(typeof(SEND_INPUT_FOR_32_BIT));
                    SendInput(1, theinput, sizeOfInputVariable);

                    return (IntPtr) 1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        internal void UnhookWindowsHookEx()
        {
            UnhookWindowsHookEx(_hookID);
        }

        internal IntPtr GetCWindow()
        {
            return GetConsoleWindow();
        }

        internal bool SW(int nCmdShow = SW_HIDE)
        {
            return ShowWindow(handle, nCmdShow);
        }

        #region int32 staff

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        internal struct SEND_INPUT_FOR_32_BIT
        {
            [FieldOffset(0)]
            public uint InputType;
            [FieldOffset(4)]
            public KEYBOARD_INPUT_FOR_32_BIT KeyboardInputStruct;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct KEYBOARD_INPUT_FOR_32_BIT
        {
            public ushort VirtualKeyCode;
            public ushort ScanCode;
            public uint Flags;
            public uint Time;
            public uint ExtraInfo;
            public uint Padding1;
            public uint Padding2;
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

        [DllImport("kernel32.dll")]
        static private extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SendInput")]
        static extern UInt32 SendInput(UInt32 numInputs, [MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] SEND_INPUT_FOR_32_BIT[] sendInputsFor, Int32 cbSize);
        #endregion
    }
}
