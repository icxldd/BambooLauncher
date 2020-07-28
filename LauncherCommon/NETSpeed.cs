using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LauncherCommon
{


    public class NETSpeed
    {

        

        //网络发送速度
        public float NetTrafficSend { get; set; }
        //网络接收速度
        public float NetTrafficReceive { get; set; }



        private PerformanceCounterCategory performanceNetCounterCategory;

        private PerformanceCounter[] trafficSentCounters;

        private PerformanceCounter[] trafficReceivedCounters;
        //用于记录性能对象实例名称
        private string[] interfaces = null;

        public void InitNetCounters()
        {

            performanceNetCounterCategory = new PerformanceCounterCategory("Network Interface");
            //获取性能对象实例名称
            interfaces = performanceNetCounterCategory.GetInstanceNames();

            int length = interfaces.Length;

            if (length > 0)

            {

                trafficSentCounters = new PerformanceCounter[length];

                trafficReceivedCounters = new PerformanceCounter[length];

            }

            for (int i = 0; i < length; i++)

            {

                trafficReceivedCounters[i] = new PerformanceCounter("Network Interface", "Bytes Received/sec", interfaces[i]);

                trafficSentCounters[i] = new PerformanceCounter("Network Interface", "Bytes Sent/sec", interfaces[i]);

            }

        }

        /// <summary>
        /// 网络上传速度
        /// </summary>
        public void GetCurretTrafficSent()

        {

            int length = interfaces.Length;

            float sendSum = 0.0F;
            for (int i = 0; i < length; i++)
            {
                //获取上传数据量
                float temp = trafficSentCounters[i].NextValue();
                //第一次获取值为0的处理方法
                if (i == 0 && temp == 0)
                {
                    Thread.Sleep(500);
                    temp = trafficSentCounters[i].NextValue();
                }
                sendSum += temp;

            }
            float tmp = (sendSum / 1024);

            NetTrafficSend = (float)(Math.Round((double)tmp, 1));

        }
        /// <summary>
        /// 网络下载速度
        /// </summary>
        public void GetCurrentTrafficReceived()
        {
            int length = interfaces.Length;

            float receiveSum = 0.0F;

            for (int i = 0; i < length; i++)
            {
                //获取下载数据量
                float temp = trafficReceivedCounters[i].NextValue();
                if (i == 0 && temp == 0)
                {
                    Thread.Sleep(500);
                    temp = trafficReceivedCounters[i].NextValue();
                }
                receiveSum += temp;
            }

            float tmp = (receiveSum / 1024);

            NetTrafficReceive = (float)(Math.Round((double)tmp, 1));

        }

        public double GetDownSpeed()
        {
            this.GetCurrentTrafficReceived();
            float netTrafficReceive = this.NetTrafficReceive;
            return NetTrafficReceive;
        }

    }
}
