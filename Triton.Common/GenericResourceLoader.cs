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
    public class GenericResourceLoader : IResourceSerializer
    {
        private readonly ResourceManager _resourceManager;

        public string Extension => ".v";
        public string DefaultFilename => "missing.v";
        public bool SupportsStreaming => false;

        public object DataContractJsonSerializer { get; private set; }

        public GenericResourceLoader(ResourceManager resourceManager)
            => _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));

        public object Create(Type type)
            => Activator.CreateInstance(type);

        public async Task Deserialize(object resource, byte[] data)
        {
            var converter = new ReferenceableConverter(_resourceManager);

            var settings = new JsonSerializerSettings();
            settings.Converters.Add(converter);

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
            => throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }


}
