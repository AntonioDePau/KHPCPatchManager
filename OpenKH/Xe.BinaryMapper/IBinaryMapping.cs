using System;
using System.IO;

namespace Xe.BinaryMapper
{
    public interface IBinaryMapping
    {
        T ReadObject<T>(Stream stream, T item, int baseOffset = 0) where T : class;
        T WriteObject<T>(Stream stream, T item, int baseOffset = 0) where T : class;
    }

    public static class BinaryMappingExtensions
    {
        public static T ReadObject<T>(this IBinaryMapping binaryMapping, Stream stream, int baseOffset = 0)
            where T : class =>
            (T)binaryMapping.ReadObject(stream, Activator.CreateInstance<T>(), baseOffset);
    }
}
