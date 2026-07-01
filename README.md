# JulyCommon

July 框架 L0 基座工具集（`com.july.common`）。提供日志、Result 模式、异常体系、线程安全单例，以及启动期全局 DI 容器。被 JulyArch / JulyBoot / JulyGame 等上层包依赖。

> **本文档描述框架的真实行为，与 `Runtime/` 代码一一对应。**

## 模块概览

| 模块 | 类型 | 职责 |
|------|------|------|
| **JulyDI** | 静态容器 | 启动期值对象注册/解析（BootConfig、CDN 端点等） |
| **JLogger** | 静态工具 | Debug 替代，通道过滤 + 级别过滤 + 服务端上报 |
| **FrameworkResult / FrameworkResult&lt;T&gt;** | struct | 统一 Result 模式，带错误码 |
| **FrameworkErrorCode** | enum | 分层错误码（1xxx–8xxx，9xxx 预留给项目） |
| **JulyException** | 异常类 | 携带 `FrameworkErrorCode` 的框架异常 |
| **Singleton&lt;T&gt;** | 抽象基类 | 线程安全双重检查锁单例 |

## JulyDI — 启动期 DI 容器

存放**无生命周期、不随 Scope** 的启动期值对象。与 `ArchContext.GetSystem<T>()` 管理的 System 互补：

| | JulyDI | ArchContext |
|---|--------|-------------|
| 存放对象 | 启动配置、快照、端点 | Store / System / View |
| 生命周期 | 启动注册 → 退出 Clear | InitializeAsync → Shutdown |
| 典型时机 | AOT 启动链 | 热更注册后 |

> 详细对比见 `Docs/di-vs-arch.md`（项目层文档）。

```csharp
// 启动期注册
JulyDI.Register(new BootConfig { CdnUrl = "https://cdn.example.com" });

// 任意位置解析
var config = JulyDI.Resolve<BootConfig>();

// 可选解析（未注册返回 false）
if (JulyDI.TryResolve<BootConfig>(out var cfg)) { ... }

// 退出清理（JulyGameEntry.OnDestroy 自动调用）
JulyDI.Clear();  // 逆序 Dispose 所有 IDisposable 实例
```

重复注册同类型会输出 Warning 并覆盖。

## JLogger — 条件编译日志

`[Conditional("JULYGF_DEBUG")]` 标记的方法在**未定义 `JULYGF_DEBUG` 宏时编译期移除**，Release 包体零开销：

| 方法组 | JULYGF_DEBUG | 说明 |
|--------|:---:|------|
| `Log` / `LogDebug` / `LogInfo` / `LogWarning` / `LogChannel` / `Assert` | 需要 | 调试日志，Release 不编译 |
| `LogError` / `LogFatal` / `LogException` / `LogChannelError` | 不需要 | 错误始终保留 |

```csharp
// 通道过滤（Flags 枚举，可按模块开关）
JLogger.InitLogChannels(LogChannel.UI | LogChannel.Network);
JLogger.LogChannel(LogChannel.UI, "UISystem", "Window opened");

// 级别过滤
JLogger.SetMinLevel(LogLevel.Warning);

// 服务端上报（Error/Fatal/Exception 自动触发）
JLogger.SetReporter(new MyLogReporter());
```

## FrameworkResult — Result 模式

```csharp
// 同步
FrameworkResult result = FrameworkResult.Success();
FrameworkResult<T> data = FrameworkResult<int>.Success(42);

if (result) { ... }                          // 隐式 bool 转换
result.ThrowIfFailure();                       // 失败抛 JulyException
var value = data.GetValueOrThrow();            // 失败抛异常，成功返回值

// 从异常创建（自动映射 ErrorCode）
var fail = FrameworkResult.FromException(ex);

// UniTask 扩展（ResultExtensions）
var result = await LoadAssetAsync().ToResultAsync();
var result = await ResultExtensions.TryExecuteAsync(() => DoWorkAsync());
```

`FrameworkResult<T>` 支持从 `T` 隐式转换为 Success 结果。

## FrameworkErrorCode — 分层错误码

| 区间 | 类别 | 示例 |
|------|------|------|
| 0 | 成功 | `Success` |
| 1xxx | 通用 | `InvalidArgument`、`Timeout`、`Cancelled` |
| 2xxx | Module | `ModuleNotFound`、`ModuleInitFailed` |
| 3xxx | Provider | `ProviderNotFound` |
| 4xxx | 资源 | `ResourceNotFound`、`ResourceLoadFailed` |
| 5xxx | 网络 | `NetworkConnectionFailed`、`NetworkTimeout` |
| 6xxx | UI | `UINotFound`、`UIOpenFailed` |
| 7xxx | 数据 | `SerializeFailed`、`SaveFailed` |
| 8xxx | 配置 | `ConfigNotFound`、`ConfigFormatError` |
| 9xxx | 业务 | **预留给项目扩展** |

`JulyException` 携带 `ErrorCode`，`FrameworkResult.FromException` 会自动识别 `JulyException` 并保留其错误码。

## Singleton&lt;T&gt;

线程安全双重检查锁，子类 override `OnInit()` / `OnDispose()`：

```csharp
public class MyManager : Singleton<MyManager>
{
    protected override void OnInit() { /* 首次访问时调用 */ }
}

var mgr = MyManager.Instance;
Singleton<MyManager>.Dispose();  // 显式销毁
```

## JULYGF_DEBUG 宏

项目级调试开关。定义后：
- `JLogger` 的 Log / Warning / Assert / LogChannel 等方法生效
- JulyGame 的 GM 系统等 Debug 模块编译

未定义时上述代码**编译期移除**，不影响 Release 性能。

## 依赖

无框架层依赖。程序集：`JulyCommon.Runtime`（Runtime）、`JulyCommon.Editor`（Editor 日志重定向）。
