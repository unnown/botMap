﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageCompare.Classes
{
    public static class ScreenDrawer
    {
        [DllImport("User32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);
        [DllImport("User32.dll")]
        public static extern void ReleaseDC(IntPtr hwnd , IntPtr dc);

        private static System.Drawing.Font font = new System.Drawing.Font("Arial" , 16);
        private static System.Drawing.SolidBrush brush = new System.Drawing.SolidBrush(System.Drawing.Color.White);

        private static List<String> lines = new List<string>();

        public static void printDebug()
        {
            while (Program.running)
            {
                if (!Program.paused)
                {
                    var desktopPtr = GetDC(IntPtr.Zero);
                    var g = Graphics.FromHdc(desktopPtr);

                    try
                    {
                        var count = 0;
                        lock (lines)
                        {
                            foreach (var line in lines)
                            {
                                g.DrawString(line, font, brush, new PointF(20, 50 + (18 * count)));
                                count += 1;
                            }
                        }
                    }
                    catch (Exception ex) { }

                    g.Dispose();
                    ReleaseDC(IntPtr.Zero, desktopPtr);
                }
            }
        }

        public static void drawText(string text, bool append = false)
        {
            lock (lines)
            {
                if (lines != null && lines.Count > 0 && lines.Last() == text)
                {
                    return;
                }

                if (!append)
                {
                    lines = new List<string>();
                }
                lines.Add(text);
                
            }
        }
    }
}
