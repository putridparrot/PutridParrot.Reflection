using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace PutridParrot.Reflection
{
    public static partial class Reflect
    {
        /// <summary>
        /// Creates an array of the elementType supplied at runtime
        /// </summary>
        /// <param name="elementType"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static Array CreateArray(Type elementType, params object[] values)
        {
            if (values == null)
            {
                return Array.CreateInstance(elementType, 0);
            }

            var array = Array.CreateInstance(elementType, values.Length);
            for (var i = 0; i < values.Length; i++)
            {
                array.SetValue(values[i], i);
            }

            return array;
        }

        public static bool IsType<T>(Type type)
        {
            return ReferenceEquals(typeof(T), type);
        }

        /// <summary>
        /// Determines whether a type is a numeric type. 
        /// If it's nullable then the underlying type is checked.
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static bool IsNumeric(Type type)
        {
            if (!type.IsArray)
            {
                if (IsNullable(type))
                {
                    return IsNumeric(Nullable.GetUnderlyingType(type));
                }
                switch (Type.GetTypeCode(type))
                {
                    case TypeCode.Int16:
                    case TypeCode.UInt16:
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Double:
                    case TypeCode.Decimal:
                        return true;
                }
            }
            return false;
        }

        public static bool IsNullable(Type type)
        {
            return type.IsGenericType &&
                /*!type.GetTypeInfo().IsGenericTypeDefinition &&*/
                ReferenceEquals(type.GetGenericTypeDefinition(), typeof(Nullable<>));
        }

        /// <summary>
        /// Gets the field info for a field whether public 
        /// or non public on an instance
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public static FieldInfo GetField(this Type type, string fieldName)
        {
            return type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }


        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/implicit-numeric-conversions-table
        private static readonly Dictionary<Type, List<Type>> ImplicitConverionTypes = new Dictionary<Type, List<Type>>
        {
            { typeof(sbyte), new List<Type> { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(byte), new List<Type> { typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(short), new List<Type> { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(ushort), new List<Type> { typeof(int), typeof(uint), typeof(long), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(int), new List<Type> { typeof(long), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(uint), new List<Type> { typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(long), new List<Type> { typeof(float), typeof(double), typeof(decimal) } },
            { typeof(char), new List<Type> { typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) } },
            { typeof(float), new List<Type> { typeof(double) } },
            { typeof(ulong), new List<Type> { typeof(float), typeof(double), typeof(decimal) } }
        };

        // https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/explicit-numeric-conversions-table
        private static readonly Dictionary<Type, List<Type>> ExplicitConverionTypes = new Dictionary<Type, List<Type>>
        {
            { typeof(sbyte), new List<Type> { typeof(byte), typeof(ushort), typeof(uint), typeof(ulong), typeof(char) } },
            { typeof(byte), new List<Type> { typeof(sbyte), typeof(char) } },
            { typeof(short), new List<Type> { typeof(sbyte), typeof(byte), typeof(ushort), typeof(uint), typeof(ulong), typeof(char) } },
            { typeof(ushort), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(char) } },
            { typeof(int), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(uint), typeof(ulong), typeof(char) } },
            { typeof(uint), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(char) } },
            { typeof(long), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(ulong), typeof(char) } },
            { typeof(ulong), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(char) } },
            { typeof(char), new List<Type> { typeof(sbyte), typeof(byte), typeof(short) } },
            { typeof(float), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(decimal) } },
            { typeof(double), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float), typeof(decimal) } },
            { typeof(decimal), new List<Type> { typeof(sbyte), typeof(byte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(char), typeof(float), typeof(double) } }
        };

        public static bool CanConvertTo(this Type type, Type to, bool includeExplicitConversions = false)
        {
            if (ReferenceEquals(type, to) || to.IsAssignableFrom(type))
                return true;

            if (type == null)
            {
                return to.IsClass || IsNullable(to);
            }

            // use implict tables from above links
            if (ImplicitConverionTypes.TryGetValue(type, out var implictConversion) && 
                implictConversion.Contains(to))
            {
                return true;
            }

            var typeConverter = TypeDescriptor.GetConverter(type);
            if (typeConverter.CanConvertTo(to))
            {
                return true;
            }

            // any implicit operators
            if (type
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Any(m => m.ReturnType == to && m.Name == "op_Implicit"))
            {
                return true;
            }

            // if explcit conversions should be included
            if (includeExplicitConversions)
            {
                if (ExplicitConverionTypes.TryGetValue(type, out var explicitConversion) && 
                    explicitConversion.Contains(to))
                {
                    return true;
                }

                return type
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Any(m => m.ReturnType == to && m.Name == "op_Explicit");
            }

            return false;
        }

        // cannot recall the use case for needing this
        //private static T InternalCast<T>(object o)
        //{
        //    return (T) o;
        //}

        //public static object Cast(Type type, object o)
        //{
        //    var methodInfo = typeof(Reflect).GetMethod(nameof(InternalCast));
        //    if (methodInfo != null)
        //    {
        //        var genericMethod = methodInfo.MakeGenericMethod(type);
        //        return genericMethod.Invoke(null, new[] {o});
        //    }
        //    return o;
        //}
    }

}
