using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Dynamic;
using System.Linq;

namespace Ambermoon.Data.Serialization.Json
{
    public class EventProviderConverter<T> : JsonConverter<T> where T : class, IEventProvider, new()
    {
        class EventValueProvider : IValueProvider
        {
            readonly IValueProvider valueProvider;
            readonly IEventProvider eventProvider;

            public EventValueProvider(IValueProvider valueProvider, IEventProvider eventProvider)
            {
                this.valueProvider = valueProvider;
                this.eventProvider = eventProvider;
            }

            public object GetValue(object target)
            {
                var value = valueProvider.GetValue(target) as Event;
                return Tuple.Create(eventProvider, value);
            }

            public void SetValue(object target, object value)
            {
                var @event = value as Tuple<IEventProvider, Event>;
                valueProvider.SetValue(target, @event.Item2);
            }
        }

        public override T ReadJson(JsonReader reader, Type objectType, T existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.StartObject)
            {
                var obj = existingValue ?? new T();
                JObject item = JObject.Load(reader);

                serializer.Populate(item.CreateReader(), obj);

                var eventIndices = item["EventList"].Value<int[]>();

                obj.EventList.Clear();

                foreach (var index in eventIndices)
                    obj.EventList.Add(obj.Events[index]);

                return obj;
            }

            throw new JsonSerializationException(); // TODO?
        }

        public override void WriteJson(JsonWriter writer, T value, JsonSerializer serializer)
        {
            JObject obj = JObject.FromObject(value, new JsonSerializer { ContractResolver = new IgnoreConverterContractResolver()});

            obj.Property("Events").
            //obj.Add("EventList", JToken.FromObject(value.EventList.Select(e => value.Events.IndexOf(e)).ToArray()));
            obj.WriteTo(writer);
        }
    }
}
