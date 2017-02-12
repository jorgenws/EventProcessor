using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EventProcessor
{
    internal class DependenciesFactory
    {
        public IEnumerable<IDependencies> Create(IEnumerable<Assembly> assemblies)
        {
            var finder = new TypeFinder();
            return finder.FindTypesOf<IDependencies>(assemblies).Select(c => Create(c.AsType()));
        }

        private static IDependencies Create(Type type)
        {
            var methodInfo = typeof(DependenciesFactory).GetTypeInfo().GetDeclaredMethod(nameof(CreateDependency));
            var genericMethod = methodInfo.MakeGenericMethod(new[] { type });

            Func<IDependencies> dependencies = Expression.Lambda<Func<IDependencies>>(Expression.Call(null, genericMethod)).Compile();

            return dependencies();
        }

        private static IDependencies CreateDependency<T>() where T : IDependencies, new()
        {
            return new T();
        }
    }
}
