using System;
using System.Threading;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace DS3HudPlus
{
    public static class Memory
    {
        #region Dll
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        [Out] byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesRead
        );

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(
        IntPtr hProcess,
        IntPtr lpBaseAddress,
        byte[] lpBuffer,
        int dwSize,
        out IntPtr lpNumberOfBytesWritten
        );

        [DllImport("User32.dll", SetLastError = true)]
        static extern short GetKeyState(int nVirtKey);

        [DllImport("User32.dll", SetLastError = true)]
        public static extern long GetWindowLong(
        IntPtr handle,
        int nIndex
        );

        [DllImport("User32.dll", SetLastError = true)]
        public static extern long SetWindowLong(
        IntPtr handle,
        int nIndex,
        long dwNewLong
        );

        [DllImport("User32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr hWndInsertAfter,
        int X,
        int Y,
        int cx,
        int cy,
        uint uFlags
        );

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr CreateRemoteThread(
        IntPtr hProcess,
        IntPtr lpThreadAttributes,
        uint dwStackSize,
        IntPtr lpStartAddress,
        IntPtr lpParameter,
        uint dwCreationFlags,
        out IntPtr lpThreadId
        );

        [DllImport("Kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAllocEx(
        IntPtr hProcess,
        IntPtr lpAddress,
        uint dwSize,
        uint flAllocationType,
        uint flProtect
        );
        #endregion

        #region BasePointers
        static public IntPtr BaseDS3;
        static public IntPtr BaseA;
        static public IntPtr BaseB;
        static public IntPtr BaseC;
        static public IntPtr BaseD;
        static public IntPtr BaseE;
        static public IntPtr BaseF;
        static public IntPtr BaseZ;
        static public IntPtr Param;
        static public IntPtr GameFlagData;
        static public IntPtr LockBonus_ptr;
        static public IntPtr DrawNearOnly_ptr;
        static public IntPtr debug_flags;
        #endregion

        static public IntPtr DS3Handle;
        static int Ds3ProcessId;
        static public int lastErr;
        static public bool chill;

        

        static public void SetBases(Process ds3)
        {
            DS3Handle = ds3.Handle;
            Ds3ProcessId = ds3.Id;
            BaseDS3 = ds3.MainModule.BaseAddress;
            BaseA = new IntPtr(BaseDS3.ToInt64() + 0x4740178);
            BaseB = new IntPtr(BaseDS3.ToInt64() + 0x4768E78);
            BaseC = new IntPtr(BaseDS3.ToInt64() + 0x4743AB0);
            BaseD = new IntPtr(BaseDS3.ToInt64() + 0x4743A80);
            BaseE = new IntPtr(BaseDS3.ToInt64() + 0x473FD08);
            BaseF = new IntPtr(BaseDS3.ToInt64() + 0x473AD78);
            BaseZ = new IntPtr(BaseDS3.ToInt64() + 0x4768F98);
            Param = new IntPtr(BaseDS3.ToInt64() + 0x4782838);
            GameFlagData = new IntPtr(BaseDS3.ToInt64() + 0x473BE28);
            LockBonus_ptr = new IntPtr(BaseDS3.ToInt64() + 0x4766CA0);
            DrawNearOnly_ptr = new IntPtr(BaseDS3.ToInt64() + 0x4766555);
            debug_flags = new IntPtr(BaseDS3.ToInt64() + 0x4768F68);
        }

        static public byte[] ReadMem(IntPtr baseAdd, int size, int caller = 0)
        {
            byte[] buf = new byte[size];
            IntPtr bRead = new IntPtr();
            DS3Handle = Process.GetProcessesByName("DarkSoulsIII")[0].Handle;
            ReadProcessMemory(Process.GetProcessById(Ds3ProcessId).Handle, baseAdd, buf, size, out bRead);
            lastErr = Marshal.GetLastWin32Error();
            if (lastErr != 0)
            {
                Console.WriteLine("ERROR: " + lastErr + " | caller: " + caller);
                if (lastErr == 6 || lastErr == 299)
                {
                    DS3Handle = Process.GetProcessesByName("DarkSoulsIII")[0].Handle;
                    if (!chill)
                    {
                        Console.WriteLine("Entering chill zone");
                        chill = true;
                        Thread chillout = new Thread(() =>
                        {
                            Thread.Sleep(2000);
                            Console.WriteLine("Exiting chill zone");
                            chill = false;
                        });
                        chillout.Start();
                    }

                }
            }
            return buf;
        }
        static public IntPtr PointerOffset(IntPtr ptr, long[] offsets)
        {

            foreach (long offset in offsets)
            {
                ptr = new IntPtr(BitConverter.ToInt64(ReadMem(ptr, 8)) + offset);
            }
            return ptr;
        }
    }
}