using System.Collections.Generic;

namespace PropertyBinder.Tests
{
    internal class UniversalStubEx : UniversalStub
    {
        public string String3 { get; set; }

        public UniversalStubEx NestedEx { get; set; }
        
        public IInheritedList<string> ValueList { get; set; }
    }
    
    /// <summary>
    /// This is needed to test specific case when binding accesses Collection property of inherited ISampleList, this was not working initially
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IInheritedList<T> : ISampleList<T>
    {
    }

    internal interface ISampleList<T>
    {
        IList<string> List { get; }

        object ReferenceType { get; }
        
        int this[string key] { get; }
        
        int ValueType { get; }
    }
}