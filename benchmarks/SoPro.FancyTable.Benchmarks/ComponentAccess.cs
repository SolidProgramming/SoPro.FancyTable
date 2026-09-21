using System.Linq.Expressions;
using System.Reflection;

namespace SoPro.FancyTable.Benchmarks;

// Bind once during GlobalSetup: no reflection invocation in the measured path,
// and no benchmark-only public API or dependencies in the production library.
internal static class ComponentAccess
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

    public static TDelegate Bind<TDelegate>(object instance, string name) where TDelegate : Delegate =>
        (instance.GetType().GetMethod(name, Flags)
            ?? throw new MissingMethodException(instance.GetType().FullName, name))
        .CreateDelegate<TDelegate>(instance);

    public static void Set<T>(object instance, string name, T value) =>
        GetField(instance, name).SetValue(instance, value);

    public static T Get<T>(object instance, string name) =>
        (T)GetField(instance, name).GetValue(instance)!;

    public static Action Clear(object instance, string name)
    {
        FieldInfo field = GetField(instance, name);
        var assignment = Expression.Assign(
            Expression.Field(Expression.Constant(instance), field),
            Expression.Constant(null, field.FieldType));
        return Expression.Lambda<Action>(Expression.Block(assignment, Expression.Empty())).Compile();
    }

    private static FieldInfo GetField(object instance, string name) =>
        instance.GetType().GetField(name, Flags)
            ?? throw new MissingFieldException(instance.GetType().FullName, name);
}

public sealed class BenchmarkRow(string name)
{
    public string Name { get; } = name;
    public List<BenchmarkRow> Children { get; } = [];
}
