using Interceptor;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageCompare.Classes
{
    //test commit
    public static class DxInput
    {
        private static readonly Random rand = new Random();

        public static Input input = new Input
        {
            KeyboardFilterMode = KeyboardFilterMode.KeyUp
        };

        public static bool IsLoaded
        {
            get => input.IsLoaded;
            set { }
        }

        public static void Unload()
        {
            input.Unload();
        }

        public static void SendKey(Interceptor.Keys key)
        {
            DxInput.input.KeyPressDelay = rand.Next(10, 20);
            DxInput.input.SendKey(key);
        }
        public static void SendMouseEvent(Interceptor.MouseState key)
        {
            Thread.Sleep(rand.Next(10, 20));
            DxInput.input.SendMouseEvent(Interceptor.MouseState.RightExtraDown);
        }

        public static void initInput()
        {
            try
            {
                input.Load();
                input.OnKeyPressed += Input_OnKeyPressed;
            }
            catch ( Exception ex )
            {
                Console.Clear();
                Console.WriteLine("Unable to initialize DirectX input capture");
                Console.WriteLine("Run installer?");
                var confirm = Console.ReadKey();
                Console.Clear();
                if ( confirm.KeyChar == 'y' )
                {
                    var proc = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "lib/install-lib.exe" ,
                            Arguments = "/install" ,
                            UseShellExecute = false ,
                            RedirectStandardOutput = true ,
                            CreateNoWindow = true
                        }
                    };

                    proc.Start();
                    var count = 0;
                    while ( !proc.StandardOutput.EndOfStream )
                    {
                        var line = proc.StandardOutput.ReadLine();
                        if ( count >= 4 )
                        {
                            Console.WriteLine(line.Replace("Interception" , "Library"));
                        }
                        count += 1;
                    }

                    input.Unload();
                    Console.ReadKey();
                    Environment.Exit(-3);
                }
            }
        }

        private static void Input_OnKeyPressed(object sender , KeyPressedEventArgs e)
        {
            if (e.State == KeyState.Up)
            {
                if (e.Key == Interceptor.Keys.F10)
                {
                    e.Handled = true;

                    Program.paused = !Program.paused;
                    var state = Program.paused ? "paused" : "resumed";
                    Console.WriteLine($"Program {state}");
                }
                else if (e.Key == Interceptor.Keys.Escape)
                {
                    Program.running = false;
                }
                else if (e.Key == Interceptor.Keys.F9)
                {
                    Console.WriteLine("Scheduling ss");
                    Program.scheduleScreenshot = true;
                }
            }
        }
    }
}
