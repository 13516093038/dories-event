using System;
using System.Collections.Generic;
using R3;
using UnityEngine;

namespace Dories.Event.Runtime
{
    /// <summary>
    /// 全局类型化事件总线：发布方和订阅方只约定消息类型，互不引用。
    /// Receive 返回 R3 Observable，可直接 .Subscribe(...).AddTo(this) 绑定生命周期，并使用 R3 操作符。
    /// 本组件销毁时所有订阅随之结束。仅限主线程使用；消息类型建议用 struct 避免分配。
    /// </summary>
    public class EventEntity : MonoBehaviour
    {
        internal sealed class ChannelInfo
        {
            public int Subscribers;
            public int PublishCount;
            public float LastPublishTime = -1f;
        }

        private readonly Dictionary<Type, IDisposable> _subjects = new();
        private readonly Dictionary<Type, ChannelInfo> _infos = new();

        // 仅供 Inspector 监视
        internal Dictionary<Type, ChannelInfo> Infos => _infos;

        public void Publish<T>(T message)
        {
            var subject = GetOrCreate<T>(out var info);
#if UNITY_EDITOR
            info.PublishCount++;
            info.LastPublishTime = Time.unscaledTime;
#endif
            subject.OnNext(message);
        }

        public Observable<T> Receive<T>()
        {
            var subject = GetOrCreate<T>(out var info);
#if UNITY_EDITOR
            return subject.Do(onSubscribe: () => info.Subscribers++, onDispose: () => info.Subscribers--);
#else
            return subject;
#endif
        }

        // 发布时也建通道，Inspector 才能看到"有人发但没人收"的事件
        private Subject<T> GetOrCreate<T>(out ChannelInfo info)
        {
            var type = typeof(T);
            if (_subjects.TryGetValue(type, out var subject))
            {
                info = _infos[type];
                return (Subject<T>)subject;
            }

            var created = new Subject<T>();
            _subjects.Add(type, created);
            _infos.Add(type, info = new ChannelInfo());
            return created;
        }

        internal void DisposeAll()
        {
            foreach (var subject in _subjects.Values)
                subject.Dispose();
            _subjects.Clear();
            _infos.Clear();
        }

        private void OnDestroy() => DisposeAll();
    }
}
