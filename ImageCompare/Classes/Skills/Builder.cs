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
    public class Builder : BaseSkill
    {
       
        private ExhaustiveTemplateMatching tm = null;
        private GameState state = GameState.GoHome;
        private Random rand = new Random();
        private int count = 0;
        private int buildCount = 120;

        public enum GameState
        {
            None,
            GoHome,
            Confirm,
            BuildMode,
            Loop1,
            Loop2,
            Exit,
            ConfirmStop
        }

        public Builder()
        {
            this.SkillName = "AFK Builder";
            Console.Title = this.SkillName;

            this.tm = new ExhaustiveTemplateMatching(0.60f);

            this.imgHeight = (int)Math.Floor(Screen.PrimaryScreen.Bounds.Height * 0.2);
            this.imgWidth = (int)Math.Floor(Screen.PrimaryScreen.Bounds.Width * 0.2);
            this.startY = (Screen.PrimaryScreen.Bounds.Height / 2) - (this.imgHeight / 2);
            this.startX = (Screen.PrimaryScreen.Bounds.Width / 2) - (this.imgWidth / 2);
        }

        public override void takeAction()
        {
           switch (this.state)
            {
                case GameState.GoHome:
                    Console.WriteLine("Sending home command");                    
                    Program.config.actions[ButtonAction.Home].TakeActions();
                    this.state = GameState.Confirm;
                    Thread.Sleep(200);
                    break;

                case GameState.Confirm:
                    Program.config.actions[ButtonAction.Confirm].TakeActions();
                    this.state = GameState.BuildMode;
                    Thread.Sleep(200);
                    break;

                case GameState.BuildMode:
                    Thread.Sleep(10000);
                    Program.config.actions[ButtonAction.BuildMode].TakeActions();
                    this.state = GameState.Loop1;
                    break;

                case GameState.Loop1:
                    Thread.Sleep(200);
                    Program.config.actions[ButtonAction.Space].TakeActions();
                    this.state = GameState.Loop2;
                    break;

                case GameState.Loop2:
                    Thread.Sleep(200);
                    Program.config.actions[ButtonAction.RightClick].DownAction();                    
                    Thread.Sleep(rand.Next(10, 20));
                    Program.config.actions[ButtonAction.RightClick].UpAction();

                    count += 1;
                    if (count >= buildCount)
                    {
                        this.state = GameState.Exit;
                    }
                    else
                    {
                        this.state = GameState.Loop1;
                    }                    
                    break;

                case GameState.Exit:
                    Console.WriteLine("Ending home command");
                    Program.config.actions[ButtonAction.Home].TakeActions();
                    this.state = GameState.ConfirmStop;
                    Thread.Sleep(200);
                    break;

                case GameState.ConfirmStop:
                    Program.config.actions[ButtonAction.Confirm].TakeActions();
                    Program.paused = true;
                    Program.askMode();
                    break;
            }
        }
    }
}
