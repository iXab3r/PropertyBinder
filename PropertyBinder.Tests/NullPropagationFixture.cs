﻿using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Shouldly;

namespace PropertyBinder.Tests
{
    [TestFixture]
    internal class NullPropagationFixture : BindingsFixture
    {
        [Test]
        public void ShouldPropagateNulls()
        {
            _binder.Bind(x => (int?) x.String.Length).PropagateNullValues().To(x => x.NullableInt);
            using (_binder.Attach(_stub))
            {
                _stub.NullableInt.ShouldBe(null);
                _stub.String = "a";
                _stub.NullableInt.ShouldBe(1);
                _stub.String = "";
                _stub.NullableInt.ShouldBe(0);
                _stub.String = null;
                _stub.NullableInt.ShouldBe(null);
            }
        }
        
        [Test]
        public void ShouldPropagateNullInterface()
        {
            _binder.Bind(x => (bool?) x.NestedInterface.Flag).PropagateNullValues().To((x,v) => x.NullableBool = v);
            using (_binder.Attach(_stub))
            {
                _stub.NullableBool.ShouldBe(null);
                _stub.NestedInterface = new UniversalStub();
                _stub.NullableBool.ShouldBe(false);
                _stub.NestedInterface.Flag = true;
                _stub.NullableBool.ShouldBe(true);
            }
        }

        [Test]
        public void ShouldPropagateNullsInCoalesceOperator()
        {
            _binder.Bind(x => x.Nested.String ?? x.String).PropagateNullValues().To(x => x.String2);
            using (_binder.Attach(_stub))
            {
                _stub.String2.ShouldBe(null);
                _stub.String = "a";
                _stub.String2.ShouldBe("a");
                _stub.Nested = new UniversalStub { String = "b" };
                _stub.String2.ShouldBe("b");
            }
        }

        [Test]
        public void ShouldPropagateNullsInMethodsAndStructs()
        {
            _binder.Bind(x => x.Nested.String + x.Pair.Value.String).PropagateNullValues().To(x => x.String2);
            using (_binder.Attach(_stub))
            {
                _stub.String2.ShouldBe(string.Empty);
                _stub.Nested = new UniversalStub { String = "a" };
                _stub.String2.ShouldBe("a");
                _stub.Pair = new KeyValuePair<string, UniversalStub>(string.Empty, new UniversalStub { String = "1" });
                _stub.String2.ShouldBe("a1");
                _stub.Pair.Value.String = "2";
                _stub.String2.ShouldBe("a2");
            }
        }

        [Test]
        public void ShouldPropagateNullsWhenBindingToActions()
        {
            _binder.Bind(x => (int?) x.String.Length).PropagateNullValues().To((x, v) => x.NullableInt = v);
            using (_binder.Attach(_stub))
            {
                _stub.NullableInt.ShouldBe(null);
                _stub.String = "a";
                _stub.NullableInt.ShouldBe(1);
                _stub.String = "";
                _stub.NullableInt.ShouldBe(0);
                _stub.String = null;
                _stub.NullableInt.ShouldBe(null);
            }
        }

        [Test]
        public void ShouldPropagateNullsThroughStaticMethods()
        {
            _binder.Bind(x => (int?) Math.Pow(x.Nested.Int, 2)).PropagateNullValues().To(x => x.NullableInt);
            using (_binder.Attach(_stub))
            {
                _stub.NullableInt.ShouldBe(null);
                _stub.Nested = new UniversalStub { Int = 5 };
                _stub.NullableInt.ShouldBe(25);
                _stub.Nested = new UniversalStub { Int = 3 };
                _stub.NullableInt.ShouldBe(9);
                _stub.Nested = null;
                _stub.NullableInt.ShouldBe(null);
            }
        }

        [Test]
        public void ShouldPropagateNullsThroughStaticProperties()
        {
            _binder.Bind(x => x.Nested.String ?? string.Empty).PropagateNullValues().To(x => x.String);
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe(string.Empty);
                _stub.Nested = new UniversalStub();
                _stub.String.ShouldBe(string.Empty);
                _stub.Nested.String = "a";
                _stub.String.ShouldBe("a");
                _stub.Nested = null;
                _stub.String.ShouldBe(string.Empty);
            }
        }

        [Test]
        public void ShouldPropagateNullsThroughAggregations()
        {
            _binder.Bind(x => x.Collection.FirstOrDefault(y => y.Int == 1).String).PropagateNullValues().To(x => x.String);
            using (_binder.Attach(_stub))
            {
                _stub.String.ShouldBe(null);
                _stub.Collection.Add(new UniversalStub { Int = 1, String = "a" });
                _stub.String.ShouldBe("a");
                _stub.Collection.Clear();
                _stub.String.ShouldBe(null);
            }
        }
    }
}
