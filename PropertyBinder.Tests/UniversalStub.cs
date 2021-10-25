using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace PropertyBinder.Tests
{
    internal interface IUniversalStub : INotifyPropertyChanged
    {
        int Int { get; set; }
        int? NullableInt { get; set; }
        bool? NullableBool { get; set; }
        decimal Decimal { get; set; }
        double Double { get; set; }
        bool Flag { get; set; }
        AnnotatedBoolean AnnotatedFlag { get; set; }
        string String { get; set; }
        string String2 { get; set; }
        DateTime DateTime { get; set; }
        UniversalStub Nested { get; set; }
        KeyValuePair<string, UniversalStub> Pair { get; set; }
        ICommand Command { get; set; }
        ObservableCollection<UniversalStub> Collection { get; set; }
        IEnumerable<UniversalStub> EnumerableCollection { get; set; }
        ObservableDictionary<string> Dictionary { get; set; }
        int SubscriptionsCount { get; }
    }

    internal class UniversalStub : IUniversalStub
    {
        public string StringField;

        public UniversalStub StubField;

        public UniversalStub()
        {
            Collection = new ObservableCollection<UniversalStub>();
        }

        public IUniversalStub NestedInterface { get; set; }

        public int Int { get; set; }

        public int? NullableInt { get; set; }

        public bool? NullableBool { get; set; }

        public decimal Decimal { get; set; }

        public double Double { get; set; }

        public bool Flag { get; set; }
        
        public AnnotatedBoolean AnnotatedFlag { get; set; }

        public string String { get; set; }

        public string String2 { get; set; }

        public DateTime DateTime { get; set; }

        public UniversalStub Nested { get; set; }

        public KeyValuePair<string, UniversalStub> Pair { get; set; }

        public ICommand Command { get; set; }

        public ObservableCollection<UniversalStub> Collection { get; set; }

        public IEnumerable<UniversalStub> EnumerableCollection { get; set; }

        public ObservableDictionary<string> Dictionary { get; set; }

        public int SubscriptionsCount
        {
            get
            {
                if (PropertyChanged == null)
                {
                    return 0;
                }

                return PropertyChanged.GetInvocationList().Length;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event EventHandler TestEvent;

        public void RaiseTestEvent()
        {
            TestEvent?.Invoke(this, EventArgs.Empty);
        }

        public void HandleTestEvent(object sender, EventArgs args)
        {
        }

        public T ReturnValue<T>(T value)
        {
            return value;
        }
    }
}