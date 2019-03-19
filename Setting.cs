using Newbe.CQP.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Teemo.CoolQ.PocketPlugins
{
    public partial class panel : Form
    {
        private string roomid = "";

        public panel()
        {
            InitializeComponent();
            foreach (var key in PocketPlugins.RunTimeCfg.Keys)
            {
                ListenRunTimeConfig cfg = (ListenRunTimeConfig)PocketPlugins.RunTimeCfg[key];
                list_listen.Items.Add(cfg);
            }
            list_listen.DisplayMember = "IdolName";
        }

        private void btn_gettoken_Click(object sender, EventArgs e)
        {
            UserInfo user = new UserInfo();
            user.UserName = txt_phone.Text;
            user.PassWord = txt_password.Text;


            string json = Common.GetUserInfo(user.UserName, user.PassWord, "");
            JObject obj = JObject.Parse(json);
            if ((int)obj["status"] == 200)
            {
                user.TokenUpdate = DateTime.Now;
                user.PocketToken = obj["content"]["token"].ToString();

                lab_token.Text = string.Format("Token：{0}  Token更新时间：{1}", user.PocketToken, user.TokenUpdate.ToString());
            }
            else
            {
                lab_token.Text = string.Format("Token：{0}", obj["message"].ToString());
            }

            PocketPlugins.User = user;
        }

        private void btn_findidol_Click(object sender, EventArgs e)
        {
            roomid = "";
            string idolname = txt_idolname.Text;
            string[] roominfo = File.ReadAllLines("room.data");

            bool findres = false;
            foreach (string room in roominfo)
            {
                if (room.Contains(idolname))
                {
                    findres = true;
                    roomid = room.Split(':')[1];
                    lab_find.Text = "查找到的信息：房间id为" + roomid;
                }
            }
            if (!findres)
                lab_find.Text = "查找不到信息请手动填写在右边→";
        }

        private void btn_idoladd_Click(object sender, EventArgs e)
        {
            if (list_listen.Items.Count >= PocketSetting.MaxListen)
            {
                MessageBox.Show(string.Format("超出了{0}人的限制！禁止添加！", PocketSetting.MaxListen), "错误");
                return;
            }

            if (int.Parse(txt_roomdelay.Text) < 5000 || int.Parse(txt_longdelay.Text) < 25000)
            {
                MessageBox.Show("短延迟不能低于5000，长延迟不能低于25000", "错误");
                return;
            }


            ListenRunTimeConfig cfg = new ListenRunTimeConfig();
            cfg.IdolName = txt_idolname.Text;
            if(roomid == "")
            {
                if (textBox_handname.Text != "")
                    roomid = textBox_handname.Text;
                else
                {
                    MessageBox.Show("无法添加这位小偶像，请确认是否已经正确查找到roomid信息");
                    return;
                }

            }
            cfg.RoomId = int.Parse(roomid);
            cfg.TransmitText = cb_text.Checked;
            cfg.TransmitAudio = cb_audio.Checked;
            cfg.TransmitVideo = cb_video.Checked;
            cfg.TransmitImage = cb_image.Checked;
            cfg.TransmitFanpai = cb_fanpai.Checked;
            cfg.TransmitGift = cb_flip.Checked;
            cfg.TransmitLive = cb_live.Checked;
            cfg.TransmitFlip = cb_flip.Checked;
            cfg.Atall = atall.Checked;

            cfg.QQGroups = new List<long>();
            string[] qqGroups = txt_qqgroups.Text.Split(',');
            if (qqGroups.Length == 1)
            {
                if (qqGroups[0] != "")
                    cfg.QQGroups.Add(long.Parse(qqGroups[0]));
            }
            else
            {
                foreach (string qqGroup in qqGroups)
                {
                    if (qqGroup != "")
                    {
                        cfg.QQGroups.Add(long.Parse(qqGroup));
                    }
                }
            }
            if (cfg.QQGroups.Count == 0)
            {
                MessageBox.Show("请添加QQ群！", "错误");
                return;
            }

            if (rb_air.Checked)
                PocketPlugins.CommonCfg.CoolQAir = true;
            if (rb_pro.Checked)
                PocketPlugins.CommonCfg.CoolQAir = false;

            cfg.Delay = int.Parse(txt_roomdelay.Text);
            cfg.ShortDelay = int.Parse(txt_roomdelay.Text);
            cfg.LongDelay = int.Parse(txt_longdelay.Text);
            cfg.First = true;

            cfg.Proxy = PocketPlugins.Proxy;

            if (PocketPlugins.RunTimeCfg.ContainsKey(cfg.IdolName))
            {
                PocketPlugins.RunTimeCfg[cfg.IdolName] = cfg;
                MessageBox.Show(cfg.IdolName + " 配置已存在，本次添加为配置替换操作", "成功");
            }
            else
            {
                list_listen.Items.Add(cfg);
                list_listen.DisplayMember = "IdolName";
                PocketPlugins.RunTimeCfg.Add(cfg.IdolName, cfg);
                MessageBox.Show(cfg.IdolName + " 信息配置成功", "成功");
            }
            roomid = "";
        }

        private void btn_run_Click(object sender, EventArgs e)
        {
            /*if (txt_livedelay.Text == "")
            {
                MessageBox.Show("请输入直播监听延迟时间");
                return;
            }
            PocketPlugins.CommonCfg.LiveDelay = int.Parse(txt_livedelay.Text);*/
            int timedelay = 0;
            foreach (var idol in PocketPlugins.RunTimeCfg.Keys)
            {
                Common.StartListenRoomTask(idol.ToString(), timedelay);
                timedelay = timedelay + 5000;
            }

            MessageBox.Show("任务启动完毕，请留意酷Q日志以及小偶像房间消息，欢迎多给开发投食", "完成");
        }

        private void btn_configsave_Click(object sender, EventArgs e)
        {
            JObject configFile = new JObject();

            if (PocketPlugins.User != null)
                configFile["User"] = JToken.FromObject(PocketPlugins.User);

            configFile["IMEI"] = PocketSetting.IMEI;
            configFile["ShortDelay"] = PocketSetting.ShortDelay = int.Parse(txt_roomdelay.Text);
            configFile["LongDelay"] = PocketSetting.LongDelay = int.Parse(txt_longdelay.Text);
            configFile["httpport"]  = textBox_HTTPPORT.Text;

            /*if(txt_livedelay.Text != "")
                configFile["LiveDelay"] = txt_livedelay.Text;*/

            if (AutoStart.Checked)
                configFile["AutoStart"] = true;
            else
                configFile["AutoStart"] = false;

            if (checkBox_http.Checked)
                configFile["httpmsg"]  = true;
            else
                configFile["httpmsg"] = false;

            if (rb_air.Checked)
                configFile["CoolQAir"] = true;
            else
                configFile["CoolQAir"] = false;

            JArray arr = new JArray();
            foreach (var idol in PocketPlugins.RunTimeCfg.Keys)
            {
                ListenRunTimeConfig config = (ListenRunTimeConfig)PocketPlugins.RunTimeCfg[idol];
                arr.Add(JToken.FromObject(config));
            }
            configFile["IdolInfo"] = arr;

            File.WriteAllText("PocketConfig.json", configFile.ToString());
            MessageBox.Show("保存完毕", "成功");
        }

        private void btn_loadconfig_Click(object sender, EventArgs e)
        {
            if (!File.Exists("PocketConfig.json"))
            {
                MessageBox.Show("配置文件不存在！请检查文件名是否正确", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            string json = File.ReadAllText("PocketConfig.json");
            JObject jsonObj = JObject.Parse(json);

            if (jsonObj.Property("User") != null)
            {
                PocketPlugins.User = JsonConvert.DeserializeObject<UserInfo>(jsonObj["User"].ToString());
                txt_phone.Text = PocketPlugins.User.UserName;
                lab_token.Text = string.Format("Token：{0}  Token更新时间：{1}", PocketPlugins.User.PocketToken, PocketPlugins.User.TokenUpdate.ToString());
            }

            if (jsonObj.Property("IMEI") != null)
            {
                PocketSetting.IMEI = jsonObj["IMEI"].ToString();
                txt_imei.Text = PocketSetting.IMEI;
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
                    rb_air.Checked = true;
                    PocketPlugins.CommonCfg.CoolQAir = true;
                }
                else
                {
                    rb_pro.Checked = true;
                    PocketPlugins.CommonCfg.CoolQAir = false;
                }
            }

            if (jsonObj.Property("httpmsg") != null)
            {
                if (bool.Parse(jsonObj["httpmsg"].ToString()))
                {
                    checkBox_http.Checked = true;
                    PocketPlugins.CommonCfg.httpmsg = true;
                }
                else
                {
                    checkBox_http.Checked = false;
                    PocketPlugins.CommonCfg.httpmsg = false;
                }
            }
            PocketPlugins.CommonCfg.httpport = textBox_HTTPPORT.Text = jsonObj["httpport"].ToString();

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
                    list_listen.Items.Add(config);
                }
                list_listen.DisplayMember = "IdolName";
            }
            MessageBox.Show("加载完成", "成功");
        }

        private void list_listen_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (list_listen.SelectedItem == null)
                return;

            ListenRunTimeConfig config = (ListenRunTimeConfig)list_listen.SelectedItem;
            txt_idolname.Text = config.IdolName;
            roomid = config.RoomId.ToString();
            txt_roomdelay.Text = PocketSetting.ShortDelay.ToString();
            if (int.Parse(txt_roomdelay.Text) < 5000)
                txt_roomdelay.Text = "5000";
            txt_longdelay.Text = PocketSetting.LongDelay.ToString();
            if (int.Parse(txt_longdelay.Text) < 25000)
                txt_longdelay.Text = "25000";
            //           txt_roomdelay.Text = config.ShortDelay.ToString();
            //           txt_longdelay.Text = config.LongDelay.ToString();

            cb_audio.Checked = config.TransmitAudio;
            cb_image.Checked = config.TransmitImage;
            cb_fanpai.Checked = config.TransmitFanpai;
            cb_text.Checked = config.TransmitText;
            cb_video.Checked = config.TransmitVideo;
            cb_live.Checked = config.TransmitLive;
            cb_flip.Checked = config.TransmitFlip;
            cp_gift.Checked = config.TransmitGift;
            atall.Checked = config.Atall;

            if (config.QQGroups.Count == 1)
                txt_qqgroups.Text = config.QQGroups[0].ToString();
            else
            {
                txt_qqgroups.Text = config.QQGroups[0].ToString();
                for (int tmp = 1; tmp < config.QQGroups.Count; tmp++)
                {
                    txt_qqgroups.Text += "," + config.QQGroups[tmp].ToString();
                }
            }
        }

        private void btn_delete_Click(object sender, EventArgs e)
        {
            if (list_listen.SelectedItem == null)
            {
                MessageBox.Show("请先选中配置！", "错误");
                return;
            }
            ListenRunTimeConfig config = (ListenRunTimeConfig)list_listen.SelectedItem;
            PocketPlugins.RunTimeCfg.Remove(config.IdolName);
            list_listen.Items.Remove(config);
            MessageBox.Show("删除完毕", "成功");
        }

        private void btn_proxyapply_Click(object sender, EventArgs e)
        {
            if (txt_proxy.Text == "")
            {
                MessageBox.Show("即将进行代理关闭操作，如果是误操作，请自行再次查找代理");
                PocketPlugins.Proxy = null;
                foreach (string key in PocketPlugins.RunTimeCfg.Keys)
                {
                    ListenRunTimeConfig cfg = (ListenRunTimeConfig)PocketPlugins.RunTimeCfg[key];
                    cfg.Proxy = null;
                }
                MessageBox.Show("更换完成，请留意监听情况和酷Q日志");
                return;
            }

            if (!txt_proxy.Text.Contains(":"))
            {
                MessageBox.Show("大兄弟，不知道用别乱折腾了,欢迎捐助！");
                return;
            }
            string fullProxy = txt_proxy.Text;
            PocketProxy proxy = new PocketProxy();
            proxy.FullIP = fullProxy;
            string[] info = fullProxy.Split(':');
            proxy.IP = info[0];
            proxy.Port = info[1];
            proxy.UseProxy = true;

            if (roomid == "")
            {
                MessageBox.Show("由于有自动化测试需要，请先随机查找一个成员后再应用", "错误");
                return;
            }
            string json = Common.GetRoomMessage(proxy, PocketPlugins.User.PocketToken, roomid);
            if (json == "")
            {
                MessageBox.Show("代理不可用，换一个吧", "错误");
            }
            try
            {
                JObject obj = JObject.Parse(json);
                if ((int)obj["status"] != 200)
                    MessageBox.Show("可能有问题,建议更换");
                else
                {
                    MessageBox.Show("代理可用，即将开始更换代理");
                    PocketPlugins.Proxy = proxy;
                    foreach (string key in PocketPlugins.RunTimeCfg.Keys)
                    {
                        ListenRunTimeConfig cfg = (ListenRunTimeConfig)PocketPlugins.RunTimeCfg[key];
                        cfg.Proxy = proxy;
                    }
                    MessageBox.Show("更换完成，请留意监听情况和酷Q日志");
                }

            }
            catch (Exception)
            {
                MessageBox.Show("代理不可用，换一个吧", "错误");
            }

        }

        private void btn_change_imei_Click(object sender, EventArgs e)
        {
            PocketSetting.IMEI = txt_imei.Text;
        }

        private void txt_roomdelay_TextChanged(object sender, EventArgs e)
        {

        }

        private void panel_Load(object sender, EventArgs e)
        {           
            if (txt_roomdelay.Text == "")
                txt_roomdelay.Text = "10000";
            if (txt_longdelay.Text == "")
                txt_longdelay.Text = "25000";
            cb_audio.Checked = true;
            cb_image.Checked = true;
            cb_fanpai.Checked = true;
            cb_text.Checked = true;
            cb_video.Checked = true;
            cb_live.Checked = true;
            cb_flip.Checked = true;
            cp_gift.Checked = true;
            if (!File.Exists("PocketConfig.json"))
            {
                MessageBox.Show("配置文件不存在！请检查文件名是否正确", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            btn_loadconfig_Click(sender,e);
            string json = File.ReadAllText("PocketConfig.json");
            JObject jsonObj = JObject.Parse(json);

            if (PocketPlugins.CommonCfg.msglog == true)
                checkBox_log.Checked = true;
            else
                checkBox_log.Checked = false;

            if (jsonObj.Property("User") != null)
            {
                PocketPlugins.User = JsonConvert.DeserializeObject<UserInfo>(jsonObj["User"].ToString());
                txt_phone.Text = PocketPlugins.User.UserName;
                txt_password.Text = PocketPlugins.User.PassWord;
                txt_imei.Text = PocketSetting.IMEI;
                lab_token.Text = string.Format("Token：{0}  Token更新时间：{1}", PocketPlugins.User.PocketToken, PocketPlugins.User.TokenUpdate.ToString());
            }
            if (jsonObj.Property("AutoStart") != null)
            {
                //bool auto = Convert.ToBoolean(jsonObj.["AutoStart"]);
                if (Convert.ToBoolean(jsonObj["AutoStart"]) == true)
                {
                    AutoStart.Checked = true;
                    //PocketPlugins.CommonCfg.AutoStart = true;
                }

                else
                {
                    AutoStart.Checked = false;
                    //PocketPlugins.CommonCfg.AutoStart = false;
                }
            }

            if (jsonObj.Property("httpmsg") != null)
            {
                //bool auto = Convert.ToBoolean(jsonObj.["AutoStart"]);
                if (Convert.ToBoolean(jsonObj["httpmsg"]) == true)
                {
                    checkBox_http.Checked = true;
                    PocketPlugins.CommonCfg.httpmsg = true;
                }

                else
                {
                    checkBox_http.Checked = false;
                    PocketPlugins.CommonCfg.httpmsg = false;
                }
            }

            if (jsonObj.Property("IMEI") != null)
            {
                PocketSetting.IMEI = jsonObj["IMEI"].ToString();
                txt_imei.Text = PocketSetting.IMEI;
            }

            if (jsonObj.Property("httpport") != null)
            {
                PocketPlugins.CommonCfg.httpport = jsonObj["httpport"].ToString();
                textBox_HTTPPORT.Text = PocketPlugins.CommonCfg.httpport;
            }
            if (jsonObj.Property("CoolQAir") != null)
            {
                bool Air = bool.Parse(jsonObj["CoolQAir"].ToString());
                if (Air)
                {
                    rb_air.Checked = true;
                    PocketPlugins.CommonCfg.CoolQAir = true;
                }
                else
                {
                    rb_pro.Checked = true;
                    PocketPlugins.CommonCfg.CoolQAir = false;
                }
            }

            //string text = "♡";
            //Common.sendgroupmsgChose(217563254, text);
        }

        private void btn_idsearch_Click(object sender, EventArgs e)
        {
            btn_idsearch.Enabled = false;
            if (txt_searchname.Text == "")
                MessageBox.Show("请填写姓名，支持模糊查找");
            else
            {
                string roomidmsg = "";
                string idolname = txt_searchname.Text;
                string json = Common.GetRoomID(idolname);
//                File.AppendAllText("roomid.log", DateTime.Now.ToString() + "\r\n" + json + "\r\n");
                JObject obj = JObject.Parse(json);
                if ((int)obj["status"] == 200)
                {
                    IEnumerable<JToken> datas = obj.SelectTokens("$.content.data[*]");
                    foreach (JToken roominfo in datas)
                    {
                        if ((int)(roominfo["roomType"]) == 0)
                        {
                            roomidmsg = roomidmsg + roominfo["creatorName"] + ":" + roominfo["roomId"] + " " + roominfo["roomName"] + "\r\n";
                        }
                    }

                    if (roomidmsg != "")
                    {
                        text_idbox.Text = roomidmsg;
                        MessageBox.Show("查找完毕，请自行编辑room.data添加保存后重开插件加载，注意不要重复添加");
                    }
                    else
                    {
                        text_idbox.Text = "没有查找到相关小偶像";
                    }
                }

                else
                {
                    MessageBox.Show("查找失败，请检查网络或者稍后重试，本功能短时间大量使用可能导致IP封锁，不确定，佛了");
                }
            }
            btn_idsearch.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {

            string text = "♡";
            string testqqgroup = text_testQQ.Text;
            if (testqqgroup == "")
            {
                MessageBox.Show("请填写测试QQ群");
                return;
            }
            Common.sendgroupmsgChose(long.Parse(testqqgroup), text);
            MessageBox.Show("尝试发送到群 "+ testqqgroup +" 成功，请查看消息是否正常，如不正常检查调试日志和目录下errorhttp.log");
            //Common.sendgroupmsgChose(217563254, text2);
            //Encoding utf8;
            //Encoding gb18030;
            //utf8 = Encoding.GetEncoding("UTF-8");
            //gb18030 = Encoding.GetEncoding("GB2312");
            //byte[] encodeBytes = Encoding.GetEncoding("utf-16").GetBytes(text);
            //encodeBytes = Encoding.Convert(Encoding.GetEncoding("utf-16"), Encoding.GetEncoding("GB2312"), encodeBytes);
            //File.AppendAllText("testmsg.log", Encoding.GetEncoding("GB18030").GetString(encodeBytes));
            //File.AppendAllText("testmsg.log", text);
            //string newtext = Encoding.Convert(gb18030, utf8, encodeBytes);
            //string newtext = text;
            //byte[] encodeBytes1 = System.Text.Encoding.GetEncoding("UTF-16").GetBytes(text1);
            //byte[] encodeBytes2 = System.Text.Encoding.Default.GetBytes(text2);
            //byte[] encodeBytes = System.Text.Encoding.Default.GetBytes(text);
            //string newtext = System.Text.Encoding.;
            //string newtext = System.Text.Encoding.GetEncoding("GB18030").GetString(encodeBytes);
            //string newtext = BitConverter.ToString(encodeBytes);
            //PocketPlugins.Api.SendGroupMsg(217563254, Encoding.GetEncoding("GB2312").GetString(encodeBytes));
            //text1 = System.Text.Encoding.GetEncoding("GB18030").GetString(encodeBytes1);
            //text2 = System.Text.Encoding.GetEncoding("GB18030").GetString(encodeBytes2);
            //PocketPlugins.Api.SendGroupMsg(217563254, text1+"\r\n"+ text2+"\r\n"+ System.Text.Encoding.GetEncoding("GB18030").GetString(encodeBytes1)+ "\r\n"+System.Text.Encoding.GetEncoding("GB18030").GetString(encodeBytes2));
        }

        private void checkBox_http_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_http.Checked)
                PocketPlugins.CommonCfg.httpmsg = true;
            else
                PocketPlugins.CommonCfg.httpmsg = false;
        }

        private void checkBox_log_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_log.Checked)
            {
                PocketPlugins.Api.AddLog(10, CoolQLogLevel.Debug, "口袋房间调试日志开始");
                PocketPlugins.CommonCfg.msglog = true;
            }

            else
            {
                PocketPlugins.Api.AddLog(10, CoolQLogLevel.Debug, "口袋房间调试日志关闭");
                PocketPlugins.CommonCfg.msglog = false;
            }
                
        }
    }
}
