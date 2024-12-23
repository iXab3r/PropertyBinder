﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    [Ignore("Too flaky on build-servers")]
    internal class ThreadingBindingFixture : BindingsFixture
    {
        private static readonly ThreadSafeRandom Rng = new ThreadSafeRandom();
        
        [Test]
        [Repeat(100)]
        public void ShouldAssignBoundPropertyWhenAttached()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);
            _stub.Int = 3;
            _stub.String.ShouldBe(null);

            var startSignal = new ManualResetEventSlim(false);
            var changer = Task.Factory.StartNew(() =>
            {
                startSignal.Wait();
                Thread.Sleep(Rng.Next(0, 10));
                _stub.Int = 4;
            });

            IDisposable anchor = default;
            var attacher =  Task.Factory.StartNew(() =>
            {
                startSignal.Wait();
                Thread.Sleep(Rng.Next(0, 10));
                anchor = _binder.Attach(_stub);
            });

            startSignal.Set();
            Task.WaitAll(changer, attacher);
            _stub.Int.ShouldBe(4);
            _stub.String.ShouldBe("4");
            anchor?.Dispose();
        }
        
        [Test]
        [Repeat(100)]
        [Ignore("Known issue - multi-threaded changes will lead to inconclusive result")]
        public void ShouldAssignBoundPropertyWhenAttachedMultipleChanges()
        {
            _binder.Bind(x => x.Int.ToString()).To(x => x.String);
            _stub.Int = 3;
            _stub.String.ShouldBe(null);
            var startSignal = new ManualResetEventSlim(false);
            var changer1 = Task.Factory.StartNew(() =>
            {
                startSignal.Wait();
                Thread.Sleep(Rng.Next(0, 10));
                _stub.Int = 4;
            });
            var changer2 = Task.Factory.StartNew(() =>
            {
                startSignal.Wait();
                Thread.Sleep(Rng.Next(0, 10));
                _stub.Int = 5;
            });
            
            _binder.Attach(_stub);
            _stub.String.ShouldBe("3");

            startSignal.Set();
            Task.WaitAll(changer1, changer2);
            _stub.String.ShouldBe(_stub.Int.ToString());
        }
        
        [Test]
        [Repeat(100)]
        public void ShouldAssignBoundPropertyInsideCollectionWhenAttached()
        {
            _binder.Bind(x => x.Collection.All(y => y.Int == 1)).To(x => x.Flag);
            var item1 = new UniversalStub();
            var item2 = new UniversalStub();
            _stub.Collection.Add(item1);
            _stub.Collection.Add(item2);
            _stub.Flag.ShouldBe(false);

            var startSignal = new ManualResetEventSlim(false);
            var changer1 = Task.Factory.StartNew(() =>
            {
                startSignal.Wait();
                Thread.Sleep(Rng.Next(0, 10));
                item1.Int = 1;
            });
            
            var changer2 = Task.Factory.StartNew(() =>
            {
                startSignal.Wait();
                Thread.Sleep(Rng.Next(0, 10));
                item2.Int = 1;
            });
            
            var attacher =  Task.Factory.StartNew(() =>
            {
                startSignal.Wait();
                Thread.Sleep(Rng.Next(0, 10));
                _binder.Attach(_stub);
            });
            startSignal.Set();
            Task.WaitAll(changer1, changer2, attacher);
            _stub.Flag.ShouldBe(true);
        }
    }
}