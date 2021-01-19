/*
  █▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀ ▀▀▀▀▀▀▀▀▀▀▀▀▀▀ ▀▀▀  ▀  ▀      ▀▀
  █  The MIT License (MIT)
  █
  █  Copyright (c) 2021 JP Dillingham (jp@dillingham.ws)
  █
  █  Permission is hereby granted, free of charge, to any person obtaining a copy
  █  of this software and associated documentation files (the "Software"), to deal
  █  in the Software without restriction, including without limitation the rights
  █  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
  █  copies of the Software, and to permit persons to whom the Software is
  █  furnished to do so, subject to the following conditions:
  █
  █  The above copyright notice and this permission notice shall be included in all
  █  copies or substantial portions of the Software.
  █
  █  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
  █  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
  █  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
  █  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
  █  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
  █  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
  █  SOFTWARE.
  █
  ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀  ▀▀ ▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀▀██
                                                                                               ██
                                                                                           ▀█▄ ██ ▄█▀
                                                                                             ▀████▀
                                                                                               ▀▀                            */

namespace Utility.EnvironmentVariables
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    /// <summary>
    ///     Indicates that the property is to be used as a target for automatic population of values from environment variables
    ///     when invoking the <see cref="EnvironmentVariables.Populate(string)"/> method.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class EnvironmentVariableAttribute : Attribute
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="EnvironmentVariableAttribute"/> class.
        /// </summary>
        /// <param name="name">The name of the environment variable</param>
        public EnvironmentVariableAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        ///     Gets or sets the name of the environment variable.
        /// </summary>
        public string Name { get; set; }
    }

    /// <summary>
    ///     Provides static methods used to populate properties from environment variable values.
    /// </summary>
    public static class EnvironmentVariables
    {
        /// <summary>
        ///     Populates the properties in the invoking class marked with the
        ///     <see cref="EnvironmentVariableAttribute"/><see cref="Attribute"/> with the values specified in environment variables.
        /// </summary>
        /// <param name="caller">Internal parameter used to identify the calling method.</param>
        public static void Populate([CallerMemberName] string caller = default(string))
        {
            var type = GetCallingType(caller);
            var targetProperties = GetTargetProperties(type);

            foreach (var property in targetProperties)
            {
                var propertyType = property.Value.PropertyType;

                string value = Environment.GetEnvironmentVariable(property.Key);
                object convertedValue;

                if (propertyType == typeof(bool))
                {
                    convertedValue = value?.Equals("true", StringComparison.InvariantCultureIgnoreCase) ?? false;
                }
                else if (propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    Type valueType;

                    if (propertyType.IsArray)
                    {
                        valueType = propertyType.GetElementType();
                    }
                    else
                    {
                        valueType = propertyType.GetGenericArguments()[0];
                    }

                    // create a list to store converted values
                    Type valueListType = typeof(List<>).MakeGenericType(valueType);
                    var valueList = (IList)Activator.CreateInstance(valueListType);

                    // populate the list
                    foreach (object v in value.Split(',').Select(s => s.Trim()))
                    {
                        valueList.Add(ChangeType(v, property.Key, property.Value));
                    }

                    if (propertyType.IsArray)
                    {
                        var valueArray = Array.CreateInstance(propertyType.GetElementType(), valueList.Count);

                        for (int i = 0; i < valueArray.Length; i++)
                        {
                            valueArray.SetValue(valueList[i], i);
                        }

                        convertedValue = valueArray;
                    }
                    else
                    {
                        convertedValue = valueList;
                    }
                }
                else
                {
                    convertedValue = ChangeType(value, property.Key, property.Value);
                }

                property.Value.SetValue(null, convertedValue);
            }
        }

        private static Type GetCallingType(string caller)
        {
            var callingMethod = new StackTrace().GetFrames()
                .Select(f => f.GetMethod())
                .FirstOrDefault(m => m.Name == caller);

            if (callingMethod == default(MethodBase))
            {
                throw new InvalidOperationException($"Unable to determine the containing type of the calling method '{caller}'.  Explicitly specify the originating Type.");
            }

            return callingMethod.DeclaringType;
        }

        private static object ChangeType(object value, string name, PropertyInfo property)
        {
            var toType = property.PropertyType;

            try
            {
                if (toType.IsEnum)
                {
                    return Enum.Parse(toType, (string)value, true);
                }

                return Convert.ChangeType(value, toType, CultureInfo.InvariantCulture);
            }
            catch (Exception ex) when (ex is InvalidCastException || ex is FormatException || ex is OverflowException || ex is ArgumentNullException)
            {
                string message = $"Failed to convert value '{value}' to target type {toType}";
                throw new ArgumentException(message, name, ex);
            }
        }

        private static Dictionary<string, PropertyInfo> GetTargetProperties(Type type)
        {
            Dictionary<string, PropertyInfo> properties = new Dictionary<string, PropertyInfo>();

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
            {
                // attempt to fetch the ArgumentAttribute of the property
                CustomAttributeData attribute = property.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == typeof(EnvironmentVariableAttribute).Name);

                // if found, extract the Name property and add it to the dictionary
                if (attribute != default(CustomAttributeData))
                {
                    string name = (string)attribute.ConstructorArguments[0].Value;

                    if (!properties.ContainsKey(name))
                    {
                        properties.Add(name, property);
                    }
                }
            }

            return properties;
        }
    }
}
