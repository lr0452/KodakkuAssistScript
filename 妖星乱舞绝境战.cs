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
    name: "妖星乱舞绝境战",
    territorys: [],
    guid: "17d17154-54d8-4195-87af-b9a07f48ec19",
    version: "0.0.0.1",
    author: "LRXR",
    note: "新建文件夹")]
public class 妖星乱舞绝境战
{
    // ============================================================
    // 用户设置
    // ============================================================
    [UserSetting("启用文字提示")] public bool EnableTextPrompts { get; set; } = true;

    [UserSetting("启用开发者调试模式")] public bool EnableDeveloperMode { get; set; } = false;

    [UserSetting("安全的颜色")] public ScriptColor SafeColour { get; set; } = new() { V4 = new Vector4(0, 1, 0, 1) };

    [UserSetting("危险的颜色")] public ScriptColor DangerColour { get; set; } = new() { V4 = new Vector4(1, 0, 0, 1) };
    
    // ============================================================
    // 初始化
    // ============================================================
    public void Init(ScriptAccessory accessory)
    {
        accessory.Method.RemoveDraw(".*");
    }
}