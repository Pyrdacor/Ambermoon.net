using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ambermoon.Data.Serialization.Json
{
    public class IgnoreConverterContractResolver : StructuredContractResolver
    {
        protected override JsonConverter ResolveContractConverter(Type objectType)
        {
            return null;
        }
    }
}
