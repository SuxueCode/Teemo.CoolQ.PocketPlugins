using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newbe.CQP.Framework;
using System.Collections;

namespace Teemo.CoolQ.PocketPlugins
{
    public class PocketPlugins : PluginBase
    {
        public override string AppId => "Teemo.CoolQ.PocketPlugins";
        public static UserInfo User;
        public static PocketProxy Proxy;
        public static CommonConfig CommonCfg = new CommonConfig();
        //记录运行时配置
        public static Hashtable RunTimeCfg = new Hashtable();
        //记录运行任务
        public static Hashtable RunProject = new Hashtable();
        public static ICoolQApi Api;

        public PocketPlugins(ICoolQApi coolQApi) : base(coolQApi)
        {
            Api = CoolQApi;
        }

        public override int Enabled()
        {
            return base.Enabled();
        }

        public override int ProcessGroupMessage(int subType, int sendTime, long fromGroup, long fromQq, string fromAnonymous, string msg, int font)
        {
            if(msg.Contains("debug"))
            {
                string[] cmd = msg.Split(' ');

                if (cmd[0] != "debug")
                    return 1;

                if (cmd[1]=="time")
                {
                    string send = "debug info:";
                    foreach (string key in RunTimeCfg.Keys)
                    {
                        ListenRunTimeConfig cfg = (ListenRunTimeConfig)RunTimeCfg[key];
                        if (cfg.QQGroups.Contains(fromGroup))
                        {
                            send += string.Format("\r\n{0}:{1}[{2}]", cfg.IdolName, cfg.UpdateTime.ToString(), key);
                        }
                        CoolQApi.SendGroupMsg(fromGroup, send);
                    }
                }

                if (cmd[1] == "restart")
                {
                    string key = cmd[2];
                    if (RunProject.ContainsKey(key))
                        RunProject.Remove(key);

                    Common.StartListenRoomTask(key);
                    CoolQApi.SendGroupMsg(fromGroup, key + "任务重建完毕，请查看酷Q日志配合time命令查看是否恢复任务");
                }
                
            }
            return base.ProcessGroupMessage(subType, sendTime, fromGroup, fromQq, fromAnonymous, msg, font);
        }

        public override int ProcessMenuClickA()
        {
            panel tmp = new panel();
            tmp.Show();
            return base.ProcessMenuClickA();
        }



    }
}
