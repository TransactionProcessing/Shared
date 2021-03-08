namespace Shared.General
{
    using System;
    using System.Collections.Generic;

    public static class TypeMap
    {
        #region Fields

        /// <summary>
        /// The map
        /// </summary>
        public static readonly Dictionary<Type, String> Map = new();

        /// <summary>
        /// The reverse map
        /// </summary>
        public static readonly Dictionary<String, Type> ReverseMap = new();

        #endregion

        #region Methods

        public static void AddType<T>(String name)
        {
            TypeMap.ReverseMap[name] = typeof(T);
            TypeMap.Map[typeof(T)] = name;
        }

        public static void AddType(Type type, String name)
        {
            TypeMap.ReverseMap[name] = type;
            TypeMap.Map[type] = name;
        }

        public static Type GetType(String typeName) => TypeMap.ReverseMap[typeName];

        public static String GetTypeName<T>() => TypeMap.Map[typeof(T)];

        public static String GetTypeName(Object o) => TypeMap.Map[o.GetType()];

        #endregion
    }
}