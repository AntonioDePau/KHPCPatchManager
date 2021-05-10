using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Xe.BinaryMapper
{
    internal partial class RealBinaryMapping
    {
        public T WriteObject<T>(Stream stream, T item, int baseOffset)
            where T : class =>
            (T)WriteObject(new BinaryWriter(stream), item, baseOffset);

        private object WriteObject(BinaryWriter writer, object item, int baseOffset)
        {
            var result = WriteObject(new MappingWriteArgs
            {
                Writer = writer
            }, item, baseOffset);

            if (writer.BaseStream.Position > writer.BaseStream.Length)
                writer.BaseStream.SetLength(writer.BaseStream.Position);

            return result;
        }

        private object WriteObject(MappingWriteArgs args, object item, int baseOffset)
        {
            if (mappings.TryGetValue(item.GetType(), out var mapping))
            {
                mapping.Writer(new MappingWriteArgs
                {
                    Writer = args.Writer,
                    Item = item,
                    DataAttribute = new DataAttribute(),
                });

                return item;
            }

            var properties = item.GetType()
                .GetProperties()
                .Select(x => GetPropertySettings(item.GetType(), x))
                .Where(x => x.DataInfo != null)
                .ToList();

            foreach (var property in properties)
            {
                if (property.DataInfo.Offset.HasValue)
                {
                    var newPosition = baseOffset + property.DataInfo.Offset.Value;
                    if (args.Writer.BaseStream.Position != newPosition)
                        FlushBitField(args);

                    args.Writer.BaseStream.Position = newPosition;
                }

                var value = property.MemberInfo.GetValue(item, BindingFlags.Default, null, null, null);
                args.Count = property.GetLengthFunc?.Invoke(item) ?? property.DataInfo.Count;
                WriteProperty(args, value, property.MemberInfo.PropertyType, property);
            }

            FlushBitField(args);
            return item;
        }

        private void WriteProperty(MappingWriteArgs args, object value, Type type, MyProperty property)
        {
            if (type != typeof(bool))
                FlushBitField(args);

            if (mappings.TryGetValue(type, out var mapping))
            {
                args.Item = value;
                args.DataAttribute = property.DataInfo;
                mapping.Writer(args);
            }
            else if (type.IsEnum)
            {
                var underlyingType = Enum.GetUnderlyingType(type);
                if (!mappings.TryGetValue(underlyingType, out mapping))
                    throw new InvalidDataException($"The enum {type.Name} has an unsupported size.");

                args.DataAttribute = property.DataInfo;
                args.Item = value;
                mapping.Writer(args);
            }
            else if (type.CanEnumerate())
            {
                Type itemType;
                if (type.IsArray)
                {
                    if (value is Array array && array.Rank > 1)
                        throw new NotImplementedException("Arrays with a rank greater than one are not currently supported.");

                    itemType = type.GetElementType();
                }
                else
                {
                    itemType = type.GetGenericArguments().FirstOrDefault();
                }

                if (itemType == null)
                    throw new InvalidDataException($"The list {property.MemberInfo.Name} does not have any specified type.");

                var missing = args.Count;
                foreach (var item in value as IEnumerable)
                {
                    if (missing-- < 1)
                        break;

                    WriteObject(args, item, itemType, property);
                }

                while (missing-- > 0)
                {
                    var item = Activator.CreateInstance(itemType);
                    WriteObject(args, item, itemType, property);
                }
            }
            else
            {
                WriteObject(args.Writer, value, (int)args.Writer.BaseStream.Position);
            }
        }

        internal static void FlushBitField(MappingWriteArgs args)
        {
            if (args.BitIndex <= 0)
                return;

            args.Writer.Write(args.BitData);
            args.BitIndex = 0;
            args.BitData = 0;
        }

        private void WriteObject(MappingWriteArgs args, object value, Type listType, MyProperty property)
        {
            var writer = args.Writer;
            var oldPosition = (int)writer.BaseStream.Position;
            WriteProperty(args, value, listType, property);

            var newPosition = writer.BaseStream.Position;
            var stride = property.DataInfo.Stride;
            if (stride > 0)
            {
                var missingBytes = stride - (int)(newPosition - oldPosition);
                if (missingBytes < 0)
                {
                    throw new InvalidOperationException($"The stride is smaller than {listType.Name} definition.");
                }
                else if (missingBytes > 0)
                {
                    writer.BaseStream.Position += missingBytes;
                }
            }
        }
    }
}
