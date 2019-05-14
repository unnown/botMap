using AForge.Imaging;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImageCompare.Classes.Skills
{
    public class BaseSkill
    {
        public Random rand = new Random();
        public int imgWidth = (int)Math.Floor(Screen.PrimaryScreen.Bounds.Width * 0.15);
        public int imgHeight = (int)Math.Floor(Screen.PrimaryScreen.Bounds.Height * 0.15);
        public int startX = 0;
        public int startY = 0;
        public string SkillName = "";

        public BaseSkill() { }

        public virtual void takeAction() { }
        public string getSkillName() { return this.SkillName; }

        public enum Mode
        {
            None = 0,
            Fishing = 1,
            Mining = 2,
            Builder = 3
        }

        public static void SaveFoundArea(UnmanagedImage image, TemplateMatch match)
        {
            var img = image.ToManagedImage();
            if (img.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                return;
            }

            using ( var graphics = Graphics.FromImage(img) )
            {
                using ( var myPen = new System.Drawing.Pen(System.Drawing.Color.Red, 2) )
                {
                    graphics.DrawRectangle(myPen , match.Rectangle);
                }
                img.Save($"pos_{match.Similarity}.bmp");
            }
        }

        public static UnmanagedImage GrabScreen(int StartX, int StartY, int Width, int Height)
        {
            var should = Program.scheduleScreenshot;
            Program.scheduleScreenshot = false;
            return GrabScreen(StartX, StartY, Width, Height, should);            
        }

        public static UnmanagedImage GrabScreen(int StartX , int StartY , int Width , int Height , bool save)
        {
            return GrabScreen(StartX , StartY , Width , Height , save , PixelFormat.Format8bppIndexed);
        }

        public static UnmanagedImage GrabScreen(int StartX, int StartY, int Width, int Height, bool save, PixelFormat format)
        {
            var bmpScreenshot = new Bitmap(Width , Height , PixelFormat.Format24bppRgb);
            var gfxScreenshot = Graphics.FromImage(bmpScreenshot);

            gfxScreenshot.CopyFromScreen(StartX ,
                StartY ,
                0 ,
                0 ,
                new Size(Width , Height) ,
                CopyPixelOperation.SourceCopy);

            if (save)
            {
                bmpScreenshot.Save($"Screenshot{DateTime.Now.Ticks}.bmp");
            }

            var baseTemplate = bmpScreenshot;
            if ( format != PixelFormat.Format24bppRgb )
            {
                baseTemplate = ConvertBitmap(bmpScreenshot);
            }
            var basetemplateData = baseTemplate.LockBits(new Rectangle(0 , 0 , baseTemplate.Width , baseTemplate.Height) , ImageLockMode.ReadOnly , baseTemplate.PixelFormat);
            UnmanagedImage unmamagedBase = null;

            try
            {
                unmamagedBase = new UnmanagedImage(basetemplateData);
            }
            finally
            {
                // unlock images
                baseTemplate.UnlockBits(basetemplateData);
            }

            return unmamagedBase;
        }

        private static Bitmap ConvertBitmap(Bitmap source, PixelFormat format = PixelFormat.Format8bppIndexed)
        {
            var bmData = source.LockBits(new Rectangle(0 , 0 , source.Width , source.Height) , ImageLockMode.ReadWrite , source.PixelFormat);
            var temp = new Bitmap(bmData.Width , bmData.Height , bmData.Stride , format , bmData.Scan0);
            source.UnlockBits(bmData);

            return temp;
        }

        public static UnmanagedImage ParseImage(string imgLoc)
        {
            UnmanagedImage unmamagedTemplate = null;
            var template = new Bitmap(imgLoc);
            var templateData = template.LockBits(new Rectangle(0 , 0 , template.Width , template.Height) , ImageLockMode.ReadOnly , template.PixelFormat);

            try
            {
                unmamagedTemplate = new UnmanagedImage(templateData);
            }
            finally
            {
                // unlock images
                template.UnlockBits(templateData);
            }

            return unmamagedTemplate;
        }
    }
}
