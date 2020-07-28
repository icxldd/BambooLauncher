using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LauncherCommon
{
    public class GlobalClass
    {
        public static NETSpeed netSpeed;

        static GlobalClass()
        {
            netSpeed = new NETSpeed();
            netSpeed.InitNetCounters();
        }
    }
}
