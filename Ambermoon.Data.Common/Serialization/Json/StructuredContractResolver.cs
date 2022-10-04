using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ambermoon.Data.Serialization.Json
{
    public class StructuredContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            static bool CanWrite(JsonProperty jsonProperty)
            {
                if (jsonProperty.Writable)
                    return true;

                return jsonProperty.AttributeProvider?.GetAttributes(typeof(JsonPropertyAttribute), true)?.Any() == true ||
                    jsonProperty.AttributeProvider?.GetAttributes(typeof(JsonConverterAttribute), true)?.Any() == true;
            }

            var props = base.CreateProperties(type, memberSerialization).Where(p => p.Readable && CanWrite(p));
            var types = new List<Type>() { type };

            while (type.BaseType != null && type.BaseType != typeof(object))
            {
                types.Insert(0, type.BaseType);
                type = type.BaseType;
            }
            
            var propsByType = props.GroupBy(p => p.DeclaringType).OrderBy(g => types.IndexOf(g.Key));

            return propsByType.SelectMany(p => p).ToList();
        }
    }
}
