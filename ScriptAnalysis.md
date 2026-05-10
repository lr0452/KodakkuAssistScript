# KodakkuAssist 画图脚本 学习与分析

> 脚本: `军工要地克吕提俄斯魔导工厂.cs`  
> 副本: 军工要地克吕提俄斯魔导工厂 (TerritoryId: 1345)  
> 版本: v0.0.0.4 | 作者: LRXR  
> 总行数: 370 行 (含 10 个技能方法 + 15 个辅助方法)

---

## 一、脚本整体架构

```
┌────────────────────────────────────────────┐
│  [ScriptType] 元数据                        │
│  ├── name, territorys, guid, version       │
├────────────────────────────────────────────┤
│  用户设置 (UserSetting)                     │
│  ├── bool:   文字提示 / 开发者模式           │
│  └── ScriptColor: 安全色 / 危险色           │
├────────────────────────────────────────────┤
│  Init() — 初始化                            │
├────────────────────────────────────────────┤
│  技能方法 (10个)                            │
│  └── 每个 = 数据提取 → 调用辅助方法 → 完成   │
├────────────────────────────────────────────┤
│  辅助方法 (15个)                            │
│  ├── 数据提取层 (3个): TryGet*              │
│  ├── 绘图封装层 (10个): Draw* / Guide*      │
│  └── 基础工具层 (2个): ConvertDegree /      │
│       ParseObjectId / DebugLog              │
└────────────────────────────────────────────┘
```

**三层辅助方法分离**是此脚本最核心的设计思想。每个 `[ScriptMethod]` 技能方法保持 2~6 行，逻辑全部下沉到辅助方法中。

---

## 二、文件头部详解

### 2.1 命名空间

```csharp
using System.Numerics;              // Vector2, Vector3, Vector4
using KodakkuAssist.Module.GameEvent; // Event 类型, EventTypeEnum
using KodakkuAssist.Module.Draw;      // DrawTypeEnum, DrawModeEnum, ScaleMode
using KodakkuAssist.Script;           // ScriptType, ScriptMethod, ScriptAccessory
using Newtonsoft.Json;                // JsonConvert.DeserializeObject<T>
```

**关键依赖说明：**

| 命名空间 | 为什么要它 |
|---|---|
| `KodakkuAssist.Script` | 提供 `[ScriptType]` 和 `[ScriptMethod]` 特性，可达鸭通过它们识别脚本 |
| `KodakkuAssist.Module.Draw` | 提供所有绘图枚举: `DrawTypeEnum`(形状)、`DrawModeEnum`(Default/Imgui)、`ScaleMode`(缩放) |
| `KodakkuAssist.Module.GameEvent` | 提供 `Event` 类型和 `EventTypeEnum`(事件触发类型) |
| `Newtonsoft.Json` | 事件数据(位置、朝向、时间)以 JSON 字符串存储，需要反序列化 |

> **注意**: 没有 `using Dalamud.Utility.Numerics;` — 说明 `float.Pi` 在可达鸭环境中是全局可用的，或者脚本没有使用 `float.Pi`(实际上 `ConvertDegree` 中用了 `float.Pi`。某些可达鸭版本将其作为全局 using 提供)。

### 2.2 [ScriptType] 特性

```csharp
[ScriptType(
    name: "军工要地克吕提俄斯魔导工厂",   // 在可达鸭脚本列表中显示的名称
    territorys: [1345],                    // 数组, 可填多个副本 ID
    guid: "dbf88c7b-f119-423c-954c-26aa86e58704", // 唯一标识符
    version: "0.0.0.4",                   // 版本号
    author: "LRXR",
    note: "修复部分BUG")]                  // 备注说明, 显示在脚本列表
```

**每个字段的作用：**

| 字段 | 必需 | 说明 |
|---|---|---|
| `name` | 是 | 脚本显示名称，支持中文 |
| `territorys` | 是 | 副本 ID 数组。`[1345]` 表示只在 1345 号副本生效。留空 `[]` = 全副本生效 |
| `guid` | 是 | 全局唯一标识，用 `[guid]::NewGuid()` 生成，不能与别的脚本重复 |
| `version` | 是 | 版本号字符串，可达鸭用它判断是否有更新 |
| `author` | 否 | 作者名 |
| `note` | 否 | 备注，显示在脚本列表中 |

### 2.3 用户设置 [UserSetting]

```csharp
[UserSetting("启用文字提示")]
public bool EnableTextPrompts { get; set; } = true;
```

- `[UserSetting]` 特性使该属性在可达鸭 UI 中显示为可调节选项
- `bool` 类型显示为**复选框**
- `ScriptColor` 类型显示为**颜色选择器**
- `enum` 类型显示为**下拉菜单**
- `= true` 是默认值

**颜色设置的特殊写法：**
```csharp
public ScriptColor SafeColour { get; set; } = new() { V4 = new Vector4(0, 1, 0, 1) };
//                                                        R    G  B  A
//                                                        红  绿 蓝 透明度
```

`ScriptColor` 是一个包装类型，其 `.V4` 属性返回 `Vector4`。画图时传 `SafeColour.V4` 而非直接传 `SafeColour`。

---

## 三、技能方法详解

### 3.1 方法结构模板

每个技能方法遵循统一的三段式结构：

```
① 数据提取 —— TryGet* 辅助方法, 失败则 return
② 画图调用 —— Draw* / Guide* 辅助方法
③ 可选文字 —— TextInfo 提示
```

以最简单的 **肉压杀_塔** 为例：

```csharp
[ScriptMethod(name: "肉压杀_塔",
    eventType: EventTypeEnum.StartCasting,       // ← Boss 开始读条时触发
    eventCondition: ["ActionId:regex:^(48882)$"])]  // ← 只响应 ActionId=48882
public void 肉压杀_塔(Event @event, ScriptAccessory accessory)
{
    // ① 提取塔的坐标 (从 EffectPosition)
    if (!TryGetEffectPosition(@event, out var towerPos)) return;
    // ① 提取读条时间
    if (!TryGetDurationMs(@event, out int durationMs)) return;

    // ② 在塔的位置画绿色圆圈
    DrawCircleAt(accessory, towerPos, 4, durationMs, SafeColour.V4);
    //                                  ↑半径  ↑显示时长      ↑用户设置的颜色
}
```

### 3.2 [ScriptMethod] 参数详解

```csharp
[ScriptMethod(
    name: "方法显示名称",           // 可达鸭 UI 中显示
    eventType: EventTypeEnum.XXX,   // 触发事件类型
    eventCondition: ["条件"],       // 过滤条件数组
    userControl: true/false,        // 用户能否在 UI 中开关此方法 (默认 true)
    suppress: 毫秒数)]              // 抑制重复触发的间隔 (单位 ms)
```

**事件条件格式：**

| 格式 | 示例 | 说明 |
|---|---|---|
| `ActionId:12345` | `["ActionId:48878"]` | 精确匹配单个技能 ID |
| `ActionId:regex:^(123\|456)$` | `["ActionId:regex:^(48868\|48869)$"]` | 正则匹配多个技能 ID |
| `StatusID:1578` | `["StatusID:4166"]` | 匹配 Buff/Debuff ID |
| `DataId:9020` | `["DataId:9020"]` | 匹配实体数据类型 ID |

### 3.3 各技能方法分析

#### 屏幕提示 (line 46-61)
- **事件**: 多个 ActionId 的 `StartCasting`
- **逻辑**: 根据 `ActionId` 分支，`48931` 显示特殊提示文字，其余显示通用 "AOE"
- **无画图**: 纯文字提示方法

#### 点名分摊 (line 63-73)
- **数据源**: `TargetId` (被点名玩家) + `DurationMilliseconds` (读条时间)
- **画图**: 分摊圈绑在被点名玩家身上 (`DrawCircleOnTarget`，用 `Owner`) + 四人指路箭头 (`GuideAllToTarget`)
- **关键选择**: 用 `Owner = targetId` 而非 `Position = stackPos`，使圈**跟随被点名玩家移动**

#### 石化光束 (line 78-88)
- **数据源**: `SourcePosition` + `SourceRotation` + `DurationMilliseconds`
- **画图**: 危险扇形 100° (Boss 面前) + 安全扇形 260° (Boss 背后)
- **定位方式**: Position 模式 (用 `TryGetBossTransform` 提取坐标和朝向)
- **半径 100**: 覆盖全场

#### 肉弹 (line 92-104)
- **数据源**: `SourceId` (Boss) + `DurationMilliseconds`
- **画图**: 矩形绑 Boss 身上 (`DrawRectOnOwner`)
- **条件分支**: `48876` 的矩形长度 = 90，其余 = 60
- **定位方式**: Owner 模式 (Boss 走矩形跟着走)

#### 肉压杀 (line 106-118)
- **数据源**: `SourceId` + `SourcePosition` + `DurationMilliseconds`
- **画图**: `GuideKnockback` — 双线击退指路
- **红线** (Default 模式): `Owner = sourceId, TargetObject = Me` — 从 Boss 到玩家，**长度自动 = 实时距离**
- **绿线** (Imgui 模式): `Owner = Me, TargetPosition = fromPos, Rotation = Pi` — 从玩家指向远离 Boss

#### 呕吐 (line 120-130)
- **数据源**: `SourceId` (Boss) + `DurationMilliseconds`
- **画图**: 危险扇形 120° + 安全扇形 240°
- **定位方式**: Owner 模式 (因为 Boss 在读条期间会转身，用 Owner 自动跟随)
- **与石化光束的区别**: 石化光束用 Position 模式(扇形画在固定坐标)，呕吐用 Owner 模式(扇形绑 Boss 身上)

#### 虚无黑暗 (line 145-154)
- 100×100 正方形绑 Boss 身上

#### 废料光环 (line 156-165)
- 在 EffectPosition 位置画危险圆圈（范围半径7）

#### 废料瘴气 (line 167-176)
- Boss 面前 30° 危险扇形 (Position 模式)

---

## 四、辅助方法三层架构

### 4.1 第一层: 数据提取 (3 个方法)

所有数据提取方法遵循 `Try*` 命名约定，返回 `bool`，通过 `out` 参数输出结果。失败返回 `false` 让调用方 `return` 提前退出。

#### TryGetDurationMs
```csharp
private static bool TryGetDurationMs(Event @event, out int durationMs)
```

- **输入**: `@event["DurationMilliseconds"]` — JSON 字符串，如 `"8200"`
- **输出**: `int` 毫秒数，如 `8200`
- **为什么需要反序列化**: 游戏事件中的数据以 JSON 格式编码。`DurationMilliseconds` 看起来是纯数字，但事件字典中的所有值都是 `string` 类型
- **使用频率**: 几乎每个技能方法都调用，是最常用的辅助方法

#### TryGetEffectPosition
```csharp
private static bool TryGetEffectPosition(Event @event, out Vector3 pos)
```

- **输入**: `@event["EffectPosition"]` — JSON 字符串，如 `"{\"X\":667.75,\"Y\":-15.03,\"Z\":-133.23}"`
- **输出**: `Vector3` 坐标
- **使用场景**: Boss 放塔/圈/地面 AOE 时，位置在 EffectPosition 里
- **注意**: 点名玩家的技能位置用 TargetPosition，不用 EffectPosition

#### TryGetBossTransform
```csharp
private static bool TryGetBossTransform(Event @event, out Vector3 pos, out float facing, out int durationMs)
```

- **一次性提取三个数据**: 位置(`SourcePosition`) + 朝向(`SourceRotation`) + 读条时间
- **为什么合并**: 需要 Boss 位置+朝向的场景总是同时需要读条时间，一次性提取减少重复代码
- **注意**: 朝向从 `double` 转为 `float` — `SourceRotation` 是 JSON 中的 double，画图 API 接受 float

### 4.2 第二层: 绘图封装 (10 个方法)

#### 设计原则

1. 所有方法都是 `private static` — 不依赖实例状态(颜色作为参数传入)
2. 通过 `Vector4 color` 参数接收颜色，不使用硬编码的 `DefaultDangerColor`
3. 统一的参数顺序: `ScriptAccessory a` → 定位信息 → 尺寸 → 时间 → 颜色

#### DrawDangerFan (两个重载)

| 重载 | 定位方式 | 使用场景 |
|---|---|---|
| `(Vector3 pos, float facing, ...)` | Position + Rotation | Boss 读条时原地不动 |
| `(ulong ownerId, ...)` | Owner | Boss 读条时会转身 |

```csharp
// 定位方式对比
dp.Position = pos;       // 固定在世界坐标, Boss 走了扇形留在原地
dp.Owner = ownerId;      // 绑在实体上, Boss 走扇形跟走, Boss 转扇形跟转
```

**Owner 模式的优势**: 不需要 `Rotation` 参数 — 扇形自动继承 Owner 的朝向。`DrawDangerFan(ownerId)` 重载中没有设置 `dp.Rotation`。

#### DrawSafeFan (两个重载)

与 `DrawDangerFan` 对应，但有两个关键区别：

```csharp
dp.Rotation = facing + float.Pi;          // 危险区对面 (+180°)
dp.Radian = ConvertDegree(360 - dangerAngle); // 360° - 危险角度 = 安全角度
```

**自动计算**: 调用方只需传 `dangerAngle`(危险区角度)，安全区自动 = `360 - dangerAngle`。

**Owner 重载**: `dp.Rotation = float.Pi` — 不需要 facing 参数，因为 Owner 模式下扇形朝向自动跟随实体。`Rotation = Pi` 在此基础上加 180°，指向实体背后。

#### DrawRectOnOwner

```csharp
dp.Owner = ownerId;
dp.Scale = new(width, length);  // ← X=宽度, Y=长度
```

- 矩形总是用 Owner 模式(矩形跟随 Boss 移动和转向最自然)
- `Scale.X` = 矩形宽度, `Scale.Y` = 沿 Owner 朝向的延伸长度

#### DrawCircleOnTarget vs DrawCircleAt

| 方法 | 定位 | 何时用 |
|---|---|---|
| `DrawCircleOnTarget` | `Owner = targetId` | 绑在玩家/Boss 身上, 跟人动 |
| `DrawCircleAt` | `Position = pos` | 固定位置, 如地面塔/圈 |

`DrawCircleOnTarget` 用于点名分摊(圈跟被点名的人走)，`DrawCircleAt` 用于塔/地面 AOE(圈留在原地)。

#### GuideAllToTarget

```csharp
for (int i = 0; i < 4; i++)  // 四人本, 遍历 PartyList[0..3]
{
    dp.Owner = a.Data.PartyList[i];  // 箭头从第 i 个队员身上出发
    dp.TargetObject = targetId;      // 指向目标(被点名玩家)
}
```

- **四人本硬编码**: `i < 4`。八人本需改为 `i < 8`
- **TargetObject 实时跟踪**: 和被点名玩家一样，目标移动时箭头自动更新方向
- **Imgui + ScaleMode.YByDistance**: 屏幕叠加绘制，距离越远箭头越大

#### GuideKnockback — 击退指路

```csharp
// 红线: Boss → 玩家
dp.Owner = sourceId;         // 起点 = Boss
dp.TargetObject = a.Data.Me; // 终点 = 玩家
dp.ScaleMode |= ScaleMode.YByDistance;
a.Method.SendDraw(DrawModeEnum.Default, DrawTypeEnum.Displacement, dp);

// 绿线: 玩家 → 远离Boss
dp.Owner = a.Data.Me;        // 起点 = 玩家
dp.TargetPosition = fromPos; // 终点 = Boss坐标
dp.Rotation = float.Pi;      // 180°反转 = 远离Boss
a.Method.SendDraw(DrawModeEnum.Imgui, DrawTypeEnum.Displacement, dp);
```

**两线模式差异原因：**

| | 红线 | 绿线 |
|---|---|---|
| 模式 | `Default` | `Imgui` |
| 起→终 | Owner(Boss) → TargetObject(玩家) | Owner(玩家) → TargetPosition(Boss坐标) |
| 长度 | 自动 = 两实体距离 | 固定 = `Scale.Y * 2` |
| 为什么不能用 Imgui | Imgui 不支持 `TargetObject` 动态跟踪 | — |
| 为什么不能用 Default | — | 屏幕叠加更清晰, 不受地面遮挡 |

**Rotation = Pi 的数学原理:**
- 不加 Rotation: 箭头从玩家指向 Boss (错误，指向危险源)
- `Rotation = Pi` (180° 弧度): 箭头反转，从玩家指向 Boss 的**反方向** (正确，指向安全方向)

### 4.3 第三层: 基础工具 (2+1 个方法)

#### ConvertDegree
```csharp
private static float ConvertDegree(float degree)
    => degree * float.Pi / 180f;
```

- `float.Pi` 来自 `Dalamud.Utility.Numerics`
- `180f` 的 `f` 后缀表示 `float` 字面量，没有 `f` 的话 `180` 是 `double` 类型
- **为什么需要**: 人类用度数(90°, 180°)思考，但代码中所有角度都必须是弧度

#### ParseObjectId
```csharp
private static bool ParseObjectId(string? idStr, out ulong id)
```

- 游戏中的 ObjectId 是十六进制字符串，如 `"400004EF"`
- `Replace("0x", "")`: 去掉可能的 `0x` 前缀
- `ulong.Parse(..., HexNumber)`: 按十六进制解析
- `out ulong id`: 通过 `out` 返回，`return false` 表示解析失败

#### DebugLog
```csharp
private void DebugLog(ScriptAccessory a, string msg)
{
    if (EnableDeveloperMode)
        a.Log.Debug(msg);
}
```

- 非 static(是实例方法)，因为要访问 `EnableDeveloperMode` 实例属性
- 输出到 Dalamud 控制台 (`/xllog`)

---

## 五、核心设计模式

### 5.1 防御式数据提取

每个数据提取都包在 `try-catch` 中:

```csharp
// 模式
if (!TryGetSomething(@event, out var data)) return;
// 等价于: 提取失败 → 静默退出, 不报错, 不画图
```

**为什么这样做**: 游戏事件数据可能因副本状态、网络延迟等原因缺失字段。直接崩溃会让可达鸭报错，静默退出只是这次技能不画图，不影响后续。

### 5.2 颜色通过参数传递

```csharp
// 调用方
DrawDangerFan(accessory, ..., DangerColour.V4);
DrawSafeFan(accessory, ..., SafeColour.V4);

// 辅助方法签名
DrawDangerFan(..., Vector4 color)
```

**为什么不像最初版本用 `a.Data.DefaultDangerColor`**: 用户可以在可达鸭 UI 中实时调整颜色，而不需要修改代码。

### 5.3 Owner 优先于 Position

| 场景 | 用什么 |
|---|---|
| Boss 身上的技能 (扇形、矩形、圆形) | `Owner = sourceId` |
| Boss 读条时会动/转 | `Owner = sourceId` |
| 地面固定位置 (塔、圈、AOE) | `Position = effectPos` |
| 绑在玩家身上 (分摊圈) | `Owner = targetId` |

**Owner 的优势**: 自动跟随实体移动和转向, 不需要手动计算位置和朝向。

### 5.4 指路用 TargetObject 优先于 TargetPosition

| 场景 | 用什么 |
|---|---|
| 指向会动的目标 (被点名玩家、Boss) | `TargetObject = targetId` |
| 指向固定位置 (塔、集合点) | `TargetPosition = pos` |

**TargetObject 的优势**: 从游戏内存读取实时坐标, 目标移动时箭头自动更新。`TargetPosition` 只在事件触发时取一次坐标, 之后不变。

### 5.5 方法重载实现多态

`DrawDangerFan` 和 `DrawSafeFan` 各有两个重载:
- 一个接收 `Vector3 pos, float facing` (Position 模式)
- 一个接收 `ulong ownerId` (Owner 模式)

调用方根据场景选择重载, 无需记忆不同方法名。

---

## 六、数据流完整追踪

以 **石化光束** 为例，追踪一次完整的画图过程：

```
┌─ 游戏事件 ─────────────────────────────────────────────┐
│ Boss 开始读条 ActionId=50177                           │
│ 事件数据:                                              │
│   SourcePosition:  {"X":660,"Y":-15,"Z":-141}         │
│   SourceRotation:  2.07                               │
│   DurationMilliseconds: 8200                           │
└───────────────────────────────────────────────────────┘
                    ↓
┌─ Step 1: [ScriptMethod] 匹配 ──────────────────────────┐
│ eventCondition: ["ActionId:regex:^(50177|50178)$"]     │
│ 50177 匹配成功 → 触发 石化光束()                        │
└───────────────────────────────────────────────────────┘
                    ↓
┌─ Step 2: TryGetBossTransform() ────────────────────────┐
│ JsonConvert.DeserializeObject<Vector3>(                │
│   "{\"X\":660,\"Y\":-15,\"Z\":-141}")                 │
│   → bossPos = (660, -15, -141)                        │
│                                                        │
│ JsonConvert.DeserializeObject<double>("2.07")          │
│   → bossRotation = 2.07                               │
│   → bossFacing = (float)2.07                          │
│                                                        │
│ JsonConvert.DeserializeObject<int>("8200")             │
│   → durationMs = 8200                                 │
└───────────────────────────────────────────────────────┘
                    ↓
┌─ Step 3: DrawDangerFan(Position 重载) ─────────────────┐
│ dp.Position = (660, -15, -141)      ← Boss 脚下       │
│ dp.Rotation = 2.07                  ← Boss 面朝       │
│ dp.Scale = new(100)                 ← 半径 100 码     │
│ dp.Radian = ConvertDegree(100)      ← 2.094 rad       │
│ dp.DestoryAt = 8200                 ← 8.2 秒后消失     │
│ dp.Color = DangerColour.V4          ← 用户设置的红色   │
│ SendDraw(Default, Fan, dp)                            │
└───────────────────────────────────────────────────────┘
                    ↓
┌─ Step 4: DrawSafeFan(Position 重载) ───────────────────┐
│ dp.Position = (660, -15, -141)                         │
│ dp.Rotation = 2.07 + π = 5.21     ← Boss 背后        │
│ dp.Radian = ConvertDegree(260)     ← 360-100=260°     │
│ dp.Color = SafeColour.V4           ← 用户设置的绿色   │
│ SendDraw(Default, Fan, dp)                            │
└───────────────────────────────────────────────────────┘
                    ↓
┌─ 渲染结果 ────────────────────────────────────────────┐
│ 地面出现两个扇形:                                      │
│   Boss 面前 100° = 红色 = 别站这                       │
│   Boss 背后 260° = 绿色 = 站这安全                     │
│ 8.2 秒后自动消失                                      │
└───────────────────────────────────────────────────────┘
```

---

## 七、事件数据速查表

从实践中学到的，每种事件能拿到什么数据：

| 事件类型 | 关键字段 | 用途 |
|---|---|---|
| `StartCasting` | `ActionId`, `SourceId`, `SourcePosition`, `SourceRotation`, `DurationMilliseconds`, `TargetId`, `TargetPosition`, `EffectPosition` | 最常用, 99% 的技能画图 |
| `ActionEffect` | `ActionId`, `SourceId`, `TargetId` | 步进式 AOE 等延迟判定技能 |
| `StatusAdd` | `StatusID`, `TargetId`, `Duration` | Buff/Debuff 点名、死刑标记 |
| `StatusRemove` | `StatusID`, `TargetId` | Buff 消失 (清除对应画图) |
| `ObjectEffect` | `Id1`, `Id2`, `SourcePosition` | 地面 AOE/地火的核心数据源 |
| `EnvControl` | `DirectorId`, `State`, `Index` | 转阶段信号 |
| `Tether` | `TetherId`, `SourceId`, `TargetId` | 连线机制 |
| `AddCombatant` | `DataId`, `Name`, `SourceId` | Boss/小怪出现时获取 DataId |
| `SetObjPos` | `SourceDataId`, `SourcePosition` | 追踪实体位置变化 |
| `TargetIcon` | (目标标记 ID) | 头上标记(攻击1/2/3 等)变化 |

---

## 八、绘图属性完整对照表

`DrawProperties` (通过 `GetDefaultDrawProperties()` 获取) 的所有可设置属性：

| 属性 | 类型 | 说明 | 适用形状 |
|---|---|---|---|
| `Owner` | `ulong` | 绑定的实体 ID，绘图跟随实体移动 | 全部 |
| `Position` | `Vector3` | 世界坐标，固定位置 | 全部 |
| `TargetObject` | `ulong` | 目标实体 ID，箭头/扇形朝向 | Displacement, Fan |
| `TargetPosition` | `Vector3` | 目标世界坐标 | Displacement, Straight |
| `Scale` | `Vector2` | `X=宽/半径`, `Y=长` | 全部 |
| `InnerScale` | `Vector2` | 内径 | Donut |
| `Radian` | `float` | 弧度角度 | Fan, Donut |
| `Rotation` | `float` | 旋转角度(弧度) | Fan, Rect, Straight, Displacement |
| `Offset` | `Vector3` | 相对 Owner 的偏移 `(左右, 上下, 前后)` | 全部 |
| `Color` | `Vector4` | RGBA 颜色 | 全部 |
| `DestoryAt` | `int` | 显示时长(毫秒) | 全部 |
| `Delay` | `int` | 延迟显示(毫秒) | 全部 |
| `ScaleMode` | `ScaleMode` | `YByDistance` = 根据距离缩放 | Displacement |
| `Name` | `string` | 唯一标识，用于 `RemoveDraw("name")` 精确移除 | 全部 |

---

## 九、DrawTypeEnum 形状总览

| 枚举值 | 画出来 | `Scale` 含义 | 特殊属性 |
|---|---|---|---|
| `Circle` | 圆形 | `X` = 半径 | — |
| `Fan` | 扇形 | `X` = 半径 | `Radian`(宽度), `Rotation`(朝向) |
| `Donut` | 环形 | `X` = 外径 | `InnerScale.X` = 内径 |
| `Rect` | 矩形 | `X` = 宽, `Y` = 长 | `Rotation`(朝向), `Offset`(偏移) |
| `Straight` | 直线 | `X` = 线宽, `Y` = 半长 | `Rotation`(角度) |
| `Displacement` | 箭头 | `X` = 线宽, `Y` = 半长 | `TargetObject`/`TargetPosition` |
| `Arrow` | 箭头 | `X` = 线宽, `Y` = 半长 | `TargetPosition` |

---

## 十、通过迭代学到的经验教训

### 10.1 Owner vs Position 的选择

**教训**: 一开始 `肉弹` 用了 `Position + Rotation` 手动算朝向，方向一直不对。换成 `Owner` 一行搞定。

**规则**: Boss 身上的技能一律 `Owner = sourceId`。只有地面固定位置才用 `Position`。

### 10.2 TargetPosition vs TargetObject 的选择

**教训**: 点名分摊最初用 `TargetPosition = stackPos`(固定坐标)，被点名玩家一走圈就不在脚下了。换成 `TargetObject = targetId` + `Owner = targetId` 后自动跟人。

**规则**: 目标是活的 → `TargetObject`。目标是死的(地面塔) → `TargetPosition`。

### 10.3 Imgui vs Default 的适用场景

**教训**: 击退红线用 `Imgui + Owner + TargetObject` 不会画。换回 `Default` 就好了。

**规则**:
- `Default`: 地面绘制。`Owner + TargetObject` 动态连线必须用 Default
- `Imgui`: 屏幕叠加。`Owner + TargetPosition` 的指路箭头用 Imgui 更清晰

### 10.4 EffectPosition vs TargetPosition 的区别

**教训**: 肉压杀_塔 最初用 `TargetPosition` 取塔坐标拿到 `(0,0,0)`。换成 `EffectPosition` 才对。

**规则**: Boss 放的塔/地面 AOE → `EffectPosition`。点名玩家放的圈 → `TargetPosition`。

### 10.5 辅助方法的静态 vs 实例

**教训**: 最初辅助方法用 `a.Data.DefaultDangerColor` 硬编码颜色。改为加 `Vector4 color` 参数后，颜色从 UserSetting 传入，用户可自定义。

**规则**: `private static` 方法不能访问 `ScriptColor` 实例属性，必须通过参数传入。

### 10.6 不要过早提取公共方法

**教训**: 击退的 `GuideKnockback` 改了七八版 — 两线/单线、Imgui/Default、Owner/Position，每次改都涉及签名和调用方。

**规则**: 先写对，再提取。模式稳定后再封装。

---

## 十一、当前脚本中 10 个技能的画图形状清单

| # | 技能名 | 形状 | 定位 | 颜色 |
|---|---|---|---|---|
| 1 | 屏幕提示 | 无(纯文字) | — | — |
| 2 | 点名分摊 | Circle + 四人 Displacement | Owner(Target) | 绿 |
| 3 | 石化光束 | Fan 100° + Fan 260° | Position | 红/绿 |
| 4 | 肉弹 | Rect 16×60 或 16×90 | Owner | 红 |
| 5 | 肉压杀 | Displacement×2 | Owner/Imgui | 红+绿 |
| 6 | 呕吐 | Fan 120° + Fan 240° | Owner | 红/绿 |
| 7 | 肉压杀_塔 | Circle 半径4 | Position | 绿 |
| 8 | 虚无黑暗 | Rect 100×100 | Owner | 红 |
| 9 | 废料光环 | Circle 半径7 | Position | 红 |
| 10 | 废料瘴气 | Fan 30° | Position | 红 |
