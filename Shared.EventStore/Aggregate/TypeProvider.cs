namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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

        /// <summary>
        /// Loads the domain events type dynamically.
        /// </summary>
        /// <param name="assemblyFilters">The assembly filters.</param>
        public static void LoadDomainEventsTypeDynamically(List<String> assemblyFilters = null)
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            if (assemblyFilters == null)
                assemblyFilters = TypeProvider.DefaultAssemblyFilters;

            IEnumerable<Type> allTypes = null;
            foreach (String filter in assemblyFilters)
            {
                allTypes = assemblies.Where(a => a.FullName.Contains(filter) == false).SelectMany(a => a.GetTypes());
            }

            var filteredTypes = allTypes.Where(t => t.IsSubclassOf(typeof(DomainEvent)) || t.IsSubclassOf(typeof(DomainEventRecord.DomainEvent))).OrderBy(e => e.Name)
                                        .ToList();

            foreach (Type type in filteredTypes)
            {
                TypeMap.AddType(type, type.Name);
            }
        }

        #endregion
    }
}