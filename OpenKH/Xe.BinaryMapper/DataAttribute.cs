using System;

namespace Xe.BinaryMapper
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DataAttribute : Attribute
    {
        public int? Offset { get; set; }

        public int Count { get; set; }

        public int Stride { get; set; }

        public int BitIndex { get; set; }

        public DataAttribute()
        {
            Offset = null;
            Count = 1;
            BitIndex = -1;
        }

        public DataAttribute(int offset, int count = 1, int stride = 0, int bitIndex = -1)
        {
            Offset = offset;
            Count = count;
            Stride = stride;
            BitIndex = bitIndex;
        }
    }
}
