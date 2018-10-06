using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Common
{
    public class GenericResourceSerializer : IResourceSerializer
    {
        private readonly ResourceManager _resourceManager;

        public string Extension => ".v";
        public string DefaultFilename => "missing.v";
        public bool SupportsStreaming => false;

        public object DataContractJsonSerializer { get; private set; }

        public GenericResourceSerializer(ResourceManager resourceManager)
            => _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));

        public object Create(Type type)
            => Activator.CreateInstance(type);

        public async Task Deserialize(object resource, byte[] data)
        {
            var converter = new ReferenceableConverter(_resourceManager);

            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                Converters = new List<JsonConverter>
                {
                    new ReferenceableConverter(_resourceManager),
                    new Vector2Converter(),
                    new Vector3Converter(),
                    new Vector4Converter(),
                    new QuaternionConverter()
                }
            };

            JsonConvert.PopulateObject(Encoding.UTF8.GetString(data), resource, settings);

            // Load and patch resources
            foreach (var (path, type, name) in converter.ResourcesToLoad)
            {
                var referencedResource = await _resourceManager.LoadAsync(type, name);

                var target = resource;
                var bits = path.Split('.');
                for (var i = 0; i < bits.Length - 1; i++)
                {
                    var getter = target.GetType().GetProperty(bits[i]);
                    target = getter.GetValue(target, null);
                }

                var setter = target.GetType().GetProperty(bits[bits.Length - 1]);
                setter.SetValue(target, referencedResource, null);
            }
        }

        private Type GetFieldOrPropertyType(Type type, string name)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property != null)
            {
                return property.CanWrite && property.CanRead ? property.PropertyType : null;
            }

            var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
            return !field.IsInitOnly ? field.FieldType : null;
        }

        public void Unload(object resource)
        {
            var type = resource.GetType();
            foreach (var property in type.GetProperties())
            {
                var propertyType = property.PropertyType;
                if (!propertyType.IsClass || propertyType == typeof(string) || !property.CanRead || !property.CanWrite)
                    continue;

                var value = property.GetValue(resource, null);

                if (_resourceManager.IsManaged(value))
                {
                    _resourceManager.Unload(value);
                }
                else
                {
                    Unload(value);
                }
            }
        }

        public byte[] Serialize(object resource)
        {
            var settings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.Objects,
                Converters = new List<JsonConverter>
                {
                    new ReferenceableConverter(_resourceManager),
                    new Vector2Converter(),
                    new Vector3Converter(),
                    new Vector4Converter(),
                    new QuaternionConverter()
                }
            };

            var data = JsonConvert.SerializeObject(resource, settings);
            return Encoding.UTF8.GetBytes(data);
        }
    }

    class ReferenceableConverter : JsonConverter
    {
        private readonly ResourceManager _resourceManager;
        public List<(string path, Type type, string name)> ResourcesToLoad { get; } = new List<(string path, Type type, string name)>();

        public ReferenceableConverter(ResourceManager resourceManager)
            => _resourceManager = resourceManager;

        private bool _canConvert = true;

        public override bool CanConvert(Type objectType)
        {
            if (objectType == typeof(string) || !objectType.IsClass)
            {
                _canConvert = true;
                return false;
            }

            if (!_canConvert)
            {
                _canConvert = true;
                return false;
            }
            else
            {
                return true;
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.String && objectType.IsClass && objectType != typeof(string))
            {
                // We got a reference to a resource boyz!!
                var name = reader.Value.ToString();
                ResourcesToLoad.Add((reader.Path, objectType, name));

                return null;
            }
            else
            {
                _canConvert = false;
                return serializer.Deserialize(reader, objectType);
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var type = value.GetType();
            if (type != typeof(string) && type.IsClass && _resourceManager.IsManaged(value))
            {
                // We got a resource reference to serialize!
                var (name, _) = _resourceManager.GetResourceProperties(value);
                writer.WriteValue(name);
            }
            else
            {
                _canConvert = false;
                serializer.Serialize(writer, value);
            }
        }
    }

    class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 ReadJson(JsonReader reader, Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
           => StringConverter.ParseVector2(reader.Value.ToString());

        public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
            => writer.WriteValue(StringConverter.ToString(value));
    }

    class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
           => StringConverter.ParseVector3(reader.Value.ToString());

        public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
            => writer.WriteValue(StringConverter.ToString(value));
    }

    class Vector4Converter : JsonConverter<Vector4>
    {
        public override Vector4 ReadJson(JsonReader reader, Type objectType, Vector4 existingValue, bool hasExistingValue, JsonSerializer serializer)
           => StringConverter.ParseVector4(reader.Value.ToString());

        public override void WriteJson(JsonWriter writer, Vector4 value, JsonSerializer serializer)
            => writer.WriteValue(StringConverter.ToString(value));
    }

    class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
            => StringConverter.ParseQuaternion(reader.Value.ToString());

        public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
            => writer.WriteValue(StringConverter.ToString(value));
    }
}
