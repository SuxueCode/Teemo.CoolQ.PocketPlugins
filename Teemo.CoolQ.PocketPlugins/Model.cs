using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Teemo.CoolQ.PocketPlugins
{
    public class PocketSetting
    {
        public static string IMEI { get; set; }
        public static int MaxListen { get; set; } = 5;
        public static long Interval { get; set; } = 60;
        public static int ShortDelay { get; set; }
        public static int LongDelay { get; set; }
        public bool AutoStart { get; set; }
    }
    public class UserInfo
    {
        public string UserName { get; set; }
        public string PassWord { get; set; }
        public string PocketToken { get; set; }
        public DateTime TokenUpdate { get; set; }
    }

    public class ListenRunTimeConfig
    {
        public string IdolName { get; set; }
        public int RoomId { get; set; }
        public int Delay { get; set; }
        public int ShortDelay { get; set; }
        public int LongDelay { get; set; }
        public bool TransmitText { get; set; }
        public bool TransmitImage { get; set; }
        public bool TransmitAudio { get; set; }
        public bool TransmitVideo { get; set; }
        public bool TransmitFanpai { get; set; }
        public bool TransmitFlip { get; set; }
        public bool TransmitLive { get; set; }
        public bool TransmitGift { get; set; } 
        public DateTime UpdateTime { get; set; }
        //public bool Pro { get; set; }
        public bool First { get; set; }
        public long LastTime { get; set; }
        public PocketProxy Proxy { get; set; }
        public List<long> QQGroups { get; set; }
        public RuntimeCount CountInfo { get; set; }
    }

    public class RuntimeCount
    {
        public int Error { get; set; }
        public int OK { get; set; }
    }

    public class CommonConfig
    {
        public string ProxyApi { get; set; }
        //public int LiveDelay { get; set; }
        public bool CoolQAir { get; set; }
        public bool LiveAtAll { get; set; }
        public bool AutoStart { get; set; }
    }

    public class PocketProxy
    {
        public bool UseProxy { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }
        public string FullIP { get; set; }
        public bool Test(UserInfo user,string roomid)
        {
            if (FullIP != "")
                throw new IPNotValue();

            string json = Common.GetRoomMessage(this, PocketPlugins.User.PocketToken, roomid);
            if (json == "")
                return true;
            if (json.Contains("200"))
                return true;
            else
                return false;
        }
        
    }

    public class IPNotValue : Exception { }

}
