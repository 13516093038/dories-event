using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Dories.Event.Runtime;
using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Dories.Event.Tests
{
    public class EventEntityTests
    {
        private struct GoldChanged
        {
            public int Gold;
        }

        private struct LevelUp
        {
        }

        private readonly CompositeDisposable _disposables = new();
        private EventEntity _event;

        [SetUp]
        public void SetUp() => _event = new GameObject("Event").AddComponent<EventEntity>();

        [TearDown]
        public void TearDown()
        {
            _disposables.Clear();
            _event.DisposeAll();
            Object.DestroyImmediate(_event.gameObject);
        }

        [Test]
        public void Publish_DeliversToAllSubscribers()
        {
            var a = new List<int>();
            var b = new List<int>();
            _event.Receive<GoldChanged>().Subscribe(e => a.Add(e.Gold)).AddTo(_disposables);
            _event.Receive<GoldChanged>().Subscribe(e => b.Add(e.Gold)).AddTo(_disposables);

            _event.Publish(new GoldChanged { Gold = 10 });

            CollectionAssert.AreEqual(new[] { 10 }, a);
            CollectionAssert.AreEqual(new[] { 10 }, b);
        }

        [Test]
        public void DisposedSubscription_NoLongerReceives()
        {
            int count = 0;
            var subscription = _event.Receive<GoldChanged>().Subscribe(_ => count++);

            _event.Publish(new GoldChanged());
            subscription.Dispose();
            _event.Publish(new GoldChanged());

            Assert.AreEqual(1, count);
        }

        [Test]
        public void DifferentMessageTypes_AreIsolated()
        {
            int gold = 0;
            _event.Receive<GoldChanged>().Subscribe(_ => gold++).AddTo(_disposables);

            _event.Publish(new LevelUp());

            Assert.AreEqual(0, gold);
        }

        [Test]
        public void ThrowingSubscriber_DoesNotBlockOthers()
        {
            int received = 0;
            _event.Receive<LevelUp>().Subscribe(_ => throw new InvalidOperationException("boom")).AddTo(_disposables);
            _event.Receive<LevelUp>().Subscribe(_ => received++).AddTo(_disposables);

            // R3 把订阅回调里的异常交给 UnhandledExceptionHandler（Unity 下打日志），订阅不会中断
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            LogAssert.Expect(LogType.Exception, new Regex("boom"));
            _event.Publish(new LevelUp());
            _event.Publish(new LevelUp());

            Assert.AreEqual(2, received);
        }

        [Test]
        public void Receive_SupportsR3Operators()
        {
            var big = new List<int>();
            _event.Receive<GoldChanged>().Where(e => e.Gold >= 100).Select(e => e.Gold)
                .Subscribe(big.Add).AddTo(_disposables);

            _event.Publish(new GoldChanged { Gold = 50 });
            _event.Publish(new GoldChanged { Gold = 150 });

            CollectionAssert.AreEqual(new[] { 150 }, big);
        }

        [Test]
        public void Monitor_TracksSubscribersAndPublishes()
        {
            var subscription = _event.Receive<GoldChanged>().Subscribe(_ => { });
            _event.Publish(new GoldChanged());
            _event.Publish(new LevelUp());

            var gold = _event.Infos[typeof(GoldChanged)];
            Assert.AreEqual(1, gold.Subscribers);
            Assert.AreEqual(1, gold.PublishCount);
            // 发布了但没人订阅的事件也有记录
            Assert.AreEqual(0, _event.Infos[typeof(LevelUp)].Subscribers);
            Assert.AreEqual(1, _event.Infos[typeof(LevelUp)].PublishCount);

            subscription.Dispose();
            Assert.AreEqual(0, gold.Subscribers);
        }

        [Test]
        public void DisposeAll_CompletesSubscriptions()
        {
            bool completed = false;
            _event.Receive<GoldChanged>().Subscribe(_ => { }, _ => completed = true).AddTo(_disposables);

            _event.DisposeAll();

            Assert.IsTrue(completed);
            Assert.AreEqual(0, _event.Infos.Count);
        }
    }
}
