using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace gh
{


    public class Pattern
    {
        private IntPtr process;
        ProcessModule mod;
        private string pattern;

        public Pattern(string procname, string _pattern)
        {
            Process[] p = Process.GetProcessesByName(procname);
            if (0 == p.Length)
            {
                //MessageBox.Show("no processes found");
                return;
            }
            process = OpenProcess(ProcessAccessFlags.All, false,
                p[0].Id);
            mod = p[0].MainModule;
            pattern = _pattern;
        }

        private bool checkPattern(string pattern, byte[] array2check)
        {
            int len = array2check.Length;
            string[] strBytes = pattern.Split(' ');
            int x = 0;
            foreach (byte b in array2check)
            {
                if (strBytes[x] == "?" || strBytes[x] == "??")
                {
                    x++;
                }
                else if (byte.Parse(strBytes[x], NumberStyles.HexNumber) == b)
                {
                    x++;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(
        ProcessAccessFlags processAccess, bool bInheritHandle, int processId);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(
    IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, Int32 nSize, out IntPtr lpNumberOfBytesRead);
        public List<IntPtr> Adress
        {
            get
            {
                byte[] moduleMemory = new byte[mod.ModuleMemorySize];
                IntPtr read = IntPtr.Zero;
                ReadProcessMemory(process, mod.BaseAddress, moduleMemory,
                    mod.ModuleMemorySize, out read);

                string[] splitPattern = pattern.Split(' ');
                List<IntPtr> adressList = new List<IntPtr>();
                try
                {
                    for (int y = 0; y < moduleMemory.Length; y++)
                    {
                        if (moduleMemory[y] == byte.Parse(splitPattern[0], NumberStyles.HexNumber))
                        {
                            byte[] checkArray = new byte[splitPattern.Length];
                            for (int x = 0; x < splitPattern.Length; x++)
                            {
                                checkArray[x] = moduleMemory[y + x];
                            }
                            if (checkPattern(pattern, checkArray))
                            {
                                adressList.Add((IntPtr)((uint)mod.BaseAddress + y));
                            }
                            else
                            {
                                y += splitPattern.Length - (splitPattern.Length / 2);
                            }
                        }
                    }
                    return adressList;
                }
                catch (Exception)
                {
                    throw new Exception("Could not check the pattern");
                }
            }
        }

    }
}