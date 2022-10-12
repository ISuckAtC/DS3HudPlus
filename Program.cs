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
    public struct tagPOINT
    {
        public int X;
        public int Y;
    }
    public struct WindowsMessage
    {
        public long hwnd;
        public uint message;
        public uint wParam;
        public ulong lParam;
        public ulong time;
        public tagPOINT pt;
        public ulong lPrivate;
    }

    public struct Vector3
    {
        public float X, Y, Z;
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

        public static float Distance(Vector3 a, Vector3 b)
        {
            return (a - b).Magnitude;
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
        public int ChrType;
        public TeamType TeamType;
        public Vector3 Position;
        public IntPtr PositionPtr;
        public bool Connected;
    }
    public enum TeamType
    {
        Host = 1,
        Phantom = 2,
        BlackPhantom = 3,
        Hollow = 4,
        Enemy = 6,
        Boss = 7,
        Friend = 8,
        AngryFriend = 9,
        DecoyEnemy = 10,
        BloodChild = 11,
        BattleFriend = 12,
        Dragon = 13,
        DarkSpirit = 16,
        Watchdog = 17,
        Aldrich = 18,
        DarkWraith = 24,
        NPC = 26,
        HostileNPC = 27,
        Arena = 29,
        MadPhantom = 31,
        MadSpirit = 32,
        CrabDraon = 33,
        None = 0
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
                    PlayerPointers[i] = Memory.PointerOffset(Memory.WorldChrMan, new long[] { 0x40, 0x38 * (i + 1) });
                }
                CameraPlayerPositionPtr = Memory.PointerOffset(Memory.FieldArea, new long[] { 0x28, 0x60, 0xD0 });
                CameraOffsetPositionPtr = Memory.PointerOffset(Memory.FieldArea, new long[] { 0x28, 0x60, 0xF0 });
                TargetOrbPositionPtr = Memory.PointerOffset(Memory.FieldArea, new long[] { 0x28 });
                // x: 0x13c
                // z: 0x14c
                // y: 0x15c
            }
            else
            {
                PlayerPointers[index] = Memory.PointerOffset(Memory.WorldChrMan, new long[] { 0x40, 0x38 * (index + 1) });
            }
        }


        static public PlayerInfo[] Players;
        static public IntPtr[] PlayerPointers;
        static public PlayerInfo SelfPlayer;
        static public IntPtr SelfPlayerPointer;

        static public IntPtr CameraPlayerPositionPtr;
        static public IntPtr CameraOffsetPositionPtr;
        static public IntPtr TargetOrbPositionPtr;





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

        public static bool ShowUI = true;

        public static bool Targeting;
        public static int LastPlayerTargeted;

        static void Main(string[] args)
        {
            Process ds3a = Process.GetProcessesByName("DarkSoulsIII")[0];


            Memory.Ds3ProcessId = ds3a.Id;
            Memory.DS3Process = Memory.OpenProcess(0x001F0FFF, false, ds3a.Id);
            Memory.DS3Module = ds3a.MainModule;

            Memory.SetBases();



            Console.WriteLine("Hello World!");

            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_TRANSPARENT);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_TOPMOST);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_UNFOCUSED);
            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_MAXIMIZED);

            Raylib.InitWindow(800, 450, "DS3 HudPlus - Raylib");

            Raylib.SetWindowState(ConfigFlags.FLAG_WINDOW_MAXIMIZED);

            Raylib.SetTargetFPS(60);


            //Memory.RegisterHotKey((IntPtr)null, 1, 0x4000, 0x42);



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

            Thread macros = new Thread(() => Macros.Program.MacroLoop());

            macros.Start();
            
            /*
            Thread showUI = new Thread(() =>
            {
                while (true)
                {
                    unsafe
                    {
                        WindowsMessage messagePtr;
                        if (Memory.GetMessage(out messagePtr, (IntPtr)null, 0, 0))
                        {
                            if (messagePtr.message == 0x0312)
                            {
                                Console.WriteLine("Hotkey B was pressed");
                                ShowUI = !ShowUI;
                            }
                        }
                    }
                }
            });
            */

            //showUI.Start();


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

            Memory.ReadProcessMemory(Memory.DS3Process, (IntPtr)((ulong)Memory.DS3Module.BaseAddress + 0x1C), bbb, 16, out bread);

            Console.WriteLine(BitConverter.ToString(BitConverter.GetBytes((ulong)Memory.DS3Module.BaseAddress)) + " | " + BitConverter.ToString(bbb));


            //TestPositions.Add(SelfPlayer.Position);

            int tester = 0;
            System.Diagnostics.Stopwatch sw = new Stopwatch();
            long elapsed = 0;

            

            while (!Raylib.WindowShouldClose())
            {
                if (tester == 20)
                {
                    sw.Start();
                }
                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(0, 0, 0, 0));

                if (tester == 20)
                {
                    elapsed = sw.ElapsedMilliseconds - elapsed;
                    Console.WriteLine(elapsed);
                }

                if (!ShowUI)
                {
                    Raylib.EndDrawing();
                    continue;
                }

                if (tester == 20)
                {
                    elapsed = sw.ElapsedMilliseconds - elapsed;
                    Console.WriteLine(elapsed);
                }



                //Raylib.DrawText(SelfPlayer.Name, 10, 100, 20, Color.WHITE);
                //Raylib.DrawText(SelfPlayer.HP.ToString() + "/" + SelfPlayer.MaxHP.ToString(), 200, 75, 16, Color.WHITE);
                //Raylib.DrawText("(" + SelfPlayer.Position.X + "," + SelfPlayer.Position.Z + ")", 360, 100, 20, Color.WHITE);

                //Raylib.DrawRectangle(100, 100, 300, 100, Color.WHITE);
                Vector3 cameraPlayerPosition = Vector3.FromBytes(Memory.ReadMem(CameraPlayerPositionPtr, 12));
                Vector3 cameraOffsetPosition = Vector3.FromBytes(Memory.ReadMem(CameraOffsetPositionPtr, 12));
                Vector3 direction = cameraPlayerPosition - cameraOffsetPosition;

                /*
                Vector3 targetOrbPos = new Vector3(
                    BitConverter.ToSingle(Memory.ReadMem(Memory.PointerOffset(TargetOrbPositionPtr, new long[] { 0x14c }), 4, 55), 0),
                    BitConverter.ToSingle(Memory.ReadMem(Memory.PointerOffset(TargetOrbPositionPtr, new long[] { 0x15c }), 4, 55), 0),
                    BitConverter.ToSingle(Memory.ReadMem(Memory.PointerOffset(TargetOrbPositionPtr, new long[] { 0x16c }), 4, 55), 0));

                if (targetOrbPos.X != 0 && !Targetting)
                {

                    Targetting = true;
                    TestPositions.Add(targetOrbPos);

                }
                else if (Targetting)
                {
                    Targetting = false;
                    TestPositions.Clear();
                }

                Raylib.DrawText("[" + targetOrbPos.X + ", " + targetOrbPos.Y + ", " + targetOrbPos.Z + "]", 100, 300, 30, Color.RED);
                */

                byte targeting = Memory.ReadMem(Memory.PointerOffset(Memory.LockTgtMan, new long[] { 0x2821 }), 1, 33)[0];
                Targeting = (targeting | 0x00000001) == targeting;

                string bits = "";

                for (int i = 0; i < 8; ++i)
                {
                    bits += ((targeting | (1 << i)) == targeting) ? "1 " : "0 ";
                }

                //Raylib.DrawText(bits, 100, 400, 30, Color.RED);

                IntPtr playerHandlePtr = Memory.PointerOffset(Memory.LockTgtMan, new long[] { 0x8, 0x88, 0x64 });
                if (playerHandlePtr.ToInt64() != 0)
                {
                    //Console.WriteLine(playerHandlePtr.ToInt64());
                    int playerHandle = BitConverter.ToInt32(Memory.ReadMem(playerHandlePtr, 4, 67));
                    int playerId = (playerHandle - 0x10068000) - 1;
                    if (playerId >= 0 && playerId < 5) LastPlayerTargeted = playerId;
                    //Raylib.DrawText(LastPlayerTargeted.ToString(), 100, 300, 30, Color.RED);
                }

                

                if (Targeting && Players[LastPlayerTargeted].Connected)
                {
                    Raylib.DrawText(Players[LastPlayerTargeted].Name, 300, 300, 30, Color.RED);
                    Raylib.DrawText("(" + Players[LastPlayerTargeted].HP + "/" + Players[LastPlayerTargeted].MaxHP + ")", 300, 350, 30, Color.RED);
                    int animation = BitConverter.ToInt32(Memory.ReadMem(Memory.PointerOffset(PlayerPointers[LastPlayerTargeted], new long[] { 0x1f90, 0x80, 0xc8 }), 4, 22));

                    int altAnimation = BitConverter.ToInt32(Memory.ReadMem(Memory.PointerOffset(PlayerPointers[LastPlayerTargeted], new long[] { 0x1f90, 0x28, 0x898 }), 4, 22));

                    Raylib.DrawText(altAnimation.ToString(), 300, 400, 30, Color.RED);

                    //Console.WriteLine(animation);
                    float distance = Vector3.Distance(SelfPlayer.Position, Players[LastPlayerTargeted].Position);

                    Color reactionColor = GetReactionColor(altAnimation, distance);
                    Raylib.DrawRectangle(560, 880, 800, 200, reactionColor);

                    Raylib.DrawText(distance.ToString(), 300, 450, 30, Color.RED);

                    Raylib.DrawText("ChrType: " + Players[LastPlayerTargeted].ChrType, 300, 500, 30, Color.RED);

                    Raylib.DrawText("Team Type: " + Players[LastPlayerTargeted].TeamType.ToString(), 300, 550, 30, Color.RED);
                }

                if (tester == 20)
                {
                    elapsed = sw.ElapsedMilliseconds - elapsed;
                    Console.WriteLine(elapsed);
                }

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

                if (tester == 20)
                {
                    elapsed = sw.ElapsedMilliseconds - elapsed;
                    Console.WriteLine(elapsed);
                }

                if (Raylib.IsKeyPressed(KeyboardKey.KEY_U))
                {
                    IntPtr functionAddress = Memory.DS3Module.BaseAddress + 0xBE2440;
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

                    IntPtr alloc = Memory.VirtualAllocEx(Memory.DS3Process, (IntPtr)null, 100, 0x1000, 0x40);

                    Console.WriteLine("Allocated at: " + alloc.ToString("X"));

                    bool result = Memory.WriteProcessMemory(Memory.DS3Process, alloc, buffer, buffer.Length, out numBytes);
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

                string post = "";

                for (int i = 0; i < Players.Length; ++i)
                {
                    if (Players[i].Connected)
                    {
                        post += (i + "|");
                        Raylib.DrawText(Players[i].Name + " (" + Players[i].HP.ToString() + "/" + Players[i].MaxHP.ToString() + ")", 10, 120 + (i * 20), 16, Color.WHITE);

                        float x = Players[i].Position.X - SelfPlayer.Position.X;
                        float z = Players[i].Position.Z - SelfPlayer.Position.Z;

                        float newX = (float)(x * Math.Cos(angle) - z * Math.Sin(angle));
                        float newZ = (float)(x * Math.Sin(angle) + z * Math.Cos(angle));

                        //Console.WriteLine("Relative Position: " + relativePosition.X + "|" + relativePosition.Z);

                        bool targetPlayer = (Targeting && i == LastPlayerTargeted);

                        int scaledX = (int)(newX * 8f);
                        int scaledY = (int)(-newZ * 8f);

                        float drawDistance = Vector3.Distance(new Vector3(scaledX, 0, scaledY), new Vector3(0, 0, 0));

                        Color playerColor;
                        switch (Players[i].TeamType)
                        {
                            case TeamType.Host:
                                playerColor = Color.GRAY;
                                break;
                            case TeamType.DarkSpirit:
                                playerColor = Color.RED;
                                break;
                            case TeamType.Aldrich:
                                playerColor = Color.DARKPURPLE;
                                break;
                            case TeamType.MadSpirit:
                                playerColor = Color.PURPLE;
                                break;
                            case TeamType.MadPhantom:
                                playerColor = Color.PINK;
                                break;
                            default:
                                playerColor = Color.BLACK;
                                break;
                        }

                        if (drawDistance <= 125) 
                        {
                            if (targetPlayer) Raylib.DrawCircle(1750 + scaledX, 170 + scaledY, 6, Color.BLACK);
                            Raylib.DrawCircle(1750 + scaledX, 170 + scaledY, 4, playerColor);
                            
                        }
                    }
                }
                if (post.Length > 0) //Console.WriteLine(post);

                    if (tester == 20)
                    {
                        elapsed = sw.ElapsedMilliseconds - elapsed;
                        Console.WriteLine(elapsed);
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

                if (tester == 20)
                {
                    elapsed = sw.ElapsedMilliseconds - elapsed;
                    Console.WriteLine(elapsed);
                }

                //Raylib.DrawRectangle(0, 0, 50, 50, Color.RED);
                Raylib.EndDrawing();
            }
        }

        public static IntPtr CurrentTargetPlayerPtr()
        {
            return new IntPtr();
        }

        public static Color GetReactionColor(int animation, float distance)
        {
            if (animation == 0) 
            {
                return Color.GREEN;
            }
            if (animation == 7602241 && distance < 4)
            {
                return Color.RED;
            }
            return Color.BEIGE;
        }


        public static bool CheckConnection(int index, bool player2)
        {
            SelfPlayer.HPPtr = Memory.PointerOffset(Memory.WorldChrMan, new long[] { 0x80, 0x1f90, 0x18, 0xd8 });
            SelfPlayer.MaxHPPtr = Memory.PointerOffset(Memory.WorldChrMan, new long[] { 0x80, 0x1f90, 0x18, 0xdc });
            SelfPlayer.PositionPtr = Memory.PointerOffset(Memory.WorldChrMan, new long[] { 0x40, 0x28, 0x80 });
            SetPointers(index);
            if (BitConverter.ToInt64(Memory.ReadMem(PlayerPointers[index], 8, 2)) != 0)
            {
                byte[] nameBytes = Memory.ReadMem(Memory.PointerOffset(PlayerPointers[index], new long[] { 0x1FA0, 0x88 }), 32);
                string name = Encoding.Unicode.GetString(nameBytes).Split('\0')[0];

                Players[index].HPPtr = Memory.PointerOffset(PlayerPointers[index], new long[] { 0x1FA0, 0x18 });
                Players[index].MaxHPPtr = Memory.PointerOffset(PlayerPointers[index], new long[] { 0x1FA0, 0x1C });
                Players[index].PositionPtr = Memory.PointerOffset(PlayerPointers[index], new long[] { 0x18, 0x28, 0x80 });
                Players[index].ChrType = BitConverter.ToInt32(Memory.ReadMem(Memory.PointerOffset(PlayerPointers[index], new long[] { 0x70 }), 4, 1));
                Players[index].TeamType = (TeamType)BitConverter.ToInt32(Memory.ReadMem(Memory.PointerOffset(PlayerPointers[index], new long[] { 0x74 }), 4, 1));

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
                    if (Memory.chilledCallers.Contains(0)) continue;

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
