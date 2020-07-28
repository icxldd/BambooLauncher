using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LauncherCommon;
namespace BambooLauncher.GlobalClass
{

    public static class Common
    {


        public static NETSpeed netSpeed;

        static Common()
        {
            netSpeed = new NETSpeed();
            netSpeed.InitNetCounters();
        }
    }

}
