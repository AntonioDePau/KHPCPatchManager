using System;
using System.IO;
using System.Text;

namespace Xe.BinaryMapper
{
    public partial class BinaryMapping
    {
        private static readonly Encoding DefautlEncoding;
        private static readonly MappingConfiguration DefaultConfiguration;
        public static IBinaryMapping Default { get; }

        static BinaryMapping()
        {
            DefautlEncoding = Encoding.UTF8;
            DefaultConfiguration = MappingConfiguration.DefaultConfiguration(DefautlEncoding);
            Default = new RealBinaryMapping(DefaultConfiguration);
        }

        public static T ReadObject<T>(Stream stream, int baseOffset = 0) where T : class =>
            Default.ReadObject<T>(stream, baseOffset);

        public static T ReadObject<T>(Stream stream, T item, int baseOffset = 0) where T : class =>
            Default.ReadObject<T>(stream, item, baseOffset);

        [Obsolete]
        public static T ReadObject<T>(BinaryReader reader, int baseOffset = 0) where T : class =>
            Default.ReadObject<T>(reader.BaseStream, baseOffset);

        [Obsolete]
        public static T ReadObject<T>(BinaryReader reader, T item, int baseOffset = 0) where T : class =>
            Default.ReadObject<T>(reader.BaseStream, item, baseOffset);

        public static T WriteObject<T>(Stream stream, T item, int baseOffset = 0) where T : class =>
            Default.WriteObject<T>(stream, item, baseOffset);

        [Obsolete]
        public static T WriteObject<T>(BinaryWriter writer, T item, int baseOffset = 0) where T : class =>
            Default.WriteObject<T>(writer.BaseStream, item, baseOffset);
    }
}
