using System;
using System.Collections.Generic;
using Hudl.Mjolnir.Config;

namespace Hudl.Mjolnir.Tests.Bulkhead
{
    public class TestBulkheadConfiguration : BulkheadConfiguration
    {
        private class Subscription: IDisposable
        {
            private readonly Action _onDispose;
            public Subscription(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                _onDispose();
            }
        }
        

        public TestBulkheadConfiguration()
        {
            _observers = new List<IObserver<BulkheadConfiguration>>();
        }

        private readonly List<IObserver<BulkheadConfiguration>> _observers;

        public override int MaxConcurrent
        {
            get => _maxConcurrent;
            set
            {
                _maxConcurrent = value;
                _observers?.ForEach(o => o.OnNext(this));
            }
        }

        public override IDisposable Subscribe(IObserver<BulkheadConfiguration> observer)
        {
            var subscription = new Subscription(() => _observers.Remove(observer));
            _observers.Add(observer);
            return subscription;
        }
    }
}