using System;
using System.Collections.Generic;
using Hudl.Mjolnir.Config;

namespace Hudl.Mjolnir.Tests.Configuration.Helpers
{
    public class ExampleMjolnirConfiguration: MjolnirConfiguration
    {        
        public override IDisposable Subscribe(IObserver<MjolnirConfiguration> observer)
        {
            var subscription = new Subscription(() => _observers.Remove(observer));
            _observers.Add(observer);
            return subscription;
        }

        /// <summary>
        /// Notify all observers that config has been changed.
        /// </summary>
        public void Notify()
        {
            _observers.ForEach(observer => observer.OnNext(this));
        }
        
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
        
        private readonly List<IObserver<MjolnirConfiguration>> _observers = new List<IObserver<MjolnirConfiguration>>();

    }
}