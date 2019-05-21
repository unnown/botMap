using AForge.Imaging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageCompare.Classes.Skills
{
    public class Auto : BaseSkill
    {
        public long lastHealTick = 0;
        public long lastShootTick = 0;
        private bool down = false;

        public Auto()
        {
            this.SkillName = "AFK Auto";
            Console.Title = this.SkillName;         
            Console.Clear();
        }

        public override void takeAction() {
            if ( this.lastHealTick < DateTime.Now.Ticks )
            {
                DxInput.SendKey(Interceptor.Keys.One);
                DxInput.SendKey(Interceptor.Keys.Two);
                this.lastHealTick = DateTime.Now.AddMinutes(10).Ticks;
            }

            if (this.lastShootTick < DateTime.Now.Ticks)
            {
                if (this.down)
                {
                    Console.WriteLine("Ending loop");
                    DxInput.SendMouseEvent(Interceptor.MouseState.RightExtraUp);
                    this.down = false;

                    Thread.Sleep(200);
                    DxInput.SendKey(Interceptor.Keys.Control);
                    this.lastShootTick = DateTime.Now.AddSeconds(1).Ticks;
                } else {
                    Console.Clear();
                    Console.WriteLine("Starting new loop");
                    DxInput.SendMouseEvent(Interceptor.MouseState.RightExtraDown);
                    this.down = true;
                    this.lastShootTick = DateTime.Now.AddSeconds(10).Ticks;
                }
                
            }
        }
    }
}
