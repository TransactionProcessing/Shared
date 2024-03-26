namespace Shared.EventStore.Aggregate
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using DomainDrivenDesign.EventSourcing;
    using General;

    /// <summary>
    /// 
    /// </summary>
    public static class TypeProvider
    {
        #region Methods

        public static void LoadDomainEventsTypeDynamically(Assembly[] assemblies = null)
        {
            if (assemblies == null){
                // Add a default
                assemblies = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory, "*DomainEvents*.dll")
                                      .Select(x => Assembly.Load(AssemblyName.GetAssemblyName(x))).ToArray();
            }

            IEnumerable<Type> allTypes = assemblies.SelectMany(a => a.GetTypes());

            List<Type> filteredTypes = allTypes
                                       .Where(t => t.IsSubclassOf(typeof(DomainEvent)))
                                       .OrderBy(e => e.Name).ToList();

            foreach (Type type in filteredTypes)
            {
                TypeMap.AddType(type, type.Name);
            }
        }

        #endregion
    }
}