using System;
using System.Linq;
using System.Reflection;

namespace Demos
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public class IgnoreAttribute : Attribute
    { }

    public class Program
    {
        public static void Main()
        {
            run_all_demos();
        }

        private static void run_all_demos()
        {
            Assembly
                .GetExecutingAssembly()
                .GetTypes()
                .Select(t => t.GetMethod("Run", BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance,
                                         null, Type.EmptyTypes, null))
                .Where(m => m != null && m.ReturnType == typeof(void) && !m.DeclaringType.IsGenericType && !m.IsGenericMethod
                            && (m.IsStatic || m.DeclaringType.GetConstructor(Type.EmptyTypes) != null)
                            && m.GetCustomAttributes(false).All(a => a.GetType() != typeof(IgnoreAttribute)))
                .Aggregate(new Action(() => { }),
                           (a, m) => a + (() => m.Invoke(m.IsStatic ? null : Activator.CreateInstance(m.DeclaringType),
                                                         new object[0])))
                .Invoke();
        }
    }
}
