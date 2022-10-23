using System;
using System.Linq.Expressions;
using System.Reflection;

namespace PropertyBinder.Engine;

internal sealed class BindableMember
{
    private readonly Func<Type, Delegate> createSelector;

    public BindableMember(PropertyInfo property)
    {
        Name = string.Intern(property.Name);
        createSelector = _ => CreatePropertySelector(property.DeclaringType, property);
        CanSubscribe = !property.IsDefined(typeof(ImmutableAttribute));
    }

    public BindableMember(MemberInfo field)
    {
        Name = string.Intern(field.Name);
        createSelector = _ => CreateMemberSelector(field.DeclaringType, field);
    }

    public BindableMember(string index)
    {
        Name = string.Intern(index);
        createSelector = t => CreateIndexerSelector(t, index);
        CanSubscribe = true;
    }

    public string Name { get; }

    public bool CanSubscribe { get; }

    public Delegate CreateSelector(Type parentType)
    {
        var result = createSelector(parentType);
        return result;
    }

    private static Delegate CreateMemberSelector(Type parentType, MemberInfo member)
    {
        var parameter = Expression.Parameter(parentType);
        return Binder.ExpressionCompiler.Compile(Expression.Lambda(Expression.MakeMemberAccess(parameter, member), parameter));
    }

    private static Delegate CreatePropertySelector(Type parentType, PropertyInfo property)
    {
        if (parentType.IsValueType)
        {
            return CreateMemberSelector(parentType, property);
        }

        var propertyGetter = property.GetGetMethod(true);
        var delegateType = typeof(Func<,>).MakeGenericType(parentType, property.PropertyType);
        var result = propertyGetter.CreateDelegate(delegateType);
        return result;
    }

    private static Delegate CreateIndexerSelector(Type parentType, string index)
    {
        const string getterName = "get_Item";
        var getter = parentType.GetMethod(getterName);
        if (getter == null)
        {
            throw new ArgumentException($"Failed to resolve getter method {getterName} of type {parentType}, index {index}");
        }
        var parameter = Expression.Parameter(parentType);
        return Binder.ExpressionCompiler.Compile(Expression.Lambda(
            Expression.Call(
                parameter,
                getter,
                Expression.Constant(index)),
            parameter));
    }

    public override string ToString()
    {
        return $"BindableMember {Name}";
    }
}