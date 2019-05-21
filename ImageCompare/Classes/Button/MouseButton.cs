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

        public virtual void downAction() {
            if (btnDown.HasValue)
            {
                DxInput.SendMouseEvent(btnDown.Value);
            }
        }

        public override void duringAction()
        {
            Thread.Sleep(200);
        }

        public virtual void upAction() {
            if (btnUp.HasValue)
            {
                DxInput.SendMouseEvent(btnUp.Value);
            }
        }
    }
}
