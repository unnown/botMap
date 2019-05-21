using AForge.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImageCompare.Classes.Skills
{
    public class Mining : BaseSkill
    {
        private readonly UnmanagedImage High = BaseSkill.ParseImage(@"img/high.png");        
        private ExhaustiveTemplateMatching tm = null;

        public Mining()
        {
            this.SkillName = "AFK Mining";
            Console.Title = this.SkillName;

            this.tm = new ExhaustiveTemplateMatching(0.90f);

            this.imgHeight = (int)Math.Floor(Screen.PrimaryScreen.Bounds.Height * 0.3);
            this.startY = ( Screen.PrimaryScreen.Bounds.Height / 2 ) - this.imgHeight;
        }

        public override void takeAction() {
            Console.WriteLine("Looking for highChance");
            using ( var screenData = GrabScreen(this.startX , this.startY , this.imgWidth , this.imgHeight) )
            {
                var results = this.tm.ProcessImage(screenData , this.High);
                if ( results != null && results.Length > 0 )
                {
                    Console.WriteLine($"Found high {results.First().Similarity}");
                    Program.config.actions[ButtonAction.ActionBtn].TakeActions();
                }
            }
        }
    }
}
