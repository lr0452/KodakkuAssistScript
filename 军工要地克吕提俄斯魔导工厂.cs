using System;
using System.Numerics;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Module.Draw;
using KodakkuAssist.Script;
using KodakkuAssist.Module.GameOperate;
using Dalamud.Utility.Numerics;
using Newtonsoft.Json;

namespace MyScripts.FourMan;

[ScriptType(
    name: "军工要地克吕提俄斯魔导工厂",
    territorys: [1345],
    guid: "dbf88c7b-f119-423c-954c-26aa86e58704",
    version: "0.0.0.1",
    author: "LRXR",
    note: "初版测试")]
public class 军工要地克吕提俄斯魔导工厂
{
    // ============================================================
    // 用户设置
    // ============================================================
    [UserSetting("启用文字提示")]
    public bool Enable_Text_Prompts { get; set; } = true;

    [UserSetting("启用开发者调试模式")]
    public bool Enable_Developer_Mode { get; set; } = false;

    [UserSetting("方向指路的颜色")]
    public ScriptColor Colour_Of_Guidance { get; set; } = new() { V4 = new Vector4(0, 1, 0, 1) };

    [UserSetting("危险攻击的颜色")]
    public ScriptColor Colour_Of_Danger { get; set; } = new() { V4 = new Vector4(1, 0, 0, 1) };

    // ============================================================
    // 初始化
    // ============================================================
    public void Init(ScriptAccessory accessory)
    {
        accessory.Method.RemoveDraw(".*");
    }

    
    
    
    [ScriptMethod(
        name: "AOE",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48896|50408)$"])]
    public void AOE(Event @event, ScriptAccessory accessory)
    {
        int durationMs;
        try
        {
            durationMs = JsonConvert.DeserializeObject<int>(
                @event["DurationMilliseconds"]);
        }
        catch { return; }

        accessory.Method.TextInfo("AOE", durationMs);
    }
    
    // BOSS 1 装甲之眼

    [ScriptMethod(
        name: "石化光束",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50177|50178)$"])]
    public void 石化光束(Event @event, ScriptAccessory accessory)
    {
        // 提取 Boss 位置
        Vector3 bossPos;
        try { bossPos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]); }
        catch { return; }

        // 提取 Boss 朝向
        double bossRotation;
        try { bossRotation = JsonConvert.DeserializeObject<double>(@event["SourceRotation"]); }
        catch { return; }

        // 提取读条时间
        int durationMs;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return; }

        float bossFacing = (float)bossRotation;

        // 危险区 — 红色扇形 (Boss 面前 100°)
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Position = bossPos;
        dp.Rotation = bossFacing;
        dp.Scale = new(100);
        dp.Radian = ConvertDegree(100);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultDangerColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

        // 安全区 — 绿色扇形 (Boss 背后 260°)
        dp = accessory.Data.GetDefaultDrawProperties();
        dp.Position = bossPos;
        dp.Rotation = bossFacing + float.Pi;
        dp.Scale = new(100);
        dp.Radian = ConvertDegree(260);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultSafeColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    
    [ScriptMethod(
        name: "点名分摊",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:48901"])]
    public void 点名分摊(Event @event, ScriptAccessory accessory)
    {
        
        Vector3 stackPos;
        try { stackPos = JsonConvert.DeserializeObject<Vector3>(@event["TargetPosition"]); }
        catch { return; }

        int durationMs;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return; }

        // 分摊圈 — 被点名玩家脚下 (绿色)
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Position = stackPos;
        dp.Scale = new(6);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultSafeColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

        // 四人指路 — 每人一个箭头指向分摊点
        for (int i = 0; i < 4; i++)
        {
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Owner = accessory.Data.PartyList[i];
            dp.TargetPosition = stackPos;
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = durationMs;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
    }
    
    // BOSS 2 乔尔特

    [ScriptMethod(
        name: "肉蛋",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:48869"])]
    public void 肉蛋(Event @event, ScriptAccessory accessory)
    {
        // 提取 Boss 位置
        Vector3 bossPos;
        try { bossPos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]); }
        catch { return; }

        // 提取 Boss 朝向
        double bossRotation;
        try { bossRotation = JsonConvert.DeserializeObject<double>(@event["SourceRotation"]); }
        catch { return; }

        // 提取读条时间
        int durationMs;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return; }
        
        float bossFacing = (float)bossRotation; 

        // 危险区 — 红色矩形 
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Position = bossPos;
        dp.Rotation = bossFacing;
        dp.Scale = new(10,60);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultDangerColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    // ============================================================
    // 辅助方法
    // ============================================================
    private static float ConvertDegree(float degree)
        => degree * float.Pi / 180f;

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
        catch { return false; }
    }
}
