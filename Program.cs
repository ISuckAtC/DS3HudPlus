using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Raylib_cs;
using System.Diagnostics;

namespace DS3HudPlus
{
    public struct Vector3
    {
        public float X,Y,Z;
        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);
        public Vector3 Normalized { get { return new Vector3(X / Magnitude, Y / Magnitude, Z / Magnitude); } }
        public Vector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3 FromBytes(byte[] bytes)
        {
            return new Vector3(BitConverter.ToSingle(bytes, 0), BitConverter.ToSingle(bytes, 4), BitConverter.ToSingle(bytes, 8));
        }

        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3 operator -(Vector3 a, Vector3 b) => new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3 operator -(Vector3 a) => new Vector3(-a.X, -a.Y, -a.Z);
    }
    public struct PlayerInfo
    {
        public int HP;
        public IntPtr HPPtr;
        public int MaxHP;
        public IntPtr MaxHPPtr;
        public string Name;
        public string ChrType;
        public Vector3 Position;
        public IntPtr PositionPtr;
        public bool Connected;
    }
    class Program
    {
        static void SetPointers(int index)
        {
            if (index < 0)
            {
                PlayerPointers = new IntPtr[5];
                Players = new PlayerInfo[5];
                for (int i = 0; i < 5; ++i)
                {
                    PlayerPointers[i] = Memory.PointerOffset(Memory.BaseB, new long[] { 0x40, 0x38 * (i + 1) });
                }
            }
            else
            {
                PlayerPointers[index] = Memory.PointerOffset(Memory.BaseB, new long[] { 0x40, 0x38 * (index + 1) });
                CameraPlayerPositionPtr = Memory.PointerOffset(Memory.BaseD, new long[] { 0x28, 0x60, 0xD0});
                CameraOffsetPositionPtr = Memory.PointerOffset(Memory.BaseD, new long[] { 0x28, 0x60, 0xF0});
            }
        }

        
        static public PlayerInfo[] Players;
        static public IntPtr[] PlayerPointers;
        static public PlayerInfo SelfPlayer;
        static public IntPtr SelfPlayerPointer;

        static public IntPtr CameraPlayerPositionPtr;
        static public IntPtr CameraOffsetPositionPtr;

        

        

        public static int Player1Index, Player2Index;

        public static long StartStyle;

        public int prevHealth1 = -1;
        public int prevHealth2 = -1;

        public int healthChange1;
        public int healthChange2;

        public int damageTimeOut1 = 0;
        public int damageTimeOut2 = 0;

        public int damageTimeOutLimit = 120;
        public static List<Vector3> TestPositions;
        public static IntPtr ThreadIdPtr;
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_TRANSPARENT);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_TOPMOST);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_UNFOCUSED);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_MAXIMIZED);

            Raylib.InitWindow(800, 450, "DS3 HudPlus - Raylib");

            //Raylib.SetWindowState(ConfigFlags.FLAG_WINDOW_MAXIMIZED);

            Raylib.SetTargetFPS(60);


            Process ds3 = Process.GetProcessesByName("DarkSoulsIII")[0];
            Memory.SetBases(ds3);

            SetPointers(-1);



            Player1Index = -1;
            Player2Index = -1;

            Thread playerData = new Thread(() => WatchPlayerData());

            playerData.Start();

            Thread watchConnections = new Thread(() =>
            {
                while (true)
                {
                    for (int i = 0; i < Players.Length; ++i)
                    {
                        CheckConnection(i, false);
                    }
                    Thread.Sleep(1000);
                }
            });

            watchConnections.Start();


            //damageTimeOutLimit = 60;

            Thread.Sleep(500);

            StartStyle = Memory.GetWindowLong(Raylib.GetWindowHandle(), -16);
            long setwindow = Memory.SetWindowLong(Raylib.GetWindowHandle(), -16, (StartStyle | 0x00000000L | 0x01000000L) & ~(0x00C00000L | 0x00040000L | 0x00010000L));

            long startStyleEx = Memory.GetWindowLong(Raylib.GetWindowHandle(), -20);
            long setwindowEx = Memory.SetWindowLong(Raylib.GetWindowHandle(), -20, startStyleEx | 0x80000 | 0x20);

            Memory.SetWindowPos(Raylib.GetWindowHandle(), (IntPtr)null, 0, 0, 1920, 1080, 0x0020);
            

            Memory.lastErr = System.Runtime.InteropServices.Marshal.GetLastWin32Error();

            if (setwindow == 0)
            {
                Console.WriteLine("ERROR: " + Memory.lastErr + " | caller: SetWindowLong");
            }
            
            TestPositions = new List<Vector3>();

            byte[] bbb = new byte[16];

            IntPtr bread = new IntPtr();

            Memory.ReadProcessMemory(Memory.DS3Handle, (IntPtr)((ulong)Memory.BaseDS3 + 0x1C), bbb, 16, out bread);

            Console.WriteLine(BitConverter.ToString(BitConverter.GetBytes((ulong)Memory.BaseDS3)) + " | " + BitConverter.ToString(bbb));


            while (!Raylib.WindowShouldClose())
            {
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(0, 0, 0, 0));
                
                Raylib.DrawText(SelfPlayer.Name, 10, 100, 20, Color.WHITE);
                Raylib.DrawText(SelfPlayer.HP.ToString() + "/" + SelfPlayer.MaxHP.ToString(), 200, 75, 16, Color.WHITE);
                //Raylib.DrawText("(" + SelfPlayer.Position.X + "," + SelfPlayer.Position.Z + ")", 360, 100, 20, Color.WHITE);

                //Raylib.DrawRectangle(100, 100, 300, 100, Color.WHITE);
                Vector3 cameraPlayerPosition =  Vector3.FromBytes(Memory.ReadMem(CameraPlayerPositionPtr, 12));
                Vector3 cameraOffsetPosition =  Vector3.FromBytes(Memory.ReadMem(CameraOffsetPositionPtr, 12));
                Vector3 direction = cameraPlayerPosition - cameraOffsetPosition;

                direction.Y = 0;
                
                direction = direction.Normalized;

                float angle = (float)Math.Atan2(direction.X, direction.Z);

                //Raylib.DrawText(direction.X.ToString("0.00") + "," + direction.Y.ToString("0.00") + "," + direction.Z.ToString("0.00") + "(" + angle + ")", 100, 100, 20, Color.BLACK);

                Raylib.DrawCircle(1750, 170, 130, Color.BLACK);
                Raylib.DrawCircle(1750, 170, 127, Color.WHITE);
                //Raylib.DrawRectangleLinesEx(new Rectangle(1600, 20, 300, 300), 3, Color.BLACK);
                //Raylib.DrawRectangle(1603, 23, 294, 294, Color.WHITE);

                if (Raylib.IsKeyPressed(KeyboardKey.KEY_Y))
                {
                    TestPositions.Add(SelfPlayer.Position);
                    Console.WriteLine("Added test position");
                }

                if (Raylib.IsKeyPressed(KeyboardKey.KEY_U))
                {
                    IntPtr functionAddress = Memory.BaseDS3 + 0xBE2440;
                    /*
                    sub rsp,48
                    lea rcx,[rsp+28]
                    call BE244042
                    add rsp,48
                    ret
                    */
                    byte[] buffer = new byte[]
                    {
                        0x48, 0x83, 0xEC, 0x48, 0x48, 0x8D, 0x4C, 0x24, 0x28, 0xE8, 0x16, 0x24, 0xBE, 0x00, 0x48, 0x83, 0xC4, 0x48, 0xC3
                    };


                    IntPtr numBytes;

                    IntPtr alloc = Memory.VirtualAllocEx(Memory.DS3Handle, (IntPtr)null, 100, 0x1000, 0x40);

                    Console.WriteLine("Allocated at: " + alloc.ToString("X"));
                    
                    bool result = Memory.WriteProcessMemory(Memory.DS3Handle, alloc, buffer, buffer.Length, out numBytes);
                    Memory.lastErr = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    if (Memory.lastErr != 0) Console.WriteLine("ERROR: " + Memory.lastErr);
                    /*IntPtr threadHandle = Memory.CreateRemoteThread(Memory.DS3Handle, (IntPtr)null, 128, alloc, (IntPtr)null, 0, out ThreadIdPtr);
                    if (threadHandle == null)
                    {
                        Memory.lastErr = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                        Console.WriteLine("ERROR: " + Memory.lastErr + " | caller: CreateRemoteThread");
                    }*/

                    Console.WriteLine("Done");
                }

                for (int i = 0; i < Players.Length; ++i)
                {
                    if (Players[i].Connected)
                    {
                        Raylib.DrawText(Players[i].Name, 10, 120 + (i * 20), 20, Color.WHITE);
                        Raylib.DrawText(Players[i].HP.ToString() + "/" + Players[i].MaxHP.ToString(), 260, 120 + (i * 20), 20, Color.WHITE);
                        Raylib.DrawText("(" + Players[i].Position.X + "," + Players[i].Position.Z + ")", 360, 120 + (i * 20), 20, Color.WHITE);

                        float x = Players[i].Position.X - SelfPlayer.Position.X;
                        float z = Players[i].Position.Z - SelfPlayer.Position.Z;

                        float newX = (float)(x * Math.Cos(angle) - z * Math.Sin(angle));
                        float newZ = (float)(x * Math.Sin(angle) + z * Math.Cos(angle));

                        //Console.WriteLine("Relative Position: " + relativePosition.X + "|" + relativePosition.Z);

                        Raylib.DrawCircle(1750 + (int)(newX * 4f), 170 + (int)(-newZ * 4f), 3, Color.RED);
                    }
                }

                for (int i = 0; i < TestPositions.Count; ++i)
                {
                    float x = TestPositions[i].X - SelfPlayer.Position.X;
                    float z = TestPositions[i].Z - SelfPlayer.Position.Z;

                    float newX = (float)(x * Math.Cos(angle) - z * Math.Sin(angle));
                    float newZ = (float)(x * Math.Sin(angle) + z * Math.Cos(angle));

                    Raylib.DrawCircle(1750 + (int)(newX * 4f), 170 + (int)(-newZ * 4f), 3, Color.BLUE);
                }


                

                Raylib.DrawCircle(1750, 170, 3, Color.BLACK);

                
                Raylib.DrawRectangle(0, 0, 50, 50, Color.RED);
                Raylib.EndDrawing();
            }
        }


        public static bool CheckConnection(int index, bool player2)
        {
            SelfPlayer.HPPtr = Memory.PointerOffset(Memory.BaseB, new long[] { 0x80, 0x1f90, 0x18, 0xd8 });
            SelfPlayer.MaxHPPtr = Memory.PointerOffset(Memory.BaseB, new long[] { 0x80, 0x1f90, 0x18, 0xdc });
            SelfPlayer.PositionPtr = Memory.PointerOffset(Memory.BaseB, new long[] { 0x40, 0x28, 0x80 });
            SetPointers(index);
            if (BitConverter.ToInt64(Memory.ReadMem(PlayerPointers[index], 8, 2)) != 0)
            {
                byte[] nameBytes = Memory.ReadMem(Memory.PointerOffset(PlayerPointers[index], new long[] { 0x1FA0, 0x88 }), 32);
                string name = Encoding.Unicode.GetString(nameBytes).Split('\0')[0];
                Players[index].HPPtr = Memory.PointerOffset(PlayerPointers[index], new long[] { 0x1FA0, 0x18 });
                Players[index].MaxHPPtr = Memory.PointerOffset(PlayerPointers[index], new long[] { 0x1FA0, 0x1C });
                Players[index].PositionPtr = Memory.PointerOffset(PlayerPointers[index], new long[] { 0x18, 0x28, 0x80 });

                if (!Players[index].Connected && Players[index].Name != name)
                {
                    Players[index].Name = name;
                    Console.WriteLine("\nPlayer [" + Players[index].Name + "] connected!");
                    Players[index].Connected = true;
                }

                return true;
            }
            if (Players[index].Connected)
            {
                Console.WriteLine("\nPlayer [" + Players[index].Name + "] disconnected!");
                Players[index].Connected = false;
            }
            return false;
        }

        public static void WatchPlayerData()
        {
            try
            {
                while (true)
                {
                    if (Memory.chill) continue;

                    SelfPlayer.HP = BitConverter.ToInt32(Memory.ReadMem(SelfPlayer.HPPtr, 4, 2), 0);
                    SelfPlayer.MaxHP = BitConverter.ToInt32(Memory.ReadMem(SelfPlayer.MaxHPPtr, 4, 2), 0);
                    byte[] selfPosBytes = Memory.ReadMem(SelfPlayer.PositionPtr, 12, 2);
                    SelfPlayer.Position = new Vector3(BitConverter.ToSingle(selfPosBytes, 0), BitConverter.ToSingle(selfPosBytes, 4), BitConverter.ToSingle(selfPosBytes, 8));

                    for (int i = 0; i < Players.Length; ++i)
                    {
                        if (Players[i].Connected)
                        {
                            Players[i].HP = BitConverter.ToInt32(Memory.ReadMem(Players[i].HPPtr, 4, 10 + i), 0);
                            Players[i].MaxHP = BitConverter.ToInt32(Memory.ReadMem(Players[i].MaxHPPtr, 4, 10 + i), 0);
                            byte[] posBytes = Memory.ReadMem(Players[i].PositionPtr, 12, 10 + i);
                            Players[i].Position = new Vector3(BitConverter.ToSingle(posBytes, 0), BitConverter.ToSingle(posBytes, 4), BitConverter.ToSingle(posBytes, 8));
                        }
                    }
                    Thread.Sleep(16);
                }
            }
            catch (Exception e) { Console.WriteLine(e.Message + "\n" + e.StackTrace); }
        }
    }
}
