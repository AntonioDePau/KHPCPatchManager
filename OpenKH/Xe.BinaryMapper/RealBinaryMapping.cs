using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Xe.BinaryMapper
{
    internal partial class RealBinaryMapping : IBinaryMapping
    {
        private static Dictionary<Type, Dictionary<string, Func<object, int>>> memberMappings;

        private readonly Dictionary<Type, MappingDefinition> mappings;

        public RealBinaryMapping(MappingConfiguration configuration)
        {
            if (configuration.Mappings == null)
                throw new ArgumentNullException(nameof(configuration.Mappings),
                    $"The configuration property {nameof(configuration.Mappings)} can not be null.");
            if (configuration.MemberMappings == null)
                throw new ArgumentNullException(nameof(configuration.MemberMappings),
                    $"The configuration property {nameof(configuration.MemberMappings)} can not be null.");

            mappings = configuration.Mappings;
            memberMappings = configuration.MemberMappings;
        }

        private MyProperty GetPropertySettings(Type classType, PropertyInfo propertyInfo)
        {
            var property = new MyProperty
            {
                MemberInfo = propertyInfo,
                DataInfo = Attribute.GetCustomAttribute(propertyInfo, typeof(DataAttribute)) as DataAttribute
            };

            if (memberMappings.TryGetValue(classType, out var classMapping))
            {
                if (classMapping.TryGetValue(propertyInfo.Name, out var func))
                {
                    property.GetLengthFunc = func;
                }
            }

            return property;
        }
    }
}
