using ImageCompare.Classes;
using ImageCompare.Classes.Skills;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Console = ImageCompare.Classes.Console;

namespace ImageCompare
{
    class Program
    {
        static ConsoleEventDelegate handler;
        private delegate bool ConsoleEventDelegate(int eventType);
        [DllImport("kernel32.dll" , SetLastError = true)]
        private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback , bool add);

        private static BaseSkill currSkill = null;
        private static List<long> avgTicks = new List<long>();

        public static bool scheduleScreenshot = false;
        public static BaseSkill.Mode mode = BaseSkill.Mode.None;
        public static bool paused = true;
        public static bool running = true;
        public static int animCount = 0;
        public static char[] animChar = new char[] { '|', '/', '-', '\\' };
        public static DateTime lastAnimTick = DateTime.Now;
        public static int animDelay = 250; //Ticks to secs        

        static void Main(string[] args)
        {
            handler = new ConsoleEventDelegate(ConsoleEventCallback);
            SetConsoleCtrlHandler(handler , true);
            var CoreCount = System.Environment.ProcessorCount / 2;
            var maxThreads = ( Environment.ProcessorCount * CoreCount );            
            DxInput.initInput();
            var sw = new Stopwatch();

            Console.Title = $"Max concurrent threads {maxThreads}";
            askMode();

            running = true;            
            new Task(() => { ScreenDrawer.printDebug(); }).Start();
            while ( running )
            {
                sw.Restart();

                if (!Program.paused)
                {
                    currSkill.takeAction();
                }

                sw.Stop();
                var ticks = sw.ElapsedTicks;
                if ( ticks < 3000 )
                {
                    avgTicks.Add(ticks);

                    avgTicks.Sort();

                    var Ispaused = Program.paused ? "paused" : "running";
                    Console.Title = $"{currSkill.getSkillName()} - {avgTicks.Average().ToString("#,##0")}Avg. {avgTicks.First()}min {avgTicks.Last()}max {animChar[animCount]} {Ispaused}";
                    if ( avgTicks.Count > 20000 )
                    {
                        avgTicks = new List<long>();
                    }
                }

                if ( lastAnimTick.AddMilliseconds(animDelay) < DateTime.Now )
                {
                    lastAnimTick = DateTime.Now;
                    animCount += 1;
                    if ( animCount >= animChar.Length )
                    {
                        animCount = 0;
                    }
                }
            }

            DxInput.Unload();
            Console.ReadKey();            
        }

        static bool ConsoleEventCallback(int eventType)
        {
            if ( eventType == 2 )
            {
                if ( DxInput.IsLoaded )
                {
                    DxInput.Unload();
                }
            }
            return false;
        }

        public static void askMode()
        {
            Console.WriteLine("Which mode?");
            Console.WriteLine("");
            Console.WriteLine("1. Fishing");
            Console.WriteLine("2. Mining");
            Console.WriteLine("3. Builder");
            Console.WriteLine("4. Auto");
            Console.WriteLine("");

            var confirm = Console.ReadKey();
            Console.Clear();
            setMode(confirm.KeyChar.ToString());
        }

        public static void setMode(string KeyChar)
        {
            if ( KeyChar == "1" )
            {
                mode = BaseSkill.Mode.Fishing;
                currSkill = new Fishing();
            }
            else if ( KeyChar == "2" )
            {
                mode = BaseSkill.Mode.Mining;
                currSkill = new Mining();
            }
            else if (KeyChar == "3")
            {
                mode = BaseSkill.Mode.Builder;
                currSkill = new Builder();
            }
            else if (KeyChar == "4")
            {
                mode = BaseSkill.Mode.Auto;
                currSkill = new Auto();
            }
            else
            {
                Console.WriteLine("No valid mode");

                DxInput.input.Unload();
                Console.ReadKey();
                Environment.Exit(0);
            }
        }

        static int PhysicalProcessorCount()
        {
            var processors = new ArrayList();
            foreach ( var mo in new ManagementClass("Win32_Processor").GetInstances() )
            {
                var id = (string)mo.Properties["SocketDesignation"].Value;
                if ( !processors.Contains(id) )
                {
                    processors.Add(id);
                }
            }
            return processors.Count;
        }
    }
}
