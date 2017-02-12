using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventProcessor
{
    internal class TypeFinder
    {
        public IEnumerable<TypeInfo> FindTypesOf<T>(IEnumerable<Assembly> assemblies)
        {
            return assemblies.SelectMany(c => c.DefinedTypes.Where(d => d.ImplementedInterfaces.Contains(typeof(T))).Union(c.DefinedTypes.Where(d => IsOfTypeRecursivly<T>(d))))
                             .Where(c => !c.IsAbstract)
                             .Select(c => c);
        }

        private bool IsOfTypeRecursivly<T>(TypeInfo typeInfo)
        {
            if (typeInfo == typeof(T).GetTypeInfo())
                return true;

            if (typeInfo.BaseType != null)
                return IsOfTypeRecursivly<T>(typeInfo.BaseType.GetTypeInfo());

            return false;
        }
    }
}
