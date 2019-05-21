using ImageCompare.Classes.Button;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageCompare.Classes
{
    public enum ButtonAction
    {
        HealLeaf = 0,       // 4
        HealPotion = 1,     // 5

        BasicAttack = 2,    // basic (ctrl) attack
        MainAttack = 3,     // no mana/spririt attack (mouse btn)

        Home = 4,           // N
        Confirm = 5,        // Enter
        BuildMode = 6,      // B
        Space = 7,          // Space
        RightClick = 8,     // Mouse rightclick

        LureItem = 9,       // Use lure item 6
        ActionBtn = 10,          // Fish f
    }

    public class Config
    {
        public Dictionary<ButtonAction, BaseButton> actions = new Dictionary<ButtonAction, BaseButton>();

        public List<string> defaultConfig = new List<string>()
        {
            "HealLeaf: Keys.One",
            "HealPotion: Keys.Two",
            "BasicAttack: Keys.Control",
            "MainAttack: Mouse.RightExtraDown; Mouse.RightExtraUp",
            "",
            "Home: Keys.N",
            "Confirm: Keys.Enter",
            "BuildMode: Keys.B",
            "Space: Keys.Space",
            "RightClick: Mouse.RightDown; Mouse.RightUp",
            "",
            "LureItem: Keys.Six",
            "ActionBtn: Keys.F"
        };

        public Config()
        {
            string line;
            if (!System.IO.File.Exists("config.json"))
            {
                System.IO.StreamWriter writer = new System.IO.StreamWriter("config.json");
                foreach (var cfgLine in defaultConfig) {
                    writer.WriteLine(cfgLine);
                }
                writer.Close();
            }

            System.IO.StreamReader file = new System.IO.StreamReader(@"config.json");
            while ((line = file.ReadLine()) != null)
            {
                foreach (ButtonAction val in Enum.GetValues(typeof(ButtonAction)))
                {
                    if (line.StartsWith(val.ToString()))
                    {
                        ButtonAction curAction = val;
                        BaseButton curBtn = null;
                        try
                        {
                            var index = line.IndexOf(":") + 1;
                            var action = (line.Substring(index, line.Length - index)).Trim();

                            if (!action.Contains(";"))
                            {
                                var tmp = action.Split('.');
                                if (tmp.Length == 2)
                                {
                                    switch (tmp[0].Trim().ToLower())
                                    {
                                        case "mouse":
                                            Enum.TryParse(tmp[1].Trim(), out Interceptor.MouseState mouseStatus);
                                            actions.Add(curAction, new MouseButton(mouseStatus));
                                            break;

                                        default:
                                            Enum.TryParse(tmp[1].Trim(), out Interceptor.Keys keyStatus);
                                            actions.Add(curAction, new KeyButton(keyStatus));
                                            break;
                                    }
                                }
                            }
                            else
                            {
                                var tmpActions = action.Split(';');
                                if (tmpActions.Length == 2)
                                {
                                    var tmpDown = tmpActions[0].Split('.');
                                    var tmpUp = tmpActions[1].Split('.');
                                    if (tmpDown.Length == 2 && tmpUp.Length == 2)
                                    {
                                        switch (tmpDown[0].Trim().ToLower())
                                        {
                                            case "mouse":
                                                Enum.TryParse(tmpDown[1].Trim(), out Interceptor.MouseState mouseDownStatus);
                                                Enum.TryParse(tmpUp[1].Trim(), out Interceptor.MouseState mouseUpStatus);
                                                actions.Add(curAction, new MouseButton(mouseDownStatus, mouseUpStatus));
                                                break;

                                            default:
                                                Enum.TryParse(tmpDown[1].Trim(), out Interceptor.Keys keyDownStatus);
                                                Enum.TryParse(tmpUp[1].Trim(), out Interceptor.Keys keyUpStatus);
                                                actions.Add(curAction, new KeyButton(keyDownStatus, keyUpStatus));
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex) {
                            Console.WriteLine($"Unable to parse config");
                            Console.WriteLine($"Error on line: ({line})");
                            Console.ReadKey();
                            Environment.Exit(-10);
                        }
                        break;
                    }
                }
            }
        }
    }
}
