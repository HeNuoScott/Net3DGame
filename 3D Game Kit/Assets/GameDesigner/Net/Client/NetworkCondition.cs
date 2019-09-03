namespace Net.Client
{
    using UnityEngine;
    using System.Collections;
    using System.Threading.Tasks;
    using System.Threading;
    using Net.Share;

    /// <summary>
    /// 检测网络状况
    /// </summary>
    public class NetworkCondition : NetBehaviour
    {
        private int frame;
        /// <summary>
        /// 帧的延时
        /// </summary>
        [Header("帧延迟")]
        public int delayFrame;
        /// <summary>
        /// 延时毫秒时间
        /// </summary>
        [Header("延时ms")]
        public int delayTime;
        //public Text text;
        public string text;
	    public System.DateTime time;

        void Start()
        {
            Task.Run(()=>
            {
                while (this != null)
                {
	                Thread.Sleep(1000);
	                time = System.DateTime.Now;
	                Send(NetCmd.LocalCmd, "NetworkDelay", frame);
                }
            });
        }

        // Update is called once per frame
        void Update()
        {
            frame++;
        }

        [RPCFun]
        private void NetworkDelay(int frame)
        {
            delayFrame = this.frame - frame;
	        delayTime = System.DateTime.Now.Subtract(time).Milliseconds;
            //text.text = "FPS:" + this.frame + "  " + delayTime + "ms";
            text = "FPS:" + this.frame + "  " + delayTime + "ms";
            this.frame = 0;
        }
    }
}