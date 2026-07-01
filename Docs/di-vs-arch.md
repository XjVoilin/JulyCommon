# JulyDI vs ArchContext 边界指南

两套服务定位机制，职责互补、不可混用：

| | **JulyDI** | **ArchContext** |
|---|---|---|
| 定位 | 启动期全局值对象 | 运行时架构角色 |
| 注册/解析 | `Register` / `Resolve` | `RegisterStore/System/View` / `Get*` |
| 生命周期 | 无 Init/Shutdown/Update | `InitializeAsync` → `Shutdown` |

---

## 1. 决策树

```
需要注册/解析一个对象
├─ 有 Init / Shutdown 生命周期？ ──是──► ArchContext（Store / System）
├─ 需要每帧 Update？ ──是──► ArchContext（System + IUpdatableSystem）
├─ 启动时只读配置/快照？ ──是──► JulyDI
├─ 场景绑定 View？ ──是──► ArchContext（GameView + ISingletonView）
└─ 一次性异步流程？ ──是──► ArchContext（Procedure，RunProcedure 即用即弃）
```

**一句话**：有生命周期走 ArchContext，无生命周期的启动配置走 JulyDI。

---

## 2. JulyDI 适用场景

- `BootConfig`、`CDNEndpoints`、`PlatformConfig` — 启动期值对象
- 在 **LaunchStep** 中创建并 `JulyDI.Register`，随应用全程存活
- 无 Init / Shutdown / Update；退出时 `JulyGameEntry.OnDestroy` 调用 `JulyDI.Clear()`

---

## 3. ArchContext 适用场景

| 角色 | 职责 | 访问 |
|---|---|---|
| **Store** | 可变数据 + 业务规则（写方法 `internal`） | `GetStore<T>()` |
| **System** | 业务编排，Init → PostInit → Shutdown | `GetSystem<T>()` |
| **View** | 场景内唯一控制器 | `GetView<T>()`（`ISingletonView`） |
| **Procedure** | 一次性异步长流程 | `RunProcedure()` |

在 `IHotUpdateRegistrar.Register()` 或 LaunchStep 中注册；`InitializeAsync` / `Shutdown` 统一管理生命周期。需帧更新的 System 实现 `IUpdatableSystem`。

---

## 4. 清理顺序

`JulyGameEntry.OnDestroy` 固定顺序，**不可颠倒**：

```
ArchContext.Current?.Shutdown()   // ① System（逆序）→ Store（逆序）→ Event
JulyDI.Clear()                    // ② 逆序 Dispose IDisposable
```

System Shutdown 可能仍读取 JulyDI 配置；先 Clear 会导致访问已释放对象。

---

## 5. 反模式

- ❌ 把有状态 System 注册到 JulyDI → `ctx.RegisterSystem(...)`
- ❌ 把无生命周期配置注册为 Store → `JulyDI.Register(...)`
- ❌ 同类服务混用 `JulyDI.Resolve` 和 `GetSystem` → 选定一种，全局一致
- ❌ LaunchStep 中 `new XxxSystem()` 而不 Register → Register + `InitializeAsync`

---

## 6. 代码示例

```csharp
// ── JulyDI：LaunchStep 中注册启动配置 ──
JulyDI.Register(new BootConfig { CdnUrl = "https://cdn.example.com" });
var cdn = JulyDI.Resolve<BootConfig>().CdnUrl;

// ── ArchContext：Registrar 注册 + 初始化 ──
var ctx = ArchContext.Current;
ctx.RegisterStore(new PlayerStore());
ctx.RegisterSystem(new AudioSystem());  // IUpdatableSystem 自动加入 Update
await ctx.InitializeAsync(ct);

// ── 业务访问 ──
var player = this.GetStore<PlayerStore>();
var audio = this.GetSystem<AudioSystem>();
var board = this.GetView<BoardView>();
await this.RunProcedure(new EnterGameProcedure(), ct);
```
