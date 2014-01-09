using System;

namespace Hudl.Mjolnir.Util
{
    internal sealed class NamingUtil
    {
        /// <summary>
        /// For the provided type, returns the last "component" of its assembly's name.
        /// 
        /// Examples (assembly => lastpart):
        /// 
        /// - Foo.Bar.Baz => Baz
        /// - Foo => Foo
        /// 
        /// </summary>
        /// <returns>The type's assembly's last part (after its last ".").</returns>
        public static string GetLastAssemblyPart(Type type)
        {
            var assemblyName = type.Assembly.GetName().Name;
            var dotIndex = assemblyName.LastIndexOf(".", StringComparison.InvariantCulture);
            if (dotIndex >= 0 && dotIndex + 1 < assemblyName.Length)
            {
                assemblyName = assemblyName.Substring(dotIndex + 1);
            }
            return assemblyName;
        }
    }
}
