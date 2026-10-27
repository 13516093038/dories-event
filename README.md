# Dories Event

基于 [R3](https://github.com/Cysharp/R3) 的全局类型化事件总线（`EventEntity`）。只提供机制，事件类型由项目自己定义。

## 安装 R3

1. Package Manager 加 NuGetForUnity：`https://github.com/GlitchEnzo/NuGetForUnity.git?path=/src/NuGetForUnity#v4.5.0`
2. NuGet 安装 `R3`（会带上 Microsoft.Bcl.TimeProvider、System.Threading.Channels 等依赖）
3. Package Manager 加 `https://github.com/Cysharp/R3.git?path=src/R3.Unity/Assets/R3.Unity#1.3.1`（版本与 NuGet R3 一致）

未安装 `com.cysharp.r3` 时本包不参与编译。

## 用法

`EventEntity` 挂在 GameEntry 预制体的 `Event` 节点下，通过 `GameEntry.Event` 访问（不用 GameFramework 时可自己挂到任意常驻对象上）。

```csharp
public struct GoldChanged { public int Gold; }

// 发布方
GameEntry.Event.Publish(new GoldChanged { Gold = 100 });

// 订阅方：AddTo(this) 随对象销毁自动取消订阅；可用 R3 操作符
GameEntry.Event.Receive<GoldChanged>()
    .Where(e => e.Gold > 0)
    .Subscribe(e => _label.text = e.Gold.ToString())
    .AddTo(this);
```

- Inspector 在 Play Mode 显示每种事件的订阅数、发布次数、距上次发布的时间；有人发布但没人订阅的事件标黄。
- EventEntity 销毁时所有订阅随之结束（收到 OnCompleted）。
- 模块内部的状态/数据绑定直接用 R3 的 `ReactiveProperty<T>` / `Subject<T>`，跨模块、互不引用时才用 EventEntity。
- 订阅回调抛异常不会影响其他订阅者，异常交给 R3 的 UnhandledExceptionHandler（Unity 下打 Exception 日志）。
- 仅限主线程；消息类型建议用 struct。
- 定时需求直接用 R3（`Observable.Timer` / `Interval` / `EveryUpdate`）或 UniTask（`UniTask.Delay`），框架不另做定时器模块。
