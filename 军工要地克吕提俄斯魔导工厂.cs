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
    version: "0.0.0.5",
    author: "LRXR",
    note: "0.0.0.5 \n" +
          "修复部分BUG")]
public class 军工要地克吕提俄斯魔导工厂
{
    // ============================================================
    // 用户设置
    // ============================================================
    [UserSetting("启用文字提示")]
    public bool EnableTextPrompts { get; set; } = true;

    [UserSetting("启用开发者调试模式")]
    public bool EnableDeveloperMode { get; set; } = false;

    [UserSetting("安全的颜色")]
    public ScriptColor SafeColour { get; set; } = new() { V4 = new Vector4(0, 1, 0, 1) };

    [UserSetting("危险的颜色")]
    public ScriptColor DangerColour { get; set; } = new() { V4 = new Vector4(1, 0, 0, 1) };

    // ============================================================
    // 初始化
    // ============================================================
    public void Init(ScriptAccessory accessory)
    {
        accessory.Method.RemoveDraw(".*");
        DebugLog(accessory, "脚本已初始化");
    }

    // ============================================================
    // 通用技能方法
    // ============================================================

    [ScriptMethod(name: "屏幕提示",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48896|50408|48920|48931)$"])]
    public void 屏幕提示(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        if (@event["ActionId"] == "48931")
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("持续移动到这个提示结束！", durationMs);
        }
        else
        {
            if (EnableTextPrompts) accessory.Method.TextInfo("AOE", durationMs);
        }
    }

    [ScriptMethod(name: "点名分摊",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48901|48887|48930)$"])]
    public void 点名分摊(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var targetId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawCircleOnTarget(accessory, targetId, 6, durationMs, SafeColour.V4);
        GuideAllToTarget(accessory, targetId, durationMs, SafeColour.V4);
    }


    // ---- BOSS 1 装甲之眼 ----

    [ScriptMethod(name: "石化光束",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50177|50178)$"])]
    public void 石化光束(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetBossTransform(@event, out var bossPos, out var bossFacing, out int durationMs))
            return;

        DrawDangerFan(accessory, bossPos, bossFacing, 100, 100, durationMs, DangerColour.V4);
        DrawSafeFan(accessory, bossPos, bossFacing, 100, 100, durationMs, SafeColour.V4);
    }

    // ---- BOSS 2 乔尔特 ----

    [ScriptMethod(name: "肉弹",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48868|48869|48870|48871|48876|50313)$"])]
    public void 肉弹(Event @event, ScriptAccessory accessory) 
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        float width = 16;
        float length = @event["ActionId"] == "48876" ? 90 : 60;

        DrawRectOnOwner(accessory, sourceId, width, length, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "肉压杀",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:48878"])]
    public void 肉压杀(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        GuideKnockback(accessory, sourceId, @event.SourcePosition, durationMs, 8.0f,DangerColour.V4, SafeColour.V4);

        if (EnableTextPrompts)
            accessory.Method.TextInfo("击退", durationMs);
    }

    [ScriptMethod(name: "呕吐",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50361)$"])]
    public void 呕吐(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawDangerFan(accessory, sourceId, 120, 100, durationMs, DangerColour.V4);
        DrawSafeFan(accessory, sourceId, 120, 100, durationMs, SafeColour.V4);
    }

    [ScriptMethod(name: "肉压杀_塔",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48882)$"])]
    public void 肉压杀_塔(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetEffectPosition(@event, out var towerPos)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawCircleAt(accessory, towerPos, 4, durationMs, SafeColour.V4);
    }

    // ---- BOSS 3 玛帕斯 ----

    [ScriptMethod(name: "虚无黑暗",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50313)$"])]
    public void 虚无黑暗(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, sourceId, 100, 100, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "废料光环",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48935|48940)$"])]
    public void 废料光环(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetEffectPosition(@event, out var pos)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawCircleAt(accessory, pos, 7, durationMs, DangerColour.V4);
    }

    [ScriptMethod(name: "废料瘴气",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48937|48943)$"])]
    public void 废料瘴气(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetBossTransform(@event, out var bossPos, out var bossFacing, out int durationMs))
            return;

        DrawDangerFan(accessory, bossPos, bossFacing, 30, 100, durationMs, DangerColour.V4);
    }

    // ============================================================
    // 数据提取辅助方法
    // ============================================================

    /// <summary>提取读条时间(毫秒), 失败返回 false</summary>
    private static bool TryGetDurationMs(Event @event, out int durationMs)
    {
        durationMs = 0;
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); return true; }
        catch { return false; }
    }

    /// <summary>提取 EffectPosition, 失败返回 false</summary>
    private static bool TryGetEffectPosition(Event @event, out Vector3 pos)
    {
        pos = Vector3.Zero;
        try { pos = JsonConvert.DeserializeObject<Vector3>(@event["EffectPosition"]); return true; }
        catch { return false; }
    }

    /// <summary>提取 Boss 位置+朝向+读条时间, 全部成功才返回 true</summary>
    private static bool TryGetBossTransform(Event @event, out Vector3 pos, out float facing, out int durationMs)
    {
        pos = Vector3.Zero;
        facing = 0;
        durationMs = 0;
        try { pos = JsonConvert.DeserializeObject<Vector3>(@event["SourcePosition"]); }
        catch { return false; }
        try { var r = JsonConvert.DeserializeObject<double>(@event["SourceRotation"]); facing = (float)r; }
        catch { return false; }
        try { durationMs = JsonConvert.DeserializeObject<int>(@event["DurationMilliseconds"]); }
        catch { return false; }
        return true;
    }

    // ============================================================
    // 绘图辅助方法
    // ============================================================

    /// <summary>画危险扇形 (Boss面前, Position定位)</summary>
    private static void DrawDangerFan(ScriptAccessory a, Vector3 pos, float facing, float angle, float radius, int durationMs, Vector4 color)
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
    private static void DrawDangerFan(ScriptAccessory a, ulong ownerId, float angle, float radius, int durationMs, Vector4 color)
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
    private static void DrawSafeFan(ScriptAccessory a, Vector3 pos, float facing, float dangerAngle, float radius, int durationMs, Vector4 color)
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
    private static void DrawSafeFan(ScriptAccessory a, ulong ownerId, float dangerAngle, float radius, int durationMs, Vector4 color)
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

    /// <summary>画矩形绑在实体身上</summary>
    private static void DrawRectOnOwner(ScriptAccessory a, ulong ownerId, float width, float length, int durationMs, Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Scale = new(width, length);
        dp.DestoryAt = durationMs;
        dp.Color = color;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Rect, dp);
    }

    /// <summary>画圆绑在目标身上(跟人动)</summary>
    private static void DrawCircleOnTarget(ScriptAccessory a, ulong targetId, float radius, int durationMs, Vector4 color)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = targetId;
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

    /// <summary>四人指路箭头全部指向同一个目标</summary>
    private static void GuideAllToTarget(ScriptAccessory a, ulong targetId, int durationMs, Vector4 color)
    {
        for (int i = 0; i < 4; i++)
        {
            var dp = a.Data.GetDefaultDrawProperties();
            dp.Owner = a.Data.PartyList[i];
            dp.TargetObject = targetId;
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = color;
            dp.DestoryAt = durationMs;
            a.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
    }

    /// <summary>击退指路: 红线(Boss→玩家) + 绿线(玩家→远离Boss)</summary>
    private static void GuideKnockback(ScriptAccessory a, ulong sourceId, Vector3 fromPos, int durationMs, float length, Vector4 dangerColor, Vector4 safeColor)
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
        catch { return false; }
    }
}
