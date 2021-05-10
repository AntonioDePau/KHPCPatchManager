using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Xe.BinaryMapper
{
    public class MappingConfiguration
    {
        public Dictionary<Type, Dictionary<string, Func<object, int>>> MemberMappings { get; set; }

        public Dictionary<Type, MappingDefinition> Mappings { get; set; }

        public static MappingConfiguration DefaultConfiguration() =>
            DefaultConfiguration(Encoding.UTF8);

        public static MappingConfiguration DefaultConfiguration(Encoding encoding, bool isBigEndian = false) => new MappingConfiguration
        {
            Mappings = isBigEndian ? BinaryMapper.Mappings.BigEndianMapping(encoding) : BinaryMapper.Mappings.DefaultMapping(encoding),
            MemberMappings = new Dictionary<Type, Dictionary<string, Func<object, int>>>()
        };
    }

    public static class MappingConfigurationExtensions
    {
        public static MappingConfiguration ForType<T>(
            this MappingConfiguration configuration,
            Func<MappingReadArgs, object> reader,
            Action<MappingWriteArgs> writer) =>
            ForType(configuration, typeof(T), reader, writer);

        public static MappingConfiguration ForType(
            this MappingConfiguration configuration, Type type,
            Func<MappingReadArgs, object> reader,
            Action<MappingWriteArgs> writer)
        {
            if (configuration.Mappings == null)
                configuration.Mappings = new Dictionary<Type, MappingDefinition>();

            configuration.Mappings[type] = new MappingDefinition
            {
                Reader = reader,
                Writer = writer
            };
            return configuration;
        }

        public static MappingConfiguration UseMemberForLength<T>(
            this MappingConfiguration configuration, string memberName,
            Func<T, string, int> getLengthFunc)
            where T : class
        {
            var classType = typeof(T);
            var classMappings = new Dictionary<string, Func<object, int>>();
            if (!configuration.MemberMappings.TryGetValue(classType, out classMappings))
            {
                classMappings = new Dictionary<string, Func<object, int>>();
                configuration.MemberMappings.Add(classType, classMappings);
            }

            classMappings[memberName] = o => getLengthFunc((T)o, memberName);
            return configuration;
        }

        public static IBinaryMapping Build(this MappingConfiguration configuration) =>
            new RealBinaryMapping(configuration);
    }

    public class MappingDefinition
    {
        public Func<MappingReadArgs, object> Reader { get; set; }

        public Action<MappingWriteArgs> Writer { get; set; }
    }

    public class MappingWriteArgs
    {
        public BinaryWriter Writer { get; set; }

        public object Item { get; set; }

        public DataAttribute DataAttribute { get; set; }

        public int Count { get; set; }

        public byte BitData { get; set; }

        public int BitIndex { get; set; }
    }

    public class MappingReadArgs
    {
        public BinaryReader Reader { get; set; }

        public DataAttribute DataAttribute { get; set; }

        public int Count { get; set; }

        public byte BitData { get; set; }

        public int BitIndex { get; set; }
    }
}
