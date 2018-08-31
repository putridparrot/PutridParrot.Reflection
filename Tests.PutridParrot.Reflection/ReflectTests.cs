using System;
using System.Diagnostics.CodeAnalysis;
using NUnit.Framework;
using PutridParrot.Reflection;
using Reflect = PutridParrot.Reflection.Reflect;

namespace Tests.PutridParrot.Reflection
{
    [ExcludeFromCodeCoverage]
    [TestFixture]
    public class ReflectTests
    {
        class Celsius
        {
            public static explicit operator Fahrenheit(Celsius c)
            {
                return new Fahrenheit();
            }
        }

        class Fahrenheit
        {
            public static explicit operator Celsius(Fahrenheit fahr)
            {
                return new Celsius();
            }
        }

        [Test]
        public void CreateArray_WithNoParameters_ExpectZeroLengthArray()
        {
            var array = Reflect.CreateArray(typeof(double));
            Assert.AreEqual(0, array.Length);
            Assert.IsTrue(array.GetType() == typeof(double[]));
        }

        [Test]
        public void IsNullable_NonNullableType_ExpectFalse()
        {
            Assert.IsFalse(Reflect.IsNullable(typeof(bool)));
        }

        [Test]
        public void IsNullable_NullableType_ExpectTrue()
        {
            Assert.IsTrue(Reflect.IsNullable(typeof(bool?)));
        }

        [Test]
        public void CanConvertTo_NonNullableToNullable()
        {
            Assert.IsTrue(typeof(bool).CanConvertTo(typeof(bool?)));
        }

        [Test]
        public void CanConvertTo_Implicit()
        {
            char c = 'a';
            int i = c;

            Assert.IsTrue(typeof(char).CanConvertTo(typeof(int)));
        }

        [Test]
        public void CanCanvertTo_Explcit()
        {
            Assert.IsTrue(typeof(Fahrenheit).CanConvertTo(typeof(Celsius), true));
        }

        [Test]
        public void CanCanvertTo_NullShouldConvertToClass()
        {
            Type type = null;

            Assert.IsTrue(type.CanConvertTo(typeof(Celsius), true));
        }

        [Test]
        public void CanCanvertTo_NullShouldConvertToNullable()
        {
            Type type = null;

            int? ni = null;

            Assert.IsTrue(type.CanConvertTo(typeof(Nullable<>), true));
        }
    }

}
