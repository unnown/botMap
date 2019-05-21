using ImageCompare.Classes.Button;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageCompare.Classes
{
    public enum ButtonAction
    {
        HealLeaf = 0,
        HealPotion = 1
    }

    public class Config
    {
        public Dictionary<ButtonAction, BaseButton> actions = new Dictionary<ButtonAction, BaseButton>();

        public Config()
        {

        }
    }
}
