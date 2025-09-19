using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Serialisation;

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

[ExcludeFromCodeCoverage]
public class JsonIgnoreAttributeIgnorerContractResolver : DefaultContractResolver
{
    #region Methods

    /// <summary>
    /// Creates a <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> for the given <see cref="T:System.Reflection.MemberInfo" />.
    /// </summary>
    /// <param name="member">The member to create a <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> for.</param>
    /// <param name="memberSerialization">The member's parent <see cref="T:Newtonsoft.Json.MemberSerialization" />.</param>
    /// <returns>
    /// A created <see cref="T:Newtonsoft.Json.Serialization.JsonProperty" /> for the given <see cref="T:System.Reflection.MemberInfo" />.
    /// </returns>
    protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(MemberInfo member,
                                                                                 MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        property.Ignored = false; // Here is the magic
        return property;
    }

    #endregion
}