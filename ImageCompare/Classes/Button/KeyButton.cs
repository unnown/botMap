using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageCompare.Classes.Button
{
    public class KeyButton : BaseButton
    {
        private Interceptor.Keys? downkey = null;
        private Interceptor.Keys? upkey = null;

        public override void DownAction() {
            if (this.downkey.HasValue)
            {
                DxInput.SendKey(downkey.Value);
            }
        }

        public override void UpAction()
        {
            if (upkey.HasValue)
            {
                DxInput.SendKey(upkey.Value);
            }
        }

        public KeyButton() { }

        public KeyButton(Interceptor.Keys down)
        {
            this.downkey = down;
        }

        public KeyButton(Interceptor.Keys down, Interceptor.Keys up)
        {
            this.downkey = down;
            this.upkey = up;
        }

    }
}
