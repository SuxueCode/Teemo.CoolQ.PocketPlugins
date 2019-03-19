using Newbe.CQP.Framework;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Teemo.CoolQ.PocketPlugins
{
    public class Common
    {
        //获取口袋房间消息
        public static string PocketVersion = "5.2.0";
        public static string PocketAgent = "Mobile_Pocket";
        public static string GetRoomMessage(PocketProxy proxy,string token,string roomid)
        {
            HttpClientHandler handler = new HttpClientHandler();
            handler.Proxy = new WebProxy(proxy.FullIP);
            handler.UseProxy = true;
            string repo = GetRoomMessage(handler, token, int.Parse(roomid));
            return repo;
        }

        public static string GetRoomMessage(ListenRunTimeConfig config,UserInfo user)
        {
            HttpClientHandler handler = new HttpClientHandler();
            if (config.Proxy != null)
            {
                handler.UseProxy = config.Proxy.UseProxy;
                if (config.Proxy.UseProxy)
                    handler.Proxy = new WebProxy(config.Proxy.FullIP);
            }
            string repo = GetRoomMessage(handler, user.PocketToken,config.RoomId);
            return repo;
        }
        private static string GetRoomMessage(HttpClientHandler handler,string token,int roomId)
        {
            try
            {
                HttpClient client = new HttpClient(handler);
                client.Timeout = new TimeSpan(0, 0, 8);
                HttpRequestMessage req = new HttpRequestMessage();
                req.RequestUri = new Uri("https://pjuju.48.cn/imsystem/api/im/v1/member/room/message/mainpage");
                req.Method = HttpMethod.Post;
                req.Headers.Add("IMEI", PocketSetting.IMEI);
                req.Headers.Add("version", PocketVersion);
                req.Headers.Add("User-Agent", PocketAgent);
                req.Headers.Add("os", "Android");
                req.Headers.Add("token", token);

                JObject rss = new JObject(
                    new JProperty("roomId", roomId),
                    new JProperty("chatType", 0),
                    new JProperty("lastTime", 0),
                    new JProperty("limit", 10)
                );

                req.Content = new StringContent(rss.ToString(), Encoding.UTF8, "application/json");
                string res = client.SendAsync(req).Result.Content.ReadAsStringAsync().Result;
                return res;
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                return "http error";
            }
        }

        //获取直播信息，废弃不用了
        public static string GetLive(PocketProxy proxy,UserInfo user,long lasttime)
        {
            HttpClientHandler handler = new HttpClientHandler();
            if (proxy != null)
            {
                handler.UseProxy = proxy.UseProxy;
                if (proxy.UseProxy)
                    handler.Proxy = new WebProxy(proxy.FullIP);
            }
            string repo = GetLive(handler, user.PocketToken,lasttime);
            return repo;
        }
        private static string GetLive(HttpClientHandler handler, string token,long lastime)
        {
            try
            {
                HttpClient client = new HttpClient(handler);
                HttpRequestMessage req = new HttpRequestMessage();
                req.RequestUri = new Uri("https://plive.48.cn/livesystem/api/live/v1/memberLivePage");
                req.Method = HttpMethod.Post;
                req.Headers.Add("IMEI", PocketSetting.IMEI);
                req.Headers.Add("version", PocketVersion);
                req.Headers.Add("User-Agent", PocketAgent);
                req.Headers.Add("os", "Android");
                req.Headers.Add("token", token);

                JObject rss = new JObject(
                    new JProperty("lastTime", lastime),
                    new JProperty("limit", 50),
                    new JProperty("groupId", 0),
                    new JProperty("memberId", 0),
                    new JProperty("type", 1),
                    new JProperty("giftUpdTime", 1498211389003)
                );

                req.Content = new StringContent(rss.ToString(), Encoding.UTF8, "application/json");
                string res = client.SendAsync(req).Result.Content.ReadAsStringAsync().Result;
                return res;
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                return "http error";
            }
        }

        public static void StartListenRoomTask(string IdolName,int timedelay)
        {
            Task task = new Task(() => {
                if (PocketPlugins.RunProject.ContainsKey(IdolName))
                {
                    int threadId = (int)PocketPlugins.RunProject[IdolName];
                    if (Thread.CurrentThread.ManagedThreadId != threadId)
                    {
                        PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, string.Format("[{0}]任务已经存在，退出", IdolName));
                        return;
                    }
                }
                else
                {
                    PocketPlugins.RunProject.Add(IdolName, Thread.CurrentThread.ManagedThreadId);
                }

                try
                {
                    while (true)
                    {
                        if (!PocketPlugins.RunTimeCfg.ContainsKey(IdolName))
                        {
                            PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, IdolName + " 配置不存在，监听线程退出");
                            if (PocketPlugins.RunProject.ContainsKey(IdolName))
                                PocketPlugins.RunProject.Remove(IdolName);
                            return;
                        }
                        ListenRunTimeConfig config = (ListenRunTimeConfig)PocketPlugins.RunTimeCfg[IdolName];
                        string json = GetRoomMessage(config, PocketPlugins.User);
                        if (json == "")
                        {
                            PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "Token可能过期了,请及时更换,面板左下角登录一下就好,间隔自动拉大到一分钟");
                            Thread.Sleep(60000);
                            continue;
                        }
                        if(json == "http error")
                        {
                            PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "访问异常，如配置有代理请尝试更换，如果确定代理没问题，则可能是口袋抽风，过段时间还是不行，恭喜你被拉黑了！");
                            Thread.Sleep(60000);
                            continue;
                        }
                        Thread.Sleep(timedelay);                        
                        //PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, string.Format("[{0}]监听开始。延迟{1}ms", IdolName, timedelay));
                        ProcessRoomMessage(json,config);
                        config.UpdateTime = DateTime.Now;
                        PocketPlugins.RunTimeCfg[IdolName] = config;
                        Thread.Sleep(config.Delay);
                    }
                }
                catch(Exception ex)
                {
                    WriteLog(ex);
                }

            });
            task.Start();
        }
        public static void StartLiveTask()
        {
            Task task = new Task(()=> {
                while (true)
                {
                    if (PocketPlugins.RunProject.ContainsKey("LiveListen"))
                    {
                        int threadId = (int)PocketPlugins.RunProject["LiveListen"];
                        if (Thread.CurrentThread.ManagedThreadId != threadId)
                        {
                            PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, string.Format("[{0}]任务已经存在，退出", "LiveListen"));
                            return;
                        }
                    }
                    else
                    {
                        PocketPlugins.RunProject.Add("LiveListen", Thread.CurrentThread.ManagedThreadId);
                        PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, string.Format("[{0}]任务启动", "LiveListen"));
                    }

                    try
                    {
                        long lasttime = 0;
                        while (true)
                        {
                            string json = GetLive(PocketPlugins.Proxy, PocketPlugins.User,lasttime);
                            if (json == "")
                            {
                                PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "Token可能过期了,请及时更换,面板左下角登录一下就好,间隔自动拉大到一分钟");
                                Thread.Sleep(1 * 60 * 1000);
                                continue;
                            }
                            if (json == "http error")
                            {
                                PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "访问异常，如配置有代理请尝试更换，如果确定代理没问题，则可能是口袋抽风，过段时间还是不行，恭喜你被拉黑了！");
                                Thread.Sleep(1 * 60 * 1000);
                                continue;
                            }
                            if(ProcessLive(json,ref lasttime))
                                break;
                        }
                        
                        //Thread.Sleep(PocketPlugins.CommonCfg.LiveDelay);
                    }
                    catch (Exception ex)
                    {
                        WriteLog(ex);
                    }
                    
                }
            });
            task.Start();
        }

        private static Hashtable LiveCache = new Hashtable();
        private static void ProcessRoomMessage(string json, ListenRunTimeConfig config)
        {
            try
            {
                JObject obj = JObject.Parse(json);
                if ((int)obj["status"] == 200)
                {
                    IEnumerable<JToken> datas = obj.SelectTokens("$.content.data[*]");
                    long tmpTime = 0;
                    DateTime msgTime = new DateTime(1996, 9, 10);
                    string totalTempMsg = "";
                    string templeMsgTime = "";
                    int sort = 1;
                    foreach (JToken msgs in datas)
                    {                        
                        msgTime = DateTime.Parse(msgs["msgTimeStr"].ToString());
                        //★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★★
                        JObject msg = JObject.Parse(msgs["extInfo"].ToString());
                        string[] realtime = msgs["msgTimeStr"].ToString().Split(' ');
                        string[] idolnamewords = msg["senderName"].ToString().Split('-');
                        string idolname = idolnamewords[idolnamewords.Length - 1];
                        string roomrecentmsg = "";
                        DateTime dt = new DateTime(1970, 1, 1, 8, 0, 0).AddMilliseconds(((Convert.ToDouble(config.LastTime))));
                        if (sort == 1)//处理第一条消息的逻辑
                        {                            
                            tmpTime = (long)msgs["msgTime"];        //最后一次的第一条时间
                            templeMsgTime = realtime[1];
                            //首次消息日志开始
                            if (PocketPlugins.CommonCfg.msglog == true)
                            {
                                //PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "true");
                                try
                                {
                                    switch (msg["messageObject"].ToString())
                                    {
                                        case "deleteMessage":
                                            roomrecentmsg = "你的小偶像删除了一条消息";
                                            break;
                                        case "text":
                                            if (config.TransmitText)
                                                roomrecentmsg = String.Format("{0}:{1}", idolname, msg["text"].ToString());
                                            break;
                                        case "image":
                                            JObject img = JObject.Parse(msgs["bodys"].ToString());
                                            if (config.TransmitImage)
                                            {
                                                roomrecentmsg = String.Format("{0}:\r\n发送图片:{1}", idolname, img["url"].ToString());
                                            }
                                            break;
                                        case "faipaiText":
                                            if (config.TransmitFanpai)
                                            {
                                                if (msg.Property("fanpaiName") != null)
                                                    roomrecentmsg = String.Format("{2} : {3}\r\n{0} 回复 : {1}", idolname, msg["messageText"].ToString(), msg["fanpaiName"].ToString(), msg["faipaiContent"].ToString()) + "\r\n" + totalTempMsg;
                                                else
                                                {
                                                    JObject Name = JObject.Parse(GetUserName(msg["faipaiUserId"].ToString()));
                                                    roomrecentmsg = String.Format("{2} : {3}\r\n{0} 回复 : {1}", idolname, msg["messageText"].ToString(), Name["nickName"].ToString(), msg["faipaiContent"].ToString()) + "\r\n" + totalTempMsg;
                                                }
                                            }
                                            break;
                                        case "audio":
                                            JObject audio = JObject.Parse(msgs["bodys"].ToString());
                                            string audioFilename = GetAudio(audio["url"].ToString(), audio["ext"].ToString());
                                            if (audioFilename == "")
                                                continue;
                                            if (config.TransmitAudio)
                                                roomrecentmsg = String.Format("{0}:\r\n发送语音：{1}", idolname, audio["url"].ToString());
                                            break;
                                        case "videoRecord":
                                            JObject video = JObject.Parse(msgs["bodys"].ToString());
                                            if (config.TransmitVideo)
                                                roomrecentmsg = string.Format("{0}发送视频。\r\n地址:{1}\r\n", idolname, video["url"].ToString());
                                            break;
                                        case "jujuLive":
                                            if (config.TransmitGift)
                                                roomrecentmsg = string.Format("{0}{1}", idolname, msg["text"].ToString());
                                            break;
                                        case "live":
                                            roomrecentmsg = string.Format("直播提醒:你的小心肝{0}的直播:{1}！\r\n请打开口袋48观看哟！设置关注词(直播提醒)不错过直播哦!", idolname, msg["referenceContent"].ToString());
                                            break;
                                        case "diantai":
                                            if (config.TransmitLive)
                                                roomrecentmsg = string.Format("直播提醒:你的小心肝{0}突然开了个电台:{1}！\r\n快打开口袋48观看哟！设置关注词(直播提醒)不错过直播哦!", idolname, msg["referenceContent"].ToString());
                                            break;
                                        case "idolFlip":
                                            if (config.TransmitFlip)
                                            {

                                                //}
                                                if (int.Parse(msg["idolFlipType"].ToString()) == 3)
                                                    roomrecentmsg = string.Format("{0} {1}:\r\n{2}", idolname, msg["idolFlipTitle"].ToString(), msg["idolFlipContent"].ToString());
                                                else
                                                    roomrecentmsg = string.Format("{0} 回答 {3} 的提问:\r\n{2}", idolname, msg["idolFlipTitle"].ToString(), msg["idolFlipContent"].ToString(), msg["idolFlipUserName"].ToString());
                                            }
                                            break;

                                        default:
                                            File.AppendAllText("unknowmsg.log", msgs.ToString() + "\r\n");
                                            roomrecentmsg = "未知类型消息，请检查unknowmsg.log";
                                            break;
                                    }
                                    PocketPlugins.Api.AddLog(10, CoolQLogLevel.Debug, config.LastTime.ToString()+" "+ msgs["msgTime"].ToString());                                    
                                    roomrecentmsg = "上次最晚时间："+ dt.ToString() +" 本次最新时间："+ msgs["msgTimeStr"].ToString() + "\r\n" + config.IdolName + "房间:(调试信息，勾选取消可去除本消息。仅供调试测试查看\r\n" + roomrecentmsg;
                                    PocketPlugins.Api.AddLog(10, CoolQLogLevel.Debug, roomrecentmsg);
                                    //panel.textBox_recentmsg.Text = roomrecentmsg;
                                }
                                catch (Exception ex)
                                {
                                    PocketPlugins.Api.AddLog(10, CoolQLogLevel.Debug, ex.ToString());
                                }
                            }
//                            else
//                                PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "false");

                            //else
                            //PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "false");

                            //首次消息日志结束
                        }
                        sort++;
                        //长短时切换~
                        DateTime now = DateTime.Now;
                        TimeSpan interval = now - msgTime;
                        config.Delay = interval.TotalSeconds > PocketSetting.Interval ? config.LongDelay : config.ShortDelay;
                        //首次运行，直接退出循环
                        if (config.First)
                        {
                            //PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, string.Format("[{0}]任务启动", IdolName));
                            config.LastTime = tmpTime;
                            dt = new DateTime(1970, 1, 1, 8, 0, 0).AddMilliseconds(((Convert.ToDouble(config.LastTime))));
                            PocketPlugins.Api.AddLog(10, CoolQLogLevel.Debug, config.IdolName+" 初始化结束，最新一条消息时间为:"+ config.LastTime.ToString()+" "+ dt.ToString());
                            break;
                        }
                            

                        //时间戳相等说明已经发过了，直接退出
                        if ((long)msgs["msgTime"] <= config.LastTime)
                        {
                            //continue;
                            config.LastTime = tmpTime;
                            break;//一旦小于时间直接终止遍历，赋值最后一次时间。
                        }

                        //消息分发
                        switch (msg["messageObject"].ToString())
                        {
                            case "deleteMessage":
                                totalTempMsg = "你的小偶像删除了一条消息"+"\r\n"+ totalTempMsg;
                                //CQ.SendGroupMessage(qqGroup,"你的小偶像删除了一条口袋房间的消息");
                                break;
                            case "text":
                                if (config.TransmitText)
                                {
                                    //foreach (long qqGroup in config.QQGroups)
                                    //{
                                    //   PocketPlugins.Api.SendGroupMsg(qqGroup, String.Format("{0}:{1}\r\n来源：{2}房间 发送时间：{3}", msg["senderName"].ToString(), msg["text"].ToString(), config.IdolName, msgs["msgTimeStr"].ToString()));
                                    //}
                                    if(config.IdolName != msg["senderName"].ToString())
                                        //totalTempMsg = String.Format("{0}:{1}\r\n时间:{2}", msg["senderName"].ToString(), msg["text"].ToString(), realtime[1]) + "\r\n" + totalTempMsg;
                                        totalTempMsg = String.Format("{0}:{1}", idolname, msg["text"].ToString()) + "\r\n" + totalTempMsg;
                                    else
                                        //totalTempMsg = String.Format("{0}:{1}\r\n时间:{2}", msg["senderName"].ToString(), msg["text"].ToString(), realtime[1]) + "\r\n" + totalTempMsg;
                                        totalTempMsg = String.Format("{0}:{1}", idolname, msg["text"].ToString()) + "\r\n" + totalTempMsg;
                                }
                                break;
                            case "image":
                                JObject img = JObject.Parse(msgs["bodys"].ToString());
                                string imgFilename = GetImage(img["url"].ToString());
                                if (imgFilename == "")
                                    continue;
                                if (config.TransmitImage)
                                {
                                    //foreach (long qqGroup in config.QQGroups)
                                    //{
                                    //    if (PocketPlugins.CommonCfg.CoolQAir)
                                    //        PocketPlugins.Api.SendGroupMsg(qqGroup, String.Format("{0}:\r\n发送了图片：{1}\r\n来源：{2}房间 发送时间：{3}", msg["senderName"].ToString(), img["url"].ToString(), config.IdolName, msgs["msgTimeStr"].ToString()));
                                    //    else
                                    //        PocketPlugins.Api.SendGroupMsg(qqGroup, String.Format("{0}:\r\n{1}\r\n来源：{2}房间 发送时间：{3}", msg["senderName"].ToString(), CoolQCode.Image(imgFilename), config.IdolName, msgs["msgTimeStr"].ToString()));
                                    //}
                                    if (PocketPlugins.CommonCfg.CoolQAir)
                                        //totalTempMsg = String.Format("{0}:\r\n发送图片:{1}\r\n时间:{2}", msg["senderName"].ToString(), img["url"].ToString(), realtime[1]) + "\r\n" + totalTempMsg;
                                        totalTempMsg = String.Format("{0}:\r\n发送图片:{1}", idolname, img["url"].ToString()) + "\r\n" + totalTempMsg;
                                    else
                                        //totalTempMsg = String.Format("{0}:\r\n{1}\r\n时间:{2}", msg["senderName"].ToString(), CoolQCode.Image(imgFilename), realtime[1]) + "\r\n" + totalTempMsg;
                                        totalTempMsg = String.Format("{0}:\r\n{1}", idolname, CoolQCode.Image(imgFilename)) + "\r\n" + totalTempMsg;
                                }
                                break;
                            case "faipaiText":
                                if (config.TransmitFanpai)
                                {
                                    //foreach (long qqGroup in config.QQGroups)
                                    //{
                                    //    if(msg.Property("fanpaiName") != null)
                                    //        PocketPlugins.Api.SendGroupMsg(qqGroup, String.Format("{4}\r\n{0} 回复:{1}\r\n来源：{5}房间 发送时间：{2}", msg["senderName"].ToString(), msg["messageText"].ToString(), msgs["msgTimeStr"].ToString(), "", msg["faipaiContent"].ToString(), config.IdolName));
                                    //    else
                                    //        PocketPlugins.Api.SendGroupMsg(qqGroup, String.Format("{4}\r\n{0} 回复:{1}\r\n来源：{5}房间 发送时间：{2}", msg["senderName"].ToString(), msg["messageText"].ToString(), msgs["msgTimeStr"].ToString(), "", msg["faipaiContent"].ToString(), config.IdolName));
                                    // }
                                    JObject Name = JObject.Parse(GetUserName(msg["faipaiUserId"].ToString()));
//                                    PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, string.Format("[{0}]任务已经存在，退出", IdolName));
                                    if (msg.Property("fanpaiName") != null)
                                        //totalTempMsg = String.Format("{3} : {4}\r\n{0} 回复:{1}\r\n时间:{2}", msg["senderName"].ToString(), msg["messageText"].ToString(), realtime[1], msg["fanpaiName"].ToString(), msg["faipaiContent"].ToString()) + "\r\n" + totalTempMsg;
                                        totalTempMsg = String.Format("{2} : {3}\r\n{0} 回复 : {1}", idolname, msg["messageText"].ToString(), msg["fanpaiName"].ToString(), msg["faipaiContent"].ToString()) + "\r\n" + totalTempMsg;
                                    else                                        
                                        //totalTempMsg = String.Format("{3} : {4}\r\n{0} 回复:{1}\r\n时间:{2}", msg["senderName"].ToString(), msg["messageText"].ToString(), Name["nickName"], msg["faipaiContent"].ToString()) + "\r\n" + totalTempMsg;
                                        totalTempMsg = String.Format("{2} : {3}\r\n{0} 回复 : {1}", idolname, msg["messageText"].ToString(), Name["nickName"].ToString(), msg["faipaiContent"].ToString()) + "\r\n" + totalTempMsg;

                                }
                                break;
                            case "audio":
                                JObject audio = JObject.Parse(msgs["bodys"].ToString());
                                string audioFilename = GetAudio(audio["url"].ToString(), audio["ext"].ToString());
                                if (audioFilename == "")
                                    continue;
                                if (config.TransmitAudio)
                                {
                                    //foreach (long qqGroup in config.QQGroups)
                                    //{
                                    //   if (PocketPlugins.CommonCfg.CoolQAir)
                                    //        PocketPlugins.Api.SendGroupMsg(qqGroup, String.Format("{0}:\r\n发送了语音：{1} 来源：{2}房间 发送时间：{3}", msg["senderName"].ToString(), audio["url"].ToString(), config.IdolName, msgs["msgTimeStr"].ToString()));
                                    //    else
                                    //        PocketPlugins.Api.SendGroupMsg(qqGroup, String.Format("{0}", CoolQCode.ShareRecord(audioFilename)));
                                    //        //PocketPlugins.Api.SendGroupMsg(qqGroup, String.Format("{0}:\r\n{1} 来源：口袋房间", msg["senderName"].ToString(), CoolQCode.ShareRecord(audioFilename)));
                                    //}
                                    if (PocketPlugins.CommonCfg.CoolQAir)
                                        //totalTempMsg = String.Format("{0}:\r\n发送语音：{1}\r\n时间:{2}", msg["senderName"].ToString(), audio["url"].ToString(), realtime[1]) + "\r\n" + totalTempMsg;
                                        totalTempMsg = String.Format("{0}:\r\n发送语音：{1}", idolname, audio["url"].ToString()) + "\r\n" + totalTempMsg;
                                    else
                                        //totalTempMsg = String.Format("{0}:\r\n{1}", msg["senderName"].ToString(), CoolQCode.ShareRecord(audioFilename)) + "\r\n" + totalTempMsg;
                                        //totalTempMsg = String.Format("{0}:\r\n{1}\r\n时间:{2}", msg["senderName"].ToString(), CoolQCode.ShareRecord(audioFilename), realtime[1]) + "\r\n" + totalTempMsg;
                                        totalTempMsg = String.Format("{0}:\r\n{1}", idolname, CoolQCode.ShareRecord(audioFilename)) + "\r\n" + totalTempMsg;
                                }

                                break;
                            case "videoRecord":
                                JObject video = JObject.Parse(msgs["bodys"].ToString());
                                if (config.TransmitVideo)
                                {
                                    //foreach (long qqGroup in config.QQGroups)
                                    //{
                                    //    PocketPlugins.Api.SendGroupMsg(qqGroup, string.Format("{0}发送了一个视频，请点击下面链接查看\r\n地址：{1}", msg["senderName"].ToString(), video["url"].ToString()));
                                    //}
                                    totalTempMsg = string.Format("{0}发送视频。\r\n地址:{1}\r\n", idolname, video["url"].ToString()) + "\r\n" + totalTempMsg;
                                }
                                break;
                            case "jujuLive":
                                if (config.TransmitGift)
                                {
                                    //foreach (long qqGroup in config.QQGroups)
                                    //{
                                    //    PocketPlugins.Api.SendGroupMsg(qqGroup, string.Format("{0}{1}\r\n来源：{2}房间 发送时间：{3}", msg["senderName"].ToString(), msg["text"].ToString(), config.IdolName, msgs["msgTimeStr"].ToString()));
                                    //}
                                    //totalTempMsg = string.Format("{0}{1}\r\n时间:{2}", msg["senderName"].ToString(), msg["text"].ToString(), realtime[1]) + "\r\n" + totalTempMsg;
                                    totalTempMsg = string.Format("{0}{1}", idolname, msg["text"].ToString()) + "\r\n" + totalTempMsg;
                                }
                                break;
                            case "live":
                                if (config.TransmitLive)
                                {
                                    if (config.Atall && !PocketPlugins.CommonCfg.CoolQAir)  
                                        totalTempMsg = string.Format("[CQ:at,qq=all]直播提醒:你的小心肝{0}的直播:{1}！\r\n请打开口袋48观看哟！设置关注词(直播提醒)不错过直播哦!", idolname, msg["referenceContent"].ToString()) + "\r\n" + totalTempMsg;
                                    else  
                                        totalTempMsg = string.Format("直播提醒:你的小心肝{0}的直播:{1}！\r\n请打开口袋48观看哟！设置关注词(直播提醒)不错过直播哦!", idolname, msg["referenceContent"].ToString()) + "\r\n" + totalTempMsg;
                                }
                                break;
                            case "diantai":
                                if (config.TransmitLive)
                                {
                                    if (config.Atall && !PocketPlugins.CommonCfg.CoolQAir)
                                        totalTempMsg = string.Format("[CQ:at,qq=all]直播提醒:你的小心肝{0}的电台:{1}!\r\n快打开口袋48观看哟！设置关注词(直播提醒)不错过直播哦!", idolname, msg["referenceContent"].ToString()) + "\r\n" + totalTempMsg;
                                    else
                                        totalTempMsg = string.Format("直播提醒:你的小心肝{0}的电台:{1}!\r\n快打开口袋48观看哟！设置关注词(直播提醒)不错过直播哦!", idolname, msg["referenceContent"].ToString()) + "\r\n" + totalTempMsg;
                                }
                                break;
                            case "idolFlip":
                                if (config.TransmitFlip)
                                {
                                    //foreach (long qqGroup in config.QQGroups)
                                    //{
                                    //    if (int.Parse(msg["idolFlipType"].ToString()) == 3)
                                    //        PocketPlugins.Api.SendGroupMsg(qqGroup, string.Format("{0}回答了匿名聚聚的提问:\r\n{1}\r\n回答请进入房间查看", msg["senderName"].ToString(), msg["idolFlipContent"].ToString()));
                                    //    else
                                    //        PocketPlugins.Api.SendGroupMsg(qqGroup, string.Format("{0}回答了{2}的提问:\r\n{1}\r\n回答请进入房间查看", msg["senderName"].ToString(), msg["idolFlipContent"].ToString(), msg["idolFlipUserName"].ToString()));
                                    //}
                                
                                if (int.Parse(msg["idolFlipType"].ToString()) == 3)
                                    //totalTempMsg = string.Format("{0} {1}:\r\n{2}\r\n时间:{3}", msg["senderName"].ToString(), msg["idolFlipTitle"].ToString(), msg["idolFlipContent"].ToString(), realtime[1]) + "\r\n" + totalTempMsg;
                                    totalTempMsg = string.Format("{0} {1}:\r\n{2}", idolname, msg["idolFlipTitle"].ToString(), msg["idolFlipContent"].ToString()) + "\r\n" + totalTempMsg;
                                else
                                    //totalTempMsg = string.Format("{0} 回答 {3} 的提问:\r\n{2}\r\n时间:{4}", msg["senderName"].ToString(), msg["idolFlipTitle"].ToString(), msg["idolFlipContent"].ToString(), msg["idolFlipUserName"].ToString(),realtime[1]) + "\r\n" + totalTempMsg;
                                    totalTempMsg = string.Format("{0} 回答 {3} 的提问:\r\n{2}", idolname, msg["idolFlipTitle"].ToString(), msg["idolFlipContent"].ToString(), msg["idolFlipUserName"].ToString()) + "\r\n" + totalTempMsg;
                                }
                                break;
                            default:
                                File.AppendAllText("msg.log", msgs.ToString() + "\r\n");
                                break;
                        }

                    }
                    if (totalTempMsg != "")
                    {
                        totalTempMsg = templeMsgTime +""+ config.IdolName+"房间:\r\n"+ totalTempMsg;
                        totalTempMsg = totalTempMsg.Substring(0, totalTempMsg.Length - 2);
                        foreach (long qqGroup in config.QQGroups)
                            Common.sendgroupmsgChose(qqGroup, totalTempMsg);

                        if (PocketPlugins.Api.GetLoginQQ() == 2893276319)
                        {
                            totalTempMsg = totalTempMsg.Replace("[CQ:at,qq=all]", "");
                            PocketPlugins.Api.SendPrivateMsg(1691686998, totalTempMsg);
                        }
                    }                                   
                }
                if (config.First)

                    config.First = false;
            }
            catch (Exception ex)
            {
                WriteLog(ex);
            }
        }
        private static bool ProcessLive(string json,ref long lasttime)
        {
            try
            {
                JObject obj = JObject.Parse(json);
                if ((int)obj["status"] == 200)
                {
                    IEnumerable<JToken> datas = obj.SelectTokens("$.content.liveList[*]");
                    int count = 0;
                    foreach (JToken liveInfo in datas)
                    {
                        if (LiveCache.ContainsKey(liveInfo["liveId"]))
                            break;
                        //懒得想算法了，直接很粗暴的按的字分割了
                        //title字段师xxx的直播间/电台，所以上面的idcache就直接看看有没有这个键，有的话直接推送了
                        string[] name = liveInfo["title"].ToString().Split(new string[] { "的" }, StringSplitOptions.RemoveEmptyEntries);
                        if (PocketPlugins.RunTimeCfg.ContainsKey(name[0]))
                        {
                            //File.AppendAllText("LiveError.log", "捕获：" + liveInfo["liveId"] + "\r\n");
                            PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "捕获到 " + name[0] + " 的直播，LiveID：" + liveInfo["liveId"]);
                            LiveCache.Add(liveInfo["liveId"], 1);
                            ListenRunTimeConfig cfg = (ListenRunTimeConfig)PocketPlugins.RunTimeCfg[name[0]];
                            foreach(long qqGourp in cfg.QQGroups)
                            {
                                //PocketPlugins.Api.SendGroupMsg(qqGourp, "你的小偶像 " + name[0] + " 打开了一个直播\r\n直播连接：https://h5.48.cn/2017appshare/memberLiveShare/index.html?id=" + liveInfo["liveId"] + "\r\n使用KD For PC观看直播~也很棒哦！");
                            }
                        }
                        count++;
                        lasttime = long.Parse(liveInfo["startTime"].ToString());
                    }
                    if (count == 20)
                        return false;
                    else
                        return true;
                }
                return true;
            }
            catch (Exception ex)
            {
                WriteLog(ex);
                return true;
            }
        }

        //获取用户名1

        public static string GetUserName(string id)
        {
            string api = "http://zhibo.ckg48.com/Recharge/ajax_post_checkinfo";
            string content = "pocket_id=" + id;
            byte[] bs = Encoding.UTF8.GetBytes(content);
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(api);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = bs.Length;
            request.UserAgent = "python - requests / 2.19.1";
            Stream myRequestStream = request.GetRequestStream();
            StreamWriter myStreamWriter = new StreamWriter(myRequestStream, Encoding.GetEncoding("gb2312"));
            myStreamWriter.Write(content, 0, bs.Length);
            myStreamWriter.Close();

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();
            StreamReader myStreamReader = new StreamReader(myResponseStream, Encoding.GetEncoding("utf-8"));
            string retString = myStreamReader.ReadToEnd();
            myStreamReader.Close();
            myResponseStream.Close();
            return retString;
        }

        //获取用户信息
        public static string GetUserInfo(string user,string pwd,string IMEI)
        {
            string api = "https://puser.48.cn/usersystem/api/user/v1/login/phone";
            HttpRequestMessage req = new HttpRequestMessage();
            req.Method = HttpMethod.Post;
            req.RequestUri = new Uri(api);
            if (IMEI == "")
                req.Headers.Add("IMEI", IMEI);
            else
                req.Headers.Add("IMEI", PocketSetting.IMEI);

            req.Headers.Add("Version", PocketVersion);
            req.Headers.Add("User-Agent", PocketAgent);
            req.Headers.Add("os", "Android");
            req.Headers.Add("token", "0");
            JObject rss = new JObject(
                new JProperty("password", pwd),
                new JProperty("account", user),
                new JProperty("longitude", 0),
                new JProperty("latitude", 0)
            );
            req.Content = new StringContent(rss.ToString(), Encoding.UTF8, "application/json");
            string userJson = new HttpClient().SendAsync(req).Result.Content.ReadAsStringAsync().Result;
            return userJson;
        }

        public static string GetRoomID(string idolname)
        {
            try
            {
                string api = "https://pjuju.48.cn/imsystem/api/im/v1/search";
                HttpRequestMessage req = new HttpRequestMessage();
                req.Method = HttpMethod.Post;
                req.RequestUri = new Uri(api);
                string IMEI = "";
                for (int i = 0; i < 3; i++)
                {
                    System.Random randnum = new Random();
                    int j = randnum.Next(10000, 99999);
                    IMEI = IMEI + j.ToString();
                }
             
                req.Headers.Add("IMEI", IMEI);
                req.Headers.Add("version", "5.0.1");
                //            req.Headers.Add("User-Agent", "Mobile_Pocket");
                req.Headers.Add("os", "Android");
                //            req.Headers.Add("Content-Length", "67");
                //            req.Headers.Add("Accept-Encoding", "gzip");
                req.Headers.Add("version", "5.0.1");
                //            req.Headers.Add("Connection", "Keep-Alive");
                //            req.Headers.Add("Content-Type", "application/json;charset=utf-8");
                req.Headers.Add("build", "1");
                JObject rss = new JObject(
                    new JProperty("roomName", idolname)
                );
                req.Content = new StringContent(rss.ToString(), Encoding.UTF8, "application/json");
                string userJson = new HttpClient().SendAsync(req).Result.Content.ReadAsStringAsync().Result;
                return userJson;
            }

            catch (Exception ex)
            {
                File.AppendAllText("error_roomid.log", DateTime.Now.ToString() + "\r\n" + ex.ToString() + "\r\n" + ex.StackTrace + "\r\n");
                return "";
            }

        }


        //获取文件的共用方法
        private static string GetAudio(string url, string suffix)
        {
            try
            {
                WebRequest req = WebRequest.Create(url);
                WebResponse rep = req.GetResponse();
                string filename = Path.GetRandomFileName() + "." + suffix;
                Stream stream = new FileStream(Path.Combine("data/record/", filename), FileMode.Create);
                byte[] bArr = new byte[1024];
                int size = rep.GetResponseStream().Read(bArr, 0, (int)bArr.Length);
                while (size > 0)
                {
                    stream.Write(bArr, 0, size);
                    size = rep.GetResponseStream().Read(bArr, 0, (int)bArr.Length);
                }
                stream.Close();
                rep.GetResponseStream().Close();
                return filename;
            }
            catch (Exception ex)
            {
                File.AppendAllText("error.log", DateTime.Now.ToString() + "\r\n" + ex.ToString() + "\r\n" + ex.StackTrace + "\r\n");
                return "";
            }

        }
        private static string GetImage(string url)
        {
            try
            {
                WebRequest req = WebRequest.Create(url);
                WebResponse rep = req.GetResponse();
                Bitmap img = new Bitmap(rep.GetResponseStream());
                string filename = Path.GetRandomFileName() + ".jpg";
                img.Save(Path.Combine("data/image/", filename));
                return filename;
            }
            catch (Exception ex)
            {
                File.AppendAllText("error.log", DateTime.Now.ToString() + "\r\n" + ex.ToString() + "\r\n" + ex.StackTrace + "\r\n");
                return "";
            }

        }
        
        private static Semaphore sem = new Semaphore(1, 1);
        public static void WriteLog(Exception ex)
        {
            sem.WaitOne();
            File.AppendAllText("pocket.runtime.log", string.Format("\r\n{0} 发生了异常:\r\n简要信息：{1}\r\n堆栈：\r\n{2}\r\n", DateTime.Now.ToString(), ex.Message, ex.StackTrace));
            sem.Release();
        }

        public static void sendgroupmsgChose(long groupid,string text)
        {
            //string api = string.Format("http://127.0.0.1:5700/send_group_msg?group_id={0}&message={1}",groupid.ToString(),text);
            if (PocketPlugins.CommonCfg.httpmsg)
                try
                {
                    //PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "HTTP消息");
                    string api = "http://127.0.0.1:" + PocketPlugins.CommonCfg.httpport + "/send_group_msg";
                    HttpRequestMessage req = new HttpRequestMessage();
                    req.Method = HttpMethod.Post;
                    req.RequestUri = new Uri(api);
                    JObject rss = new JObject(
                        new JProperty("group_id", groupid.ToString()),
                        new JProperty("message", text)
                                );
                    req.Content = new StringContent(rss.ToString(), Encoding.UTF8, "application/json");
                    string userJson = new HttpClient().SendAsync(req).Result.Content.ReadAsStringAsync().Result;
                }
                catch (Exception ex)
                {
                    PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "发送HTTP消息失败，转为普通消息");
                    PocketPlugins.Api.SendGroupMsg(groupid, text);
                    File.AppendAllText("errorhttp.log", DateTime.Now.ToString() + "\r\n" + ex.ToString() + "\r\n" + ex.StackTrace + "\r\n");
                }
            else
            {
                //PocketPlugins.Api.AddLog(10, CoolQLogLevel.Info, "非HTTP消息");
                PocketPlugins.Api.SendGroupMsg(groupid, text);
            }
                
        }

        public static bool sendprivitemsghttp(string text)
        {
            return true;
        }

        public static void WriteLog(string filename,Exception ex)
        {
            sem.WaitOne();
            File.AppendAllText(filename, string.Format("\r\n{0} 发生了异常:\r\n简要信息：{1}\r\n堆栈：\r\n{2}\r\n", DateTime.Now.ToString(), ex.Message, ex.StackTrace));
            sem.Release();
        }
    }
}
