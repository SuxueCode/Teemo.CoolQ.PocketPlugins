using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newbe.CQP.Framework;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

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
            if (!File.Exists("PocketConfig.json"))
            {
                PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "配置文件不存在！如设置自启动不会生效，请打开面板设置之后保存启动。");
            }
            else
            {
                string json = File.ReadAllText("PocketConfig.json");
                JObject jsonObj = JObject.Parse(json);

                

                if (jsonObj.Property("IMEI") != null)
                {
                    PocketSetting.IMEI = jsonObj["IMEI"].ToString();
                }

                string username = jsonObj["User"]["UserName"].ToString();
                string password = jsonObj["User"]["PassWord"].ToString();
                string IMEI = jsonObj["IMEI"].ToString();

                string jsontoken = Common.GetUserInfo(username, password, IMEI);
                //PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "获取TOKEN。");
                JObject obj = JObject.Parse(jsontoken);
                if ((int)obj["status"] == 200)
                {
                    jsonObj["User"]["PocketToken"] = obj["content"]["token"].ToString();                   
                    PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "获取TOKEN成功");
                    File.WriteAllText("PocketConfig.json", jsonObj.ToString());
                    if (jsonObj.Property("AutoStart") != null)
                    {
                        if (Convert.ToBoolean(jsonObj["AutoStart"]) == true)
                        {
                            json = File.ReadAllText("PocketConfig.json");
                            jsonObj = JObject.Parse(json);

                            if (jsonObj.Property("User") != null)
                            {
                                PocketPlugins.User = JsonConvert.DeserializeObject<UserInfo>(jsonObj["User"].ToString());
                            }

                            if (jsonObj.Property("IMEI") != null)
                            {
                                PocketSetting.IMEI = jsonObj["IMEI"].ToString();
                            }

                            /*
                            if (jsonObj.Property("LiveDelay") != null)
                            {
                                txt_livedelay.Text = jsonObj["LiveDelay"].ToString();
                                PocketPlugins.CommonCfg.LiveDelay = int.Parse(jsonObj["LiveDelay"].ToString());
                            }*/


                            if (jsonObj.Property("CoolQAir") != null)
                            {
                                bool Air = bool.Parse(jsonObj["CoolQAir"].ToString());
                                if (Air)
                                {
                                    PocketPlugins.CommonCfg.CoolQAir = true;
                                }
                                else
                                {
                                    PocketPlugins.CommonCfg.CoolQAir = false;
                                }
                            }

                            if (jsonObj.Property("IdolInfo") != null)
                            {
                                foreach (JObject idol in jsonObj["IdolInfo"])
                                {
                                    ListenRunTimeConfig config = JsonConvert.DeserializeObject<ListenRunTimeConfig>(idol.ToString());
                                    if (PocketPlugins.RunTimeCfg.ContainsKey(config.IdolName))
                                    {
                                        continue;
                                    }
                                    config.First = true;
                                    PocketPlugins.RunTimeCfg.Add(config.IdolName, config);
                                }
                            }

                            int timedelay = 0;
                            foreach (var idol in PocketPlugins.RunTimeCfg.Keys)
                            {
                                Common.StartListenRoomTask(idol.ToString(), timedelay);
                                timedelay = timedelay + 5000;
                            }
                        }
                        else
                            PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "配置为非自行启动，仅更新TOKEN");
                    }


                }
                else
                    PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "获取TOKEN失败，请打开面板检查账号密码配置");
                
            }

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
                        //CoolQApi.SendGroupMsg(fromGroup, send);
                    }
                }

               // if (cmd[1] == "重启")
               // {
                    //string key = cmd[2];
                    //if (RunProject.ContainsKey(key))
                    //    RunProject.Remove(key);

                    //Common.StartListenRoomTask(key);
                    //PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "任务重建完毕，请查看酷Q日志配合time命令查看是否恢复任务");
                    //CoolQApi.SendGroupMsg(fromGroup, key + "任务重建完毕，请查看酷Q日志配合time命令查看是否恢复任务");
               // }
                
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
