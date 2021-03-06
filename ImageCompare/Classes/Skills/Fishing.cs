﻿using AForge.Imaging;
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
    public class Fishing : BaseSkill
    {
        private readonly UnmanagedImage FButton = BaseSkill.ParseImage(@"img/press_f.png");
        private UnmanagedImage bobber = BaseSkill.ParseImage(@"img/redBobber.png");
        private ExhaustiveTemplateMatching tm = null;

        public Size bobber_rect = new Size();
        private GameState state = GameState.StartFish;
        public Rectangle? buttonLoc = null;
        public long lastFTick = 0;
        public long lastLureTick = 0;
        public bool useLure = false;

        private enum GameState
        {
            None,
            StartFish,
            Fishing,
            UseLure
        }

        public Fishing()
        {
            this.SkillName = "AFK Fishing";
            Console.Title = this.SkillName;

            this.tm = new ExhaustiveTemplateMatching(0.30f);
            this.bobber_rect = new Size(this.imgWidth / 2 , (int)Math.Floor(this.imgHeight / 1.5));

            this.startX = ( Screen.PrimaryScreen.Bounds.Width / 2 ) - ( this.imgWidth / 2 );
            this.startY = ( Screen.PrimaryScreen.Bounds.Height / 2 ) - ( this.imgHeight / 2 );

            Console.WriteLine("Use Lure?");
            var confirm = Console.ReadKey();
            this.useLure = ( confirm.KeyChar == 'y' );            

            Console.Clear();
        }

        public override void takeAction() {

            if ( this.useLure && this.lastLureTick < DateTime.Now.Ticks )
            {
                this.state = GameState.UseLure;
            }

            // Process the images
            switch ( this.state )
            {
                case GameState.UseLure:
                    Program.config.actions[ButtonAction.LureItem].TakeActions();

                    this.lastLureTick = DateTime.Now.AddHours(3).Ticks;
                    this.state = GameState.StartFish;
                    break;

                case GameState.StartFish:
                    Console.WriteLine("Looking for fish button in location");
                    if ( this.buttonLoc == null && !this.buttonLoc.HasValue )
                    {                        
                        using ( var screenData = GrabScreen(this.startX , this.startY , this.imgWidth , this.imgHeight, true) )
                        {
                            var results = this.tm.ProcessImage(screenData , this.FButton);
                            if ( results != null && results.Length > 0 )
                            {
                                this.buttonLoc = new Rectangle(this.startX + results.First().Rectangle.Left, this.startY + results.First().Rectangle.Top, 0, 0);                                                                
                                Program.config.actions[ButtonAction.ActionBtn].TakeActions();
                                this.state = GameState.Fishing;

                                Console.WriteLine("F button found");
                                Console.WriteLine(results.First().Similarity.ToString());
                            }
                        }
                    }
                    else
                    {
                        using ( var screenData = GrabScreen(this.buttonLoc.Value.Left , this.buttonLoc.Value.Top , this.FButton.Width , this.FButton.Height) )
                        {
                            var results = this.tm.ProcessImage(screenData , this.FButton);
                            if (results != null && results.Length > 0 && results.First().Similarity >= 0.70)
                            {
                                Console.WriteLine($"F button found in previous loc {results.First().Similarity}");

                                Program.config.actions[ButtonAction.ActionBtn].TakeActions();
                                this.state = GameState.Fishing;
                            }
                        }
                    }
                    break;

                case GameState.Fishing:
                    this.bobber = BaseSkill.ParseImage(@"img/redBobber.png"); // Reload bobber image due to memory corruption... 

                    using ( var screenData = GrabScreen(this.buttonLoc.Value.Left , this.buttonLoc.Value.Top , this.FButton.Width , this.FButton.Height) )
                    {
                        var results = this.tm.ProcessImage(screenData , this.FButton);
                        if ( results != null && results.Length > 0 && results.First().Similarity >= 0.70)
                        {
                            Console.WriteLine("");
                            Console.WriteLine("Catching new fish");
                            this.state = GameState.StartFish;
                        }
                    }

                    using ( var screenData = GrabScreen(this.startX - (this.imgWidth / 5), this.startY + this.bobber_rect.Height , this.bobber_rect.Width , this.bobber_rect.Height) )
                    {
                        var results = this.tm.ProcessImage(screenData , this.bobber);
                        if ( results != null && results.Length > 0 && results.First().Similarity >= 0.80)
                        {
                            Console.WriteLine($"Bobber found! {results.First().Similarity}");
                            //SaveFoundArea(screenData , results.First());                            

                            results = null;
                            var bobberFound = false;
                            var buttonFound = false;
                            var bobberTimer = DateTime.Now;
                            while (!Program.paused && (results == null || results.Length == 0 ))
                            {
                                if (bobberTimer.AddSeconds(10) < DateTime.Now)
                                {
                                    break;
                                }

                                using (var screenBobberData = GrabScreen(this.startX - (this.imgWidth / 5), this.startY + this.bobber_rect.Height, this.bobber_rect.Width, this.bobber_rect.Height) )
                                {
                                    var redBobber = this.tm.ProcessImage(screenData, this.bobber);
                                    if (redBobber != null && redBobber.Length > 0 && redBobber.First().Similarity >= 0.79)
                                    {
                                        //SaveFoundArea(screenBobberData , redBobber.First());
                                        Console.WriteLine($"Bobber found! {redBobber.First().Similarity}");
                                        bobberFound = true;
                                    }
                                    else
                                    {
                                        if ( !bobberFound )
                                        {
                                            Console.WriteLine($"Spam F!" );
                                            Program.config.actions[ButtonAction.ActionBtn].TakeActions();
                                            Thread.Sleep(250);
                                        }
                                        bobberFound = false;
                                    }
                                }

                                using ( var screenFData = GrabScreen(this.buttonLoc.Value.Left , this.buttonLoc.Value.Top , this.FButton.Width , this.FButton.Height) )
                                {
                                    results = this.tm.ProcessImage(screenFData , this.FButton);
                                    if (results != null && results.Length > 0) {
                                        if (results.First().Similarity < 0.70)
                                        {
                                            results = null;
                                            buttonFound = false;
                                        }
                                        else
                                        {
                                            if (buttonFound)
                                            {
                                                Console.WriteLine($"Found F {results.First().Similarity}");
                                            } else {
                                                buttonFound = true;
                                                results = null;
                                            }
                                        }
                                    }
                                }
                            }

                            this.state = GameState.StartFish;
                            break;
                        }
                    }
                    break;
            }
        }
    }
}
