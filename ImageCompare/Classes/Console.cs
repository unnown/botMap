using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageCompare.Classes
{
    /// <summary>
    /// Class to overwrite default console (debug to screen)
    /// </summary>
    public static class Console
    {
        public static string Title
        {
            get => System.Console.Title;
            set => System.Console.Title = value;
        }

        public static ConsoleKeyInfo ReadKey()
        {
            return System.Console.ReadKey();
        }

        public static void WriteLine(string text)
        {            
            ScreenDrawer.drawText(text , !string.IsNullOrEmpty(text));
            System.Console.WriteLine(text);
        }

        public static void Clear()
        {
            ScreenDrawer.drawText("" , false);
            System.Console.Clear();
        }
    }
}
