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
    version: "0.0.0.3",
    author: "LRXR",
    note: "初版已完成")]
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

    // ---- BOSS 1 装甲之眼 ----

    [ScriptMethod(name: "石化光束",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50177|50178)$"])]
    public void 石化光束(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetBossTransform(@event, out var bossPos, out var bossFacing, out int durationMs))
            return;

        DrawDangerFan(accessory, bossPos, bossFacing, 100, 100, durationMs);
        DrawSafeFan(accessory, bossPos, bossFacing, 100, 100, durationMs);
    }

    [ScriptMethod(name: "点名分摊",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48901|48887|48930)$"])]
    public void 点名分摊(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["TargetId"], out var targetId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawCircleOnTarget(accessory, targetId, 6, durationMs, accessory.Data.DefaultSafeColor);
        GuideAllToTarget(accessory, targetId, durationMs);
    }

    // ---- BOSS 2 乔尔特 ----

    [ScriptMethod(name: "肉蛋",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48869|48876|48868|50313)$"])]
    public void 肉蛋(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        float width = 16;
        float length = @event["ActionId"] == "48876" ? 90 : 60;

        DrawRectOnOwner(accessory, sourceId, width, length, durationMs, accessory.Data.DefaultDangerColor);
    }

    [ScriptMethod(name: "肉压杀",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:48878"])]
    public void 肉压杀(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetDurationMs(@event, out int durationMs)) return;

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

        DrawDangerFan(accessory, sourceId, 120, 100, durationMs);
        DrawSafeFan(accessory, sourceId, 120, 100, durationMs);
    }

    [ScriptMethod(name: "肉压杀_塔",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48882)$"])]
    public void 肉压杀_塔(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetEffectPosition(@event, out var towerPos)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawCircleAt(accessory, towerPos, 4, durationMs, accessory.Data.DefaultSafeColor);
    }

    // ---- BOSS 3 ----

    [ScriptMethod(name: "虚无黑暗",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(50313)$"])]
    public void 虚无黑暗(Event @event, ScriptAccessory accessory)
    {
        if (!ParseObjectId(@event["SourceId"], out var sourceId)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawRectOnOwner(accessory, sourceId, 100, 100, durationMs, accessory.Data.DefaultDangerColor);
    }

    [ScriptMethod(name: "废料光环",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48935|48940)$"])]
    public void 废料光环(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetEffectPosition(@event, out var pos)) return;
        if (!TryGetDurationMs(@event, out int durationMs)) return;

        DrawCircleAt(accessory, pos, 7, durationMs, accessory.Data.DefaultDangerColor);
    }

    [ScriptMethod(name: "废料瘴气",
        eventType: EventTypeEnum.StartCasting,
        eventCondition: ["ActionId:regex:^(48937|48943)$"])]
    public void 废料瘴气(Event @event, ScriptAccessory accessory)
    {
        if (!TryGetBossTransform(@event, out var bossPos, out var bossFacing, out int durationMs))
            return;

        DrawDangerFan(accessory, bossPos, bossFacing, 30, 100, durationMs);
    }

    // ============================================================
    // 数据提取辅助方法 (从事件中取数据)
    // ============================================================

    /// <summary>提取读条时间, 失败返回 false</summary>
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

    /// <summary>提取 Boss 位置 + 朝向 + 读条时间, 三项都成功才返回 true</summary>
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
    // 绘图辅助方法 (封装常见的画图组合)
    // ============================================================

    /// <summary>画危险扇形 (Boss 面前)</summary>
    private static void DrawDangerFan(ScriptAccessory a, Vector3 pos, float facing, float angle, float radius, int durationMs)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Position = pos;
        dp.Rotation = facing;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(angle);
        dp.DestoryAt = durationMs;
        dp.Color = a.Data.DefaultDangerColor;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    /// <summary>画危险扇形 (绑在 Boss 身上, 面向自动对)</summary>
    private static void DrawDangerFan(ScriptAccessory a, ulong ownerId, float angle, float radius, int durationMs)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(angle);
        dp.DestoryAt = durationMs;
        dp.Color = a.Data.DefaultDangerColor;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    /// <summary>画安全扇形 (Boss 背后)</summary>
    private static void DrawSafeFan(ScriptAccessory a, Vector3 pos, float facing, float dangerAngle, float radius, int durationMs)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Position = pos;
        dp.Rotation = facing + float.Pi;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(360 - dangerAngle);
        dp.DestoryAt = durationMs;
        dp.Color = a.Data.DefaultSafeColor;
        a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Fan, dp);
    }

    /// <summary>画安全扇形 (绑在 Boss 身上)</summary>
    private static void DrawSafeFan(ScriptAccessory a, ulong ownerId, float dangerAngle, float radius, int durationMs)
    {
        var dp = a.Data.GetDefaultDrawProperties();
        dp.Owner = ownerId;
        dp.Rotation = float.Pi;
        dp.Scale = new(radius);
        dp.Radian = ConvertDegree(360 - dangerAngle);
        dp.DestoryAt = durationMs;
        dp.Color = a.Data.DefaultSafeColor;
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

    /// <summary>画圆绑在目标身上</summary>
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

    /// <summary>四人指路箭头, 全部指向同一个目标</summary>
    private static void GuideAllToTarget(ScriptAccessory a, ulong targetId, int durationMs)
    {
        for (int i = 0; i < 4; i++)
        {
            var dp = a.Data.GetDefaultDrawProperties();
            dp.Owner = a.Data.PartyList[i];
            dp.TargetObject = targetId;
            dp.Scale = new(2);
            dp.ScaleMode |= ScaleMode.YByDistance;
            dp.Color = a.Data.DefaultSafeColor;
            dp.DestoryAt = durationMs;
            a.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
        }
    }

    // ============================================================
    // 基础辅助方法
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
