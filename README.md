#更新<br>
1.插件启动自动更新TOKEN（每次必自动）<br>
2.插件启动自动执行任务（如配置）<br>
3.上传傻瓜包


#增加<br>
增加翻牌人的名字，# 感谢flydsc的分享，现在可以通过鸡腿充值查询接口来查询口袋id对应的nickname，舍弃留言板监听。<br>
消息合并，一个监听时间段内的多条消息合并发送，减少文字量，减少酷q发送消息数。<br>
增加多目标时自动线程延时间隔，避免被封，最大数量增加到5个。<br>
更新room.data。<br>
面板初始化自动设置多项设置，减少操作填写。<br>
恢复礼物接口。<br>
感谢提莫开源，很好的学习借鉴。<br>

                      2018-08-15

# 口袋监听插件
<p>感谢大家一直以来的支持，口袋监听插件今后将不再维护</p>
<p>为了给有能力的开发继续维护这份插件，所以特地将代码剥离部分个人功能后，开源出来</p>

### 依赖
1. Newbe.CQP.Framework
2. Newtonsoft.Json
<p>请自行从nuget获取以上两个组件</p>

### 逻辑
1. StartListenRoomTask
2. GetRoomMessage
3. ProcessRoomMessage
4. Sleep

### good lucky


