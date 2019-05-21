using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageCompare.Classes.Button
{
    public class KeyButton : BaseButton
    {
        private Interceptor.Keys? key = null;

        public override void duringAction() {
            DxInput.SendKey(key.Value);
        }
    }
}
