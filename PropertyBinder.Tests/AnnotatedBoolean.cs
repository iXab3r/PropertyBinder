namespace PropertyBinder.Tests
{
    internal readonly struct AnnotatedBoolean
    {
        public AnnotatedBoolean(bool value, string annotation)
        {
            Value = value;
            Annotation = annotation;
        }

        public bool Value { get; }

        public string Annotation { get; }

        public static implicit operator bool(AnnotatedBoolean source) => source.Value;

        public static implicit operator AnnotatedBoolean(bool value) => new AnnotatedBoolean(value, "Implicit conversion");
        
        public static implicit operator bool?(AnnotatedBoolean source) => source.Value;

        public static implicit operator AnnotatedBoolean(bool? value) => new AnnotatedBoolean(value ?? false, $"Implicit conversion from nullable, hasValue: {value.HasValue}, value: {value}");
    }
}