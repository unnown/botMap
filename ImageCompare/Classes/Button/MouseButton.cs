using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ImageCompare.Classes.Button
{
    public class MouseButton : BaseButton
    {
        private Interceptor.MouseState? btnDown = null;

        private Interceptor.MouseState? btnUp = null;

        public virtual void DownAction() {
            if (btnDown.HasValue)
            {
                DxInput.SendMouseEvent(btnDown.Value);
            }
        }

        public virtual void UpAction() {
            if (btnUp.HasValue)
            {
                DxInput.SendMouseEvent(btnUp.Value);
            }
        }

        public MouseButton() { }

        public MouseButton(Interceptor.MouseState down)
        {
            this.btnDown = down;
        }
        
        public MouseButton(Interceptor.MouseState down, Interceptor.MouseState up)
        {
            this.btnDown = down;
            this.btnUp = up;
        }
    }
}
