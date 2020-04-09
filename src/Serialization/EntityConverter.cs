using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;

namespace SimpleECS
{
    class EntityConverter : JsonConverter<Entity>
    {
        public Dictionary<string, Entity> stringToEntity = new Dictionary<string, Entity>();
        public Dictionary<Entity, string> entityToString = new Dictionary<Entity, string>();
        public override Entity ReadJson(JsonReader reader, Type objectType, Entity existingValue, bool hasExistingValue, JsonSerializer serializer) 
            => stringToEntity[(reader.Value as string) ?? throw new Exception("Entities must be given as strings.")];

        public override void WriteJson(JsonWriter writer, Entity value, JsonSerializer serializer)
            => writer.WriteValue(entityToString[value]);
    }
}
