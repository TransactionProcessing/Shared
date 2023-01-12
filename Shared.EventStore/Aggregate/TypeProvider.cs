namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using DomainDrivenDesign.EventSourcing;
    using General;

    /// <summary>
    /// 
    /// </summary>
    public static class TypeProvider
    {
        #region Fields

        /// <summary>
        /// The default assembly filters
        /// </summary>
        private static readonly List<String> DefaultAssemblyFilters = new List<String>
                                                                      {
                                                                          "Microsoft"
                                                                      };

        #endregion

        #region Methods

        public static void LoadDomainEventsTypeDynamically(List<string> assemblyFilters = null)
        {
            List<Assembly> assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();

            if (assemblyFilters == null) {
                assemblyFilters = TypeProvider.DefaultAssemblyFilters;
            }

            List<Type> source = new List<Type>();
            foreach (string assemblyFilter in assemblyFilters)
            {
                List<Assembly> filteredAssemblies = assemblies.Where(a => a.FullName.Contains(assemblyFilter) == true).ToList();
                foreach (Assembly a in filteredAssemblies)
                {
                    assemblies.Remove(a);
                }
            }
            source.AddRange(assemblies.SelectMany((Func<Assembly, IEnumerable<Type>>)(a => (IEnumerable<Type>)a.GetTypes())));

            foreach (Type type in source.Where((Func<Type, bool>)(t => t.IsSubclassOf(typeof(DomainEvent)))).OrderBy((Func<Type, string>)(e => e.Name)).ToList())
                TypeMap.AddType(type, type.Name);
        }

        #endregion
    }
}