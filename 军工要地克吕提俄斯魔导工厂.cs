using System.Numerics;
using KodakkuAssist.Module.GameEvent;
using KodakkuAssist.Module.Draw;
using KodakkuAssist.Script;
using Newtonsoft.Json;

namespace LRXR.Workspace.MyScripts;

[ScriptType(
    name: "军工要地克吕提俄斯魔导工厂",
    territorys: [1345],
    guid: "dbf88c7b-f119-423c-954c-26aa86e58704",
    version: "0.0.0.2",
    author: "LRXR",
    note: "BOSS 1/2画图 BOSS3暂时未画")]
public class 军工要地克吕提俄斯魔导工厂
{
    // ============================================================
    // 用户设置
    // ============================================================
    [UserSetting("启用文字提示")]
    public bool EnableTextPrompts { get; set; } = true;

    [UserSetting("启用开发者调试模式")]
    public bool EnableDeveloperMode { get; set; } = false;

    [UserSetting("方向指路的颜色")]
    public ScriptColor ColourOfGuidance { get; set; } = new() { V4 = new Vector4(0, 1, 0, 1) };

    [UserSetting("危险攻击的颜色")]
    public ScriptColor ColourOfDanger { get; set; } = new() { V4 = new Vector4(1, 0, 0, 1) };

    // ============================================================
    // 初始化
    // ============================================================
    public void Init(ScriptAccessory accessory)
    {
        accessory.Method.RemoveDraw(".*");
    }

    
    
    
    [ScriptMethod(
        name: "屏幕提示",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48896|50408|48920|48931)$"])]
    public void 屏幕提示(Event @event, ScriptAccessory accessory)
    {
        int durationMs;
        try
        {
            durationMs = JsonConvert.DeserializeObject<int>(
                @event["DurationMilliseconds"]);
        }catch { return; }

        if (@event["ActionId"] == "48931")
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("持续移动到这个提示结束！", durationMs);
        }
        else
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("AOE", durationMs);
        }
       
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
        eventCondition: ["ActionId:regex:^(48901|48887|48930)$"])]
    public void 点名分摊(Event @event, ScriptAccessory accessory)
    {
        // 被点名玩家的 ID
        if (!ParseObjectId(@event["TargetId"], out var targetId))
            return;

        int durationMs;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return; }

        // 分摊圈 — 绑在被点名玩家身上, 人动圈跟着动
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Owner = targetId;
        dp.Scale = new(6);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultSafeColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

        // 四人指路 — 箭头指向被点名玩家, 人动箭头也跟着动
        for (int i = 0; i < 4; i++)
        {
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Owner = accessory.Data.PartyList[i];
            dp.TargetObject = targetId;
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
        eventCondition: ["ActionId:regex:^(48869|48876|48868|50313)$"])]
    public void 肉蛋(Event @event, ScriptAccessory accessory)
    {
        // 提取 Boss 的 ID (十六进制字符串 → 数字)
        if (!ParseObjectId(@event["SourceId"], out var sourceId))
            return;

        // 提取读条时间
        int durationMs;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return; }

        // 两个 ActionId 画的矩形大小不一样, 按 ID 区分宽和长
        float width, length;
        if (@event["ActionId"] == "48876")
        {
            width = 16;  
            length = 90;  
        }
        else  
        {
            width = 16;   
            length = 60;  
        }

        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Owner = sourceId;
        dp.Scale = new(width, length);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultDangerColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
    }

    [ScriptMethod(
        name: "肉压杀",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:48878"])]
    public void 肉压杀(Event @event, ScriptAccessory accessory)
    {
        /*if (!ParseObjectId(@event["SourceId"], out var sourceId))
            return;*/

        int durationMs;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return; }

        /*// 红线: Boss → 玩家 (击退来源)
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Owner = sourceId;
        dp.TargetObject = accessory.Data.Me;
        dp.ScaleMode |= ScaleMode.YByDistance;
        dp.Scale = new(1.5f);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultDangerColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);

        // 绿线: 玩家 → 远离Boss (大致的落地方向)
        dp = accessory.Data.GetDefaultDrawProperties();
        dp.Owner = accessory.Data.Me;
        dp.TargetObject = sourceId;
        dp.Rotation = float.Pi;
        dp.ScaleMode |= ScaleMode.YByDistance;
        dp.Scale = new(1.5f, 25);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultSafeColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);*/

        if (EnableTextPrompts)
            accessory.Method.TextInfo("击退", durationMs);
    }

    [ScriptMethod(
        name: "呕吐",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50361)$"])]
    public void 呕吐(Event @event, ScriptAccessory accessory)
    {
        // 提取 Boss 的 ID (十六进制字符串 → 数字)
        if (!ParseObjectId(@event["SourceId"], out var sourceId))
            return;

        // 提取读条时间
        int durationMs;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return; }
        
        // 危险区 — 红色扇形 (Boss 面前 124°)
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Owner = sourceId;
        dp.Scale = new(100);
        dp.Radian = ConvertDegree(124);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultDangerColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

        // 安全区 — 绿色扇形 (Boss 背后 236°)
        dp = accessory.Data.GetDefaultDrawProperties();
        dp.Scale = new(100);
        dp.Radian = ConvertDegree(236);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultSafeColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);

    }

    [ScriptMethod(
        name: "肉压杀_塔",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48882)$"])]
    public void 肉压杀_塔(Event @event, ScriptAccessory accessory)
    {
        Vector3 towerPos;
        try { towerPos = JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]); }
        catch { return; }

        int durationMs;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return; }

        // 塔的位置 — 绿色圆圈
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Position = towerPos;
        dp.Scale = new(4);                                
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultSafeColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);

        /*// 四人指路 — 每人一个箭头指向塔
        for (int i = 0; i < 4; i++)
        {
            dp = accessory.Data.GetDefaultDrawProperties();
            dp.Owner = accessory.Data.PartyList[i];
            dp.TargetPosition = towerPos;
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = accessory.Data.DefaultSafeColor;
            dp.DestoryAt = durationMs;
            accessory.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }*/
    }

    // BOSS 3
    [ScriptMethod(
        name: "虚无黑暗",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50313)$"])]
    public void 虚无黑暗(Event @event, ScriptAccessory accessory)
    {
        // 提取 Boss 的 ID (十六进制字符串 → 数字)
        if (!ParseObjectId(@event["SourceId"], out var sourceId))
            return;

        // 提取读条时间
        int durationMs;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return; }
        
        float    width = 100;   
        float    length = 100;  
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Owner = sourceId;
        dp.Scale = new(width, length);
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultDangerColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
    }
    [ScriptMethod(
        name: "废料光环",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48934|48940)$"])]
    public void 废料光环(Event @event, ScriptAccessory accessory)
    {
        Vector3 towerPos;
        try { towerPos = JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]); }
        catch { return; }

        int durationMs;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return; }

        // 危险圈
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Position = towerPos;
        dp.Scale = new(7);                                
        dp.DestoryAt = durationMs;
        dp.Color = accessory.Data.DefaultDangerColor;
        accessory.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Circle, dp);
        
    }
    [ScriptMethod(
        name: "废料瘴气",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48937|48943)$"])]
    public void 废料瘴气(Event @event, ScriptAccessory accessory)
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

        // 危险区 — 红色扇形 (Boss 面前 30°)
        var dp = accessory.Data.GetDefaultDrawProperties();
        dp.Position = bossPos;
        dp.Rotation = bossFacing;
        dp.Scale = new(100);
        dp.Radian = ConvertDegree(30);
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