using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Utility.Numerics;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Module.Draw;
using KodakkuAssist.Script;
using Newtonsoft.Json;

namespace LRXR.Workspace.MyScripts;

[ScriptType(
    name: "温达斯_第三巡行",
    territorys: [1368],
    guid: "160bfc20-949d-4edb-9eba-39e27f6e7aa0",
    version: "0.0.0.5",
    author: "LRXR",
    note: "初版\n" +
          "BOSS1、BOSS2、BOSS3、BOSS4画完，1.5、2.5小怪还未画\n" +
          "鸣谢 南雲鉄虎")]
public class 温达斯_第三巡行
{
    // ============================================================
    // 用户设置
    // ============================================================
    [UserSetting("启用文字提示")] public bool EnableTextPrompts { get; set; } = true;

    [UserSetting("启用开发者调试模式")] public bool EnableDeveloperMode { get; set; } = false;

    [UserSetting("安全的颜色")] public ScriptColor SafeColour { get; set; } = new() { V4 = new Vector4(0, 1, 0, 1) };

    [UserSetting("危险的颜色")] public ScriptColor DangerColour { get; set; } = new() { V4 = new Vector4(1, 0, 0, 1) };


    // ============================================================
    // 类变量
    // ============================================================
    private readonly Queue<ulong> 展开布阵法_钻环跳圈的火炎_队列 = new();
    private readonly Queue<ulong> 戈耳狄系统_大放电_队列 = new();
    private int 戈耳狄系统_已清除数;

    private string 黄昏披光暗_状态 = "";
    private readonly List<(ulong sourceId, int durationMs)> 审理之光_待画 = new();

    // ============================================================
    // 初始化
    // ============================================================
    public void Init(ScriptAccessory accessory)
    {
        accessory.Method.RemoveDraw(".*");
        戈耳狄系统_已清除数 = 0;
        DebugLog(accessory, "脚本已初始化");
    }
    // ============================================================
    // 通用技能方法
    // ============================================================

    [ScriptMethod(name: "屏幕提示_非BUFF类",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50215|50187|50161|50153|50203|50317|50343|50349|50694|49130|49179)$"])]
    public void 屏幕提示_非BUFF类(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        if (@event["ActionId"] == "50203")
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("远离钢铁", durationMs, true);
        }
        else if (@event["ActionId"] == "50343")
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("AOE！注意分组！", durationMs, true);
        }
        else if (@event["ActionId"] == "50349")
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("击退！", durationMs, true);
        }
        else if (@event["ActionId"] == "49130")
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("下层拆地板，请去上层！", durationMs, true);
        }
        else
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("AOE", durationMs, true);
        }
    }

    [ScriptMethod(name: "屏幕提示_BUFF类_记录",
        eventType: EventTypeEnum.StatusAdd,
        eventCondition: ["StatusID:regex:^(5352|5353)$"],
        userControl: false)]
    public void 屏幕提示_BUFF类_记录(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var targetId)) return;
        if (targetId != accessory.Data.Me) return;
        黄昏披光暗_状态 = @event["StatusID"];
    }


    [ScriptMethod(name: "分摊",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50214|50185|50158|50211|49182)$"])]
    public void 分摊(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var targetId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;
        var color = @event["ActionId"] == "50214" ? DangerColour.V4 : SafeColour.V4;
        if (@event["ActionId"] == "50158"||@event["ActionId"] == "49182") durationMs += 4000;
        DrawCircleOnSourceOrTarget(accessory, targetId, 6, durationMs, color);
        if (@event["ActionId"] != "50214")
        {
            GuideSelfToTarget(accessory, targetId, durationMs, SafeColour.V4);
        }
    }

    // ============================================================
    // ---- BOSS 1 恶魔香托托 ----
    // ============================================================

    #region BOSS 1 恶魔香托托

    [ScriptMethod(name: "实证研究",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50208)$"])]
    public void 实证研究(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, sourceId, 12, 80, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "粉身碎骨的地震",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50196)$"])]
    public void 粉身碎骨的地震(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, sourceId, 12, 30, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "钻环跳圈的火炎",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50201)$"])]
    public void 钻环跳圈的火炎(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawSteel(accessory, "钻环跳圈的火炎", sourceId, 6, durationMs, 0, SafeColour.V4.WithW(2f));
    }

    [ScriptMethod(name: "展开布阵法_钻环跳圈的火炎",
        eventType: EventTypeEnum.Tether,
        eventCondition: ["Id:regex:^01(80|7F)$"])]
    public void 展开布阵法_钻环跳圈的火炎(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var TargetId)) return;
        展开布阵法_钻环跳圈的火炎_队列.Enqueue(TargetId);

        DrawSteel(accessory, $"展开布阵法_火炎_{TargetId}", TargetId, 6, 30000, 0, SafeColour.V4);
    }

    [ScriptMethod(name: "展开布阵法_钻环跳圈的火炎_清除",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50202)$"])]
    public void 展开布阵法_钻环跳圈的火炎_清除(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetDurationMs(@event, out int durationMs)) return;
        if (展开布阵法_钻环跳圈的火炎_队列.Count == 0) return;
        var targetId = 展开布阵法_钻环跳圈的火炎_队列.Dequeue();

        System.Threading.Tasks.Task.Delay(durationMs).ContinueWith(_ =>
        {
            accessory.Method.RemoveDraw($"展开布阵法_火炎_{targetId}");
        });
    }

    [ScriptMethod(name: "靠近乘凉的冰洁",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50203)$"])]
    public void 靠近乘凉的冰洁(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawSteel(accessory, "靠近乘凉的冰洁", sourceId, 10, durationMs, 0, DangerColour.V4);
    }

    #endregion

    // ============================================================
    // ---- 1.5 小怪 ----
    // ============================================================

    // ============================================================
    // ---- BOSS 2 巨神重现 亚历山大 ----
    // ============================================================

    #region 巨神重现 亚历山大

    [ScriptMethod(name: "圣箭",
        eventType: EventTypeEnum.ActionEffect,
        eventCondition: ["ActionId:regex:5012[6-9]$"])] //50124释放
    public void 圣箭(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetBossTransform(@event, out var bossPos, out var bossFacing)) return;

        DrawDangerFan(accessory, bossPos, bossFacing, 90, 100, 2000, DangerColour.V4);
    }

    [ScriptMethod(name: "圣箭_持续",
        eventType: EventTypeEnum.ActionEffect,
        eventCondition: ["ActionId:regex:^(50478)$"])]
    public void 圣箭_持续(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetBossTransform(@event, out var bossPos, out var bossFacing)) return;

        DrawDangerFan(accessory, bossPos, bossFacing, 90, 100, 1000, DangerColour.V4);
    }

    [ScriptMethod(name: "审理之光",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50146|50147)$"])]
    public void 审理之光(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        int delay = @event["ActionId"] == "50146" ? 0 : 6700;
        durationMs = @event["ActionId"] == "50146" ? durationMs : durationMs - 6700;
        DrawRectOnOwnerWithDelay(accessory, sourceId, 50, 50, durationMs, delay, DangerColour.V4);
    }

    [ScriptMethod(name: "圣炎",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50143)$"])]
    public void 圣炎(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var targetId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;
        DrawTriangle(accessory, targetId, 90, 25f, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "戈耳狄系统_大放电",
        eventType: EventTypeEnum.Tether,
        eventCondition: ["Id:regex:^(01AC)$"])]
    public void 戈耳狄系统_大放电(Event @event, ScriptAccessory accessory)
    {
        // N个Tether两两一组: 第一组立刻画, 后续每组等前一组清了再画
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        戈耳狄系统_大放电_队列.Enqueue(sourceId);

        if (戈耳狄系统_大放电_队列.Count <= 2) // 第一组立刻画
        {
            DrawSteel(accessory, $"戈耳狄系统_大放电_{sourceId}", sourceId, 18, 30000, 0, DangerColour.V4);
        }
    }

    [ScriptMethod(name: "戈耳狄系统_大放电_清除",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50156)$"])]
    public void 戈耳狄系统_大放电_清除(Event @event, ScriptAccessory accessory)
    {
        if (戈耳狄系统_大放电_队列.Count == 0) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;
        var sourceId = 戈耳狄系统_大放电_队列.Dequeue();
        戈耳狄系统_已清除数++;

        accessory.Method.RemoveDraw($"戈耳狄系统_大放电_{sourceId}");

        // 每清完2个 → 画下一组前2个
        if (戈耳狄系统_已清除数 % 2 == 0 && 戈耳狄系统_大放电_队列.Count > 0)
        {
            int drawn = 0;
            foreach (var nextSourceId in 戈耳狄系统_大放电_队列)
            {
                if (drawn >= 2) break;
                DrawSteel(accessory, $"戈耳狄系统_大放电_{nextSourceId}", nextSourceId, 18, 30000, durationMs, DangerColour.V4);
                drawn++;
            }
        }
    }

    [ScriptMethod(name: "圣光枪",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50160)$"])]
    public void 圣光枪(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!ParseObjectId(@event["TargetId"], out var targetId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, sourceId, targetId, 6, 50, durationMs, DangerColour.V4);
    }

    #endregion

    // ============================================================
    // ---- 2.5 小怪 ----
    // ============================================================

    // ============================================================
    // ---- BOSS 3  普罗玛西亚 ----
    // ============================================================

    #region 普罗玛西亚

    [ScriptMethod(name: "爆炸",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50320)$"])]
    public void 爆炸(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawCircleOnSourceOrTarget(accessory, sourceId, 16, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "虚幻之环",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50321)$"])]
    public void 虚幻之环(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        //if (!TryGetDurationMs(@event, out int durationMs)) return; 读条这么短 实际那么长

        DrawCircleOnSourceOrTarget(accessory, sourceId, 13, 12400, DangerColour.V4);
    }

    [ScriptMethod(name: "黄昏乐土_黄泉灶食",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50331)$"])]
    public void 黄昏乐土_黄泉灶食(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var targetId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawDonut(accessory, targetId, 8, 50, durationMs, DangerColour.V4);
    }


    [ScriptMethod(name: "空虚思考之物_虚无之风",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50354)$"])]
    public void 空虚思考之物_虚无之风(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var targetId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, targetId, 6, 16, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "空虚思考之物_极光帷幕",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50355)$"])]
    public void 空虚思考之物_极光帷幕(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var targetId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, targetId, 7, 7, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "记忆容器_虚空之种",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50349)$"])]
    public void 记忆容器_虚空之种(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;


        if (!TryGetSourcePosition(@event, out var mobPos)) return;
        if (!TryGetMyPosition(accessory, out var playerPos)) return;

        float dist = Vector3.Distance(new Vector3(mobPos.X, 0, mobPos.Z),
            new Vector3(playerPos.X, 0, playerPos.Z));
        if (dist > 10) return;

        GuideKnockback(accessory, sourceId, mobPos, durationMs, 10.0f, DangerColour.V4, SafeColour.V4);
    }

    [ScriptMethod(name: "回天挽日",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50329)$"])]
    public void 回天挽日(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, sourceId, 50, 50, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "黄泉灶食",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50332)$"])]
    public void 黄泉灶食(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, sourceId, 5, 50, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "诱引_踩塔",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50335)$"])]
    public void 诱引_踩塔(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawCircleOnSourceOrTarget(accessory, sourceId, 4, durationMs, SafeColour.V4);
    }

    [ScriptMethod(name: "诱引_危险",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50565)$"])]
    public void 诱引_危险(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawCircleOnSourceOrTarget(accessory, sourceId, 8, durationMs, DangerColour.V4);
    }

    #endregion

    // ============================================================
    // ---- BOSS 4_1  神龙 ----
    // ============================================================

    #region 神龙

    [ScriptMethod(name: "宇宙吐息or宇宙甩尾",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(49107|49110)$"])]
    public void 宇宙吐息or宇宙甩尾(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, sourceId, 70, 50, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "黄昏星云",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(49113|49114)"])]
    public void 黄昏星云(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        if (黄昏披光暗_状态 == "5352")
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("黄色BUFF → 去上层！", durationMs, true);
        }
        else if (黄昏披光暗_状态 == "5353")
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("紫色BUFF → 去下层！", durationMs, true);
        }
    }

    [ScriptMethod(name: "灾变涡旋", eventType: EventTypeEnum.TargetIcon, eventCondition: ["Id:regex:^02A(8|9|A|B)$"])]
    public void 灾变涡旋(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var targetId)) return;
        if (targetId != accessory.Data.Me) return;

        switch (@event["Id"])
        {
            case "02A8":
                if (EnableTextPrompts) accessory.Method.TextInfo($"背对BOSS", duration: 6300, true);
                break;
            case "02A9":
                if (EnableTextPrompts) accessory.Method.TextInfo($"面对BOSS", duration: 6300, true);
                break;
            case "02AA":
                if (EnableTextPrompts) accessory.Method.TextInfo($"停止行动", duration: 6300, true);
                break;
            case "02AB":
                if (EnableTextPrompts) accessory.Method.TextInfo($"保持移动", duration: 6300, true);
                break;
        }
    }

    #endregion

    // ============================================================
    // ---- BOSS 4_2  虚无之王 ----
    // ============================================================

    #region 虚无之王

    [ScriptMethod(name: "天空轨迹",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(49140)$"])]
    public void 天空轨迹(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawCircleOnSourceOrTarget(accessory, sourceId, 2, durationMs, SafeColour.V4);
    }


    [ScriptMethod(name: "左侧交错剑",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(49154|49156)$"])]
    public void 左侧交错剑(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, sourceId, @event["ActionId"] == "49154" ? 30 : 36,
            @event["ActionId"] == "49154" ? 60 : 70, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "右侧交错剑",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(49153|49155)$"])]
    public void 右侧交错剑(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, sourceId, @event["ActionId"] == "49153" ? 30 : 36,
            @event["ActionId"] == "49153" ? 60 : 70, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "双重吐息", eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(49159|49160)$"])]
    public void 双重吐息(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetBossTransform(@event, out var bossPos, out var bossFacing, out int durationMs))
            return;
        if (@event["ActionId"] == "49159")
        {
            DrawDonut(accessory, sourceId, 20, 60, durationMs, DangerColour.V4);
        }
        else
        {
            DrawDangerFan(accessory, bossPos, bossFacing, 90, 35, durationMs, DangerColour.V4);
        }
    }

    [ScriptMethod(name: "灾变之刃", eventType: EventTypeEnum.StartCasting, eventCondition: ["ActionId:regex:^49162$"])]
    public void 灾变之刃(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetBossTransform(@event, out var bossPos, out var bossFacing, out int durationMs))
            return;

        DrawDangerFan(accessory, bossPos, bossFacing, 45, 60, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "宇宙烈焰",
        eventType: EventTypeEnum.ActionEffect,
        eventCondition: ["ActionId:regex:(49168)$"])] 
    public void 宇宙烈焰(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;

        DrawSteel(accessory, "", sourceId, 6, 1000, 0, DangerColour.V4);
    }

    #endregion

// ============================================================
// 数据提取辅助方法
// ============================================================
    private static bool TryGetSourcePosition(Event @event, out Vector3 pos)
    {
        pos = Vector3.Zero;
        try
        {
            pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>提取读条时间(毫秒), 失败返回 false</summary>
    private static bool TryGetDurationMs(Event @event, out int durationMs)
    {
        durationMs = 0;
        try
        {
            durationMs = int.Parse(@event["DurationMilliseconds"]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>提取 EffectPosition, 失败返回 false</summary>
    /// <summary>获取玩家当前坐标, 失败返回 false</summary>
    private static bool TryGetMyPosition(ScriptAccessory a, out Vector3 pos)
    {
        pos = Vector3.Zero;
        try
        {
            pos = a.Data.MyObject.Position;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool TryGetEffectPosition(Event @event, out Vector3 pos)
    {
        pos = Vector3.Zero;
        try
        {
            pos = JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>提取 Boss 位置+朝向+读条时间, 全部成功才返回 true</summary>
    private static bool TryGetBossTransform(Event @event, out Vector3 pos, out float facing, out int durationMs)
    {
        pos = Vector3.Zero;
        facing = 0;
        durationMs = 0;
        try
        {
            pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
        }
        catch
        {
            return false;
        }

        try
        {
            facing = float.Parse(@event["SourceRotation"]);
        }
        catch
        {
            return false;
        }

        try
        {
            durationMs = int.Parse(@event["DurationMilliseconds"]);
        }
        catch
        {
            return false;
        }

        return true;
    }

    /// <summary>提取 Boss 位置+朝向, 全部成功才返回 true</summary>
    private static bool TryGetBossTransform(Event @event, out Vector3 pos, out float facing)
    {
        pos = Vector3.Zero;
        facing = 0;
        try
        {
            pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]);
        }
        catch
        {
            return false;
        }

        try
        {
            facing = float.Parse(@event["SourceRotation"]);
        }
        catch
        {
            return false;
        }

        return true;
    }
// ============================================================
// 绘图辅助方法
// ============================================================

    /// <summary>画危险扇形 (Boss面前, Position定位)</summary>
    private static void DrawDangerFan(ScriptAccessory a, Vector3 pos, float facing, float angle, float radius,
        int durationMs, Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Position = pos;
        dp.Rotation = facing;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(angle);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    /// <summary>画危险扇形 (绑Boss身上, Owner自动跟)</summary>
    private static void DrawDangerFan(ScriptAccessory a, ulong ownerId, float angle, float radius, int durationMs,
        Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(angle);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    /// <summary>画安全扇形 (Boss背后, Position定位)</summary>
    private static void DrawSafeFan(ScriptAccessory a, Vector3 pos, float facing, float dangerAngle, float radius,
        int durationMs, Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Position = pos;
        dp.Rotation = facing + float.Pi;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(360 - dangerAngle);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    /// <summary>画安全扇形 (绑Boss身上, Owner自动跟)</summary>
    private static void DrawSafeFan(ScriptAccessory a, ulong ownerId, float dangerAngle, float radius, int durationMs,
        Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Rotation = float.Pi;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(360 - dangerAngle);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    /// <summary>画三角 (实体面前, 角度×半径, 底层Fan实现)</summary>
    private static void DrawTriangle(ScriptAccessory a, ulong ownerId, float angle, float radius, int durationMs,
        Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Rotation = 0;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(angle);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    /// <summary>画三角 (实体指定角度偏移, rotation=弧度)</summary>
    private static void DrawTriangle(ScriptAccessory a, ulong ownerId, float angle, float radius, float rotation,
        int durationMs, Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Rotation = rotation;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(angle);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    /// <summary>画三角 (实体背后)</summary>
    private static void DrawTriangleBehind(ScriptAccessory a, ulong ownerId, float angle, float radius, int durationMs,
        Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Rotation = float.Pi;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(angle);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    /// <summary>画矩形绑在实体身上</summary>
    private static void DrawRectOnOwner(ScriptAccessory a, ulong ownerId, float width, float length, int durationMs,
        Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Scale = new(width, length);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
    }

    /// <summary>画矩形绑在实体身上</summary>
    private static void DrawRectOnOwner(ScriptAccessory a, ulong ownerId, ulong targetId, float width, float length,
        int durationMs,
        Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.TargetObject = targetId;
        dp.Scale = new(width, length);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
    }

    /// <summary>画矩形绑在实体身上 (带延迟)</summary>
    private static void DrawRectOnOwnerWithDelay(ScriptAccessory a, ulong ownerId, float width, float length,
        int durationMs,
        int delay, Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Scale = new(width, length);
        dp.DestoryAt = durationMs;
        dp.Delay = delay;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
    }

    /// <summary>画圆绑在目标身上</summary>
    private static void DrawCircleOnSourceOrTarget(ScriptAccessory a, ulong sourceIdortargetId, float radius,
        int durationMs,
        Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = sourceIdortargetId;
        dp.Scale = new(radius);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
    }

    /// <summary>画圆在固定位置</summary>
    private static void DrawCircleAt(ScriptAccessory a, Vector3 pos, float radius, int durationMs, Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Position = pos;
        dp.Scale = new(radius);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
    }

    /// <summary>画圆在固定位置</summary>
    /// <summary>画钢铁 (Boss脚下圆形危险区)</summary>
    private static void DrawSteel(ScriptAccessory a, Vector3 pos, float radius, int durationMs, int delay,
        Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Position = pos;
        dp.Scale = new(radius);
        dp.DestoryAt = durationMs;
        dp.Delay = delay;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
    }

    /// <summary>画钢铁 (绑Boss身上, 跟人动)</summary>
    private static void DrawSteel(ScriptAccessory a, String name, ulong ownerId, float radius, int durationMs,
        int delay, Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Name = name;
        dp.Owner = ownerId;
        dp.Scale = new(radius);
        dp.DestoryAt = durationMs;
        dp.Delay = delay;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
    }

    /// <summary>画月环 (Boss脚下, 內圈安全外圈危险)</summary>
    private static void DrawDonut(ScriptAccessory a, Vector3 pos, float innerRadius, float outerRadius, int durationMs,
        Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Position = pos;
        dp.Scale = new(outerRadius);
        dp.InnerScale = new(innerRadius);
        dp.Radian = float.Pi * 2;
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
    }

    /// <summary>画月环 (绑Boss身上)</summary>
    private static void DrawDonut(ScriptAccessory a, ulong ownerId, float innerRadius, float outerRadius,
        int durationMs, Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Scale = new(outerRadius);
        dp.InnerScale = new(innerRadius);
        dp.Radian = float.Pi * 2;
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Donut, dp);
    }

    /// <summary>自己指向目标的指路箭头</summary>
    private static void GuideSelfToTarget(ScriptAccessory a, ulong targetId, int durationMs, Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = a.Data.Me;
        dp.TargetObject = targetId;
        dp.Scale = new(2);
        dp.ScaleMode |= ScaleMode.YByDistance;
        dp.Color = color;
        dp.DestoryAt = durationMs;
        a.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
    }

    /// <summary>击退指路: 红线(Boss→玩家) + 绿线(玩家→远离Boss)</summary>
    private static void GuideKnockback(ScriptAccessory a, ulong sourceId, Vector3 fromPos, int durationMs, float length,
        Vector4 dangerColor, Vector4 safeColor)
    {
        // 红线: Boss → 玩家 (Default模式下 Owner+TargetObject 自动跟人)
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = sourceId;
        dp.TargetObject = a.Data.Me;
        dp.ScaleMode |= ScaleMode.YByDistance;
        dp.Scale = new(1.5f);
        dp.DestoryAt = durationMs;
        dp.Color = dangerColor;
        a.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);

        // 绿线: 玩家 → 远离Boss
        dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = a.Data.Me;
        dp.TargetPosition = fromPos;
        dp.Rotation = float.Pi;
        dp.Scale = new(2, length);
        dp.DestoryAt = durationMs;
        dp.Color = safeColor;
        a.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
    }

// ============================================================
// 基础辅助方法
// ============================================================

    /// <summary>调试日志, 仅在开发者模式启用时输出到 Dalamud 日志</summary>
    private void DebugLog(ScriptAccessory a, string msg)
    {
        if (EnableDeveloperMode)
            a.Log.Debug(msg);
    }

    /// <summary>度数转弧度</summary>
    private static float ConvertDegree(float degree)
        => degree * float.Pi / 180f;

    /// <summary>解析十六进制 ObjectId 为 ulong</summary>
    private static bool ParseObjectId(string? idStr, out ulong id)
    {
        id = 0;
        if (string.IsNullOrEmpty(idStr)) return false;
        try
        {
            id = ulong.Parse(idStr.Replace("0x", ""),
                System.Globalization.NumberStyles.HexNumber);
            return true;
        }
        catch
        {
            return false;
        }
    }
}