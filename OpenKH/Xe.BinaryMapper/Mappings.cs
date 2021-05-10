using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xe.BinaryMapper
{
    internal static class Mappings
    {
        internal static Dictionary<Type, MappingDefinition> DefaultMapping(Encoding encoding) => new Dictionary<Type, MappingDefinition>
        {
            [typeof(bool)] = new MappingDefinition
            {
                Writer = x =>
                {
                    if (x.BitIndex >= 8)
                        RealBinaryMapping.FlushBitField(x);
                    if (x.DataAttribute.BitIndex >= 0)
                        x.BitIndex = x.DataAttribute.BitIndex;

                    if (x.Item is bool bit && bit)
                        x.BitData |= (byte)(1 << x.BitIndex);

                    x.BitIndex++;
                },
                Reader = x =>
                {
                    if (x.BitIndex >= 8)
                        x.BitIndex = 0;
                    if (x.BitIndex == 0)
                        x.BitData = x.Reader.ReadByte();
                    if (x.DataAttribute.BitIndex >= 0)
                        x.BitIndex = x.DataAttribute.BitIndex;

                    return (x.BitData & (1 << x.BitIndex++)) != 0;
                }
            },
            [typeof(byte)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((byte)x.Item),
                Reader = x => x.Reader.ReadByte()
            },
            [typeof(sbyte)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((sbyte)x.Item),
                Reader = x => x.Reader.ReadSByte()
            },
            [typeof(short)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((short)x.Item),
                Reader = x => x.Reader.ReadInt16()
            },
            [typeof(ushort)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((ushort)x.Item),
                Reader = x => x.Reader.ReadUInt16()
            },
            [typeof(int)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((int)x.Item),
                Reader = x => x.Reader.ReadInt32()
            },
            [typeof(uint)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((uint)x.Item),
                Reader = x => x.Reader.ReadUInt32()
            },
            [typeof(long)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((long)x.Item),
                Reader = x => x.Reader.ReadInt64()
            },
            [typeof(ulong)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((ulong)x.Item),
                Reader = x => x.Reader.ReadUInt64()
            },
            [typeof(float)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((float)x.Item),
                Reader = x => x.Reader.ReadSingle()
            },
            [typeof(double)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((double)x.Item),
                Reader = x => x.Reader.ReadDouble()
            },
            [typeof(TimeSpan)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write(((TimeSpan)x.Item).Ticks),
                Reader = x => new TimeSpan(x.Reader.ReadInt64())
            },
            [typeof(DateTime)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write(((DateTime)x.Item).Ticks),
                Reader = x => new DateTime(x.Reader.ReadInt64())
            },
            [typeof(string)] = new MappingDefinition
            {
                Writer = x => WriteString(x.Writer, (string)x.Item, encoding, x.Count),
                Reader = x => ReadString(x.Reader, encoding, x.Count)
            },
            [typeof(byte[])] = new MappingDefinition
            {
                Writer = x =>
                {
                    var data = (byte[])x.Item;
                    var bytesToWrite = Math.Min(data.Length, x.Count);
                    x.Writer.Write(data, 0, bytesToWrite);

                    var remainingBytes = x.Count - bytesToWrite;
                    if (remainingBytes > 0)
                        x.Writer.Write(new byte[remainingBytes], 0, remainingBytes);
                },
                Reader = x => x.Reader.ReadBytes(x.Count)
            },
        };

        internal static Dictionary<Type, MappingDefinition> BigEndianMapping(Encoding encoding) => new Dictionary<Type, MappingDefinition>
        {
            [typeof(bool)] = new MappingDefinition
            {
                Writer = x =>
                {
                    if (x.BitIndex >= 8)
                        RealBinaryMapping.FlushBitField(x);
                    if (x.DataAttribute.BitIndex >= 0)
                        x.BitIndex = x.DataAttribute.BitIndex;

                    if (x.Item is bool bit && bit)
                        x.BitData |= (byte)(1 << x.BitIndex);

                    x.BitIndex++;
                },
                Reader = x =>
                {
                    if (x.BitIndex >= 8)
                        x.BitIndex = 0;
                    if (x.BitIndex == 0)
                        x.BitData = x.Reader.ReadByte();
                    if (x.DataAttribute.BitIndex >= 0)
                        x.BitIndex = x.DataAttribute.BitIndex;

                    return (x.BitData & (1 << x.BitIndex++)) != 0;
                }
            },
            [typeof(byte)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((byte)x.Item),
                Reader = x => x.Reader.ReadByte()
            },
            [typeof(sbyte)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((sbyte)x.Item),
                Reader = x => x.Reader.ReadSByte()
            },
            [typeof(short)] = new MappingDefinition
            {
                Writer = x =>
                {
                    x.Writer.Write((byte)(((short)x.Item >> 8) & 0xff));
                    x.Writer.Write((byte)(((short)x.Item >> 0) & 0xff));
                },
                Reader = x =>
                {
                    var data = x.Reader.ReadBytes(2);
                    Array.Reverse(data);
                    return BitConverter.ToInt16(data, 0);
                }
            },
            [typeof(ushort)] = new MappingDefinition
            {
                Writer = x =>
                {
                    x.Writer.Write((byte)(((ushort)x.Item >> 8) & 0xff));
                    x.Writer.Write((byte)(((ushort)x.Item >> 0) & 0xff));
                },
                Reader = x =>
                {
                    var data = x.Reader.ReadBytes(2);
                    Array.Reverse(data);
                    return BitConverter.ToUInt16(data, 0);
                }
            },
            [typeof(int)] = new MappingDefinition
            {
                Writer = x =>
                {
                    x.Writer.Write((byte)(((int)x.Item >> 24) & 0xff));
                    x.Writer.Write((byte)(((int)x.Item >> 16) & 0xff));
                    x.Writer.Write((byte)(((int)x.Item >> 8) & 0xff));
                    x.Writer.Write((byte)(((int)x.Item >> 0 & 0xff)));
                },
                Reader = x =>
                {
                    var data = x.Reader.ReadBytes(4);
                    Array.Reverse(data);
                    return BitConverter.ToInt32(data, 0);
                }
            },
            [typeof(uint)] = new MappingDefinition
            {
                Writer = x =>
                {
                    x.Writer.Write((byte)(((uint)x.Item >> 24) & 0xff));
                    x.Writer.Write((byte)(((uint)x.Item >> 16) & 0xff));
                    x.Writer.Write((byte)(((uint)x.Item >> 8) & 0xff));
                    x.Writer.Write((byte)(((uint)x.Item >> 0) & 0xff));
                },
                Reader = x =>
                {
                    var data = x.Reader.ReadBytes(4);
                    Array.Reverse(data);
                    return BitConverter.ToUInt32(data, 0);
                }
            },
            [typeof(long)] = new MappingDefinition
            {
                Writer = x =>
                {
                    x.Writer.Write((byte)(((long)x.Item >> 56) & 0xff));
                    x.Writer.Write((byte)(((long)x.Item >> 48) & 0xff));
                    x.Writer.Write((byte)(((long)x.Item >> 40) & 0xff));
                    x.Writer.Write((byte)(((long)x.Item >> 32) & 0xff));
                    x.Writer.Write((byte)(((long)x.Item >> 24) & 0xff));
                    x.Writer.Write((byte)(((long)x.Item >> 16) & 0xff));
                    x.Writer.Write((byte)(((long)x.Item >> 8) & 0xff));
                    x.Writer.Write((byte)(((long)x.Item >> 0) & 0xff));

                },
                Reader = x =>
                {
                    var data = x.Reader.ReadBytes(8);
                    Array.Reverse(data);
                    return BitConverter.ToInt64(data, 0);
                }
            },
            [typeof(ulong)] = new MappingDefinition
            {
                Writer = x =>
                {
                    x.Writer.Write((byte)(((ulong)x.Item >> 56) & 0xff));
                    x.Writer.Write((byte)(((ulong)x.Item >> 48) & 0xff));
                    x.Writer.Write((byte)(((ulong)x.Item >> 40) & 0xff));
                    x.Writer.Write((byte)(((ulong)x.Item >> 32) & 0xff));
                    x.Writer.Write((byte)(((ulong)x.Item >> 24) & 0xff));
                    x.Writer.Write((byte)(((ulong)x.Item >> 16) & 0xff));
                    x.Writer.Write((byte)(((ulong)x.Item >> 8) & 0xff));
                    x.Writer.Write((byte)(((ulong)x.Item >> 0) & 0xff));
                },
                Reader = x =>
                {
                    var data = x.Reader.ReadBytes(8);
                    Array.Reverse(data);
                    return BitConverter.ToUInt64(data, 0);
                }
            },
            [typeof(float)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((float)x.Item),
                Reader = x => x.Reader.ReadSingle()
            },
            [typeof(double)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write((double)x.Item),
                Reader = x => x.Reader.ReadDouble()
            },
            [typeof(TimeSpan)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write(((TimeSpan)x.Item).Ticks),
                Reader = x => new TimeSpan(x.Reader.ReadInt64())
            },
            [typeof(DateTime)] = new MappingDefinition
            {
                Writer = x => x.Writer.Write(((DateTime)x.Item).Ticks),
                Reader = x => new DateTime(x.Reader.ReadInt64())
            },
            [typeof(string)] = new MappingDefinition
            {
                Writer = x => WriteString(x.Writer, (string)x.Item, encoding, x.Count),
                Reader = x => ReadString(x.Reader, encoding, x.Count)
            },
            [typeof(byte[])] = new MappingDefinition
            {
                Writer = x =>
                {
                    var data = (byte[])x.Item;
                    var bytesToWrite = Math.Min(data.Length, x.Count);
                    x.Writer.Write(data, 0, bytesToWrite);

                    var remainingBytes = x.Count - bytesToWrite;
                    if (remainingBytes > 0)
                        x.Writer.Write(new byte[remainingBytes], 0, remainingBytes);
                },
                Reader = x => x.Reader.ReadBytes(x.Count)
            },
        };

        private static string ReadString(BinaryReader reader, Encoding encoding, int length)
        {
            var data = reader.ReadBytes(length);
            var terminatorIndex = Array.FindIndex(data, x => x == 0);
            return encoding.GetString(data, 0, terminatorIndex < 0 ? length : terminatorIndex);
        }

        private static void WriteString(BinaryWriter writer, string str, Encoding encoding, int length)
        {
            var data = encoding.GetBytes(str);
            if (data.Length <= length)
            {
                writer.Write(data, 0, data.Length);
                int remainsBytes = length - data.Length;
                if (remainsBytes > 0)
                {
                    writer.Write(new byte[remainsBytes]);
                }
            }
            else
            {
                writer.Write(data, 0, length);
            }
        }
    }
}
