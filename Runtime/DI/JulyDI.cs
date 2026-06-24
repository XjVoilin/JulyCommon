using System;
using System.Collections.Generic;

namespace JulyCommon
{
    /// <summary>
    /// 全局极简 DI 容器 —— 存放启动期值对象（如 BootConfig / ConfigSnapshot / CDNEndpoints）。
    /// 这些对象无生命周期、不随 Scope，在 AOT 启动链中创建，区别于走 ArchContext.GetSystem 的 System。
    /// </summary>
    public static class JulyDI
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            var type = typeof(T);
            WarnIfOverride(type, instance.GetType());
            _services[type] = instance;
        }

        public static void Register(Type type, object instance)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (!type.IsInstanceOfType(instance))
                throw new ArgumentException($"实例类型 {instance.GetType().Name} 未实现 {type.Name}");
            WarnIfOverride(type, instance.GetType());
            _services[type] = instance;
        }

        public static T Resolve<T>()
        {
            var type = typeof(T);
            if (!_services.TryGetValue(type, out var instance))
                throw new JulyException($"服务 {type.Name} 未注册");
            return (T)instance;
        }

        public static object Resolve(Type type)
        {
            if (!_services.TryGetValue(type, out var instance))
                throw new JulyException($"服务 {type.Name} 未注册");
            return instance;
        }

        public static bool TryResolve<T>(out T instance)
        {
            instance = default;
            if (!_services.TryGetValue(typeof(T), out var obj))
                return false;
            if (obj is T typed)
            {
                instance = typed;
                return true;
            }
            return false;
        }

        public static void Clear()
        {
            var snapshot = new List<object>(_services.Values);
            for (int i = snapshot.Count - 1; i >= 0; i--)
            {
                if (snapshot[i] is IDisposable disposable)
                {
                    try { disposable.Dispose(); }
                    catch (Exception ex) { JLogger.LogException(ex); }
                }
            }
            _services.Clear();
        }

        private static void WarnIfOverride(Type type, Type newImplType)
        {
            if (!_services.TryGetValue(type, out var existing)) return;
            JLogger.LogWarning(
                $"{FrameworkConst.TagDependencyContainer} 服务 {type.Name} 已注册为 {existing.GetType().Name}，将被覆盖为 {newImplType?.Name}");
        }
    }
}
