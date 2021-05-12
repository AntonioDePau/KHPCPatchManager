using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Xe.BinaryMapper
{
    internal partial class RealBinaryMapping
    {
        private class MyProperty
        {
            public PropertyInfo MemberInfo { get; set; }

            public DataAttribute DataInfo { get; set; }

            public Func<object, int> GetLengthFunc { get; set; }
        }

        public T ReadObject<T>(Stream stream, T item, int baseOffset)
            where T : class =>
            (T)ReadRawObject(new BinaryReader(stream), item, baseOffset);

        private object ReadRawObject(BinaryReader reader, object item, int baseOffset)
        {
            if (mappings.TryGetValue(item.GetType(), out var mapping))
            {
                return mapping.Reader(new MappingReadArgs
                {
                    Reader = reader,
                    DataAttribute = new DataAttribute()
                });
            }

            var properties = item.GetType()
                .GetProperties()
                .Select(x => GetPropertySettings(item.GetType(), x))
                .Where(x => x.DataInfo != null)
                .ToList();

            var args = new MappingReadArgs
            {
                Reader = reader
            };

            foreach (var property in properties)
            {
                if (property.DataInfo.Offset.HasValue)
                {
                    var newPosition = baseOffset + property.DataInfo.Offset.Value;
                    if (reader.BaseStream.Position != newPosition + 1)
                        args.BitIndex = 0;

                    reader.BaseStream.Position = newPosition;
                }

                args.Count = property.GetLengthFunc?.Invoke(item) ?? property.DataInfo.Count;
                var value = ReadProperty(args, property.MemberInfo.PropertyType, property);
                property.MemberInfo.SetValue(item, value, BindingFlags.Default, null, null, null);
            }

            args.BitIndex = 0;
            return item;
        }

        private object ReadProperty(MappingReadArgs args, Type type, MyProperty property)
        {
            if (type != typeof(bool))
                args.BitIndex = 0;

            if (mappings.TryGetValue(type, out var mapping))
            {
                args.DataAttribute = property.DataInfo;
                return mapping.Reader(args);
            }
            else if (type.IsEnum)
            {
                var underlyingType = Enum.GetUnderlyingType(type);
                if (!mappings.TryGetValue(underlyingType, out mapping))
                    throw new InvalidDataException($"The enum {type.Name} has an unsupported size.");

                args.DataAttribute = property.DataInfo;
                return mapping.Reader(args);
            }
            else if (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)))
            {
                var listType = type.GetGenericArguments().FirstOrDefault();
                if (listType == null)
                    throw new InvalidDataException($"The list {property.MemberInfo.Name} does not have any specified type.");

                var addMethod = type.GetMethod("Add");
                var list = Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));

                for (int i = 0; i < args.Count; i++)
                {
                    var oldPosition = (int)args.Reader.BaseStream.Position;

                    var item = ReadProperty(args, listType, property);
                    addMethod.Invoke(list, new[] { item });

                    var newPosition = args.Reader.BaseStream.Position;
                    args.Reader.BaseStream.Position += Math.Max(0, property.DataInfo.Stride - (newPosition - oldPosition));
                }

                return list;
            }
            else if (type.IsArray)
            {
                if (type.GetArrayRank() > 1)
                    throw new NotImplementedException("Arrays with a rank greater than one are not currently supported.");

                var arrayType = type.GetElementType();
                if (arrayType == null)
                    throw new InvalidDataException($"Unable to get the underlying type of {type.Name}.");

                var array = Array.CreateInstance(arrayType, args.Count);
                if (arrayType.IsEnum)
                {
                    for (var i = 0; i < args.Count; i++)
                    {
                        var oldPosition = (int)args.Reader.BaseStream.Position;

                        var item = ReadProperty(args, arrayType, property);
                        array.SetValue(Enum.ToObject(arrayType, item), i);

                        var newPosition = args.Reader.BaseStream.Position;
                        args.Reader.BaseStream.Position += Math.Max(0, property.DataInfo.Stride - (newPosition - oldPosition));
                    }
                }
                else
                {
                    for (var i = 0; i < args.Count; i++)
                    {
                        var oldPosition = (int)args.Reader.BaseStream.Position;

                        var item = ReadProperty(args, arrayType, property);
                        array.SetValue(item, i);

                        var newPosition = args.Reader.BaseStream.Position;
                        args.Reader.BaseStream.Position += Math.Max(0, property.DataInfo.Stride - (newPosition - oldPosition));
                    }
                }

                return array;
            }
            else
            {
                return ReadRawObject(args.Reader, Activator.CreateInstance(type), (int)args.Reader.BaseStream.Position);
            }
        }
    }
}
