using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SimpleECS
{
    static class SceneJsonSerializer
    {
        public static string ToJson(Scene scene, bool indent = true)
        {
            scene.InsertNewComponents();
            var allTypes = scene.archetypes.Values
                .SelectMany(archetype => archetype.arrays.Keys)
                .Distinct()
                .Where(type => typeof(ISerializableComponent).IsAssignableFrom(type));

            var stringToType = AssignFreshNames(allTypes, type => type.Name);
            var typeToString = Invert(stringToType);
            var header = stringToType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.AssemblyQualifiedName);

            var stringToEntity = AssignFreshNames(scene, entity => entity.Has<NameComp>() ? (entity.Get<NameComp>().Name ?? "<null>") : "entity");
            var entityToString = Invert(stringToEntity);

            var serializedEntities = scene.ToDictionary(
                entity => entityToString[entity],
                entity => entity.GetComponentsUnsafe()
                    .Where(comp => typeToString.ContainsKey(comp.GetType()))
                    .ToDictionary(comp => typeToString[comp.GetType()])
            );

            var serializedObject = new
            {
                types = header,
                entities = serializedEntities
            };

            EntityConverter entityConverter = new EntityConverter();
            entityConverter.entityToString = entityToString;
            JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings();
            jsonSerializerSettings.Converters.Add(entityConverter);
            jsonSerializerSettings.Formatting = indent ? Formatting.Indented : Formatting.None;
            return JsonConvert.SerializeObject(serializedObject, jsonSerializerSettings);
        }

        public static Scene FromJson(string json)
        {
            Scene scene = new Scene();
            var jsonObj = JToken.Parse(json);

            var types = (jsonObj["types"] as JObject)?.Properties()
                .ToDictionary(
                    prop => prop.Name, 
                    prop => Type.GetType(prop.Value.ToObject<string>() 
                        ?? throw new Exception("Expected a string as a type name")))
                ?? throw new Exception("A types property is expected");

            var entitiesJson = (jsonObj["entities"] as JObject) 
                ?? throw new Exception("An entities property is expected");
            var stringToEntity = entitiesJson.Properties()
                .ToDictionary(
                    prop => prop.Name,
                    prop => scene.CreateEntity()
                );
            var converter = new EntityConverter();
            converter.stringToEntity = stringToEntity;
            var serializer = new JsonSerializer();
            serializer.Converters.Add(converter);
            
            foreach(var entityJson in entitiesJson.Properties())
            {
                var entity = stringToEntity[entityJson.Name];
                var componentsJson = (entityJson.Value as JObject ?? throw new Exception("Entities must be JObjects")).Properties();
                foreach(var componentJson in componentsJson)
                {
                    var type = types.GetValueOrDefault(componentJson.Name) ?? throw new Exception("Type is not defined");
                    var component = componentJson.Value.ToObject(type, serializer) ?? throw new Exception("Failed component parsing");
                    entity.Add(component);
                }
            }

            scene.InsertNewComponents();
            return scene;
        }

        static Dictionary<string, T> AssignFreshNames<T>(IEnumerable<T> items, Func<T, string> nameGenerator)
        {
            Dictionary<string, T> stringToItem = new Dictionary<string, T>();
            foreach (var item in items)
            {
                string baseName = nameGenerator(item);
                string usedName = baseName;
                int postfix = 0;
                while (stringToItem.ContainsKey(usedName))
                {
                    postfix++;
                    usedName = $"{baseName}#{postfix}";
                }
                stringToItem.Add(usedName, item);
            }
            return stringToItem;
        }

        static Dictionary<TValue, TKey> Invert<TKey, TValue>(Dictionary<TKey, TValue> input)
            where TKey : notnull
            where TValue : notnull
            => input.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
    }
}
