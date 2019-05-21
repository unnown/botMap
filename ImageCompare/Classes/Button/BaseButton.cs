using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageCompare.Classes.Button
{
    public class BaseButton
    {
        public virtual void DownAction() { }

        public virtual void UpAction() { }

        public void TakeActions()
        {
            this.DownAction();

            this.UpAction();
        }
    }
}
