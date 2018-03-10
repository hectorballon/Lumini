﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lumini.Scheduled.Extensions.ArrayExtensions;

namespace Lumini.Scheduled.Extensions
{
    public static class ObjectExtensions
    {
        private static readonly MethodInfo CloneMethod =
            typeof(object).GetMethod("MemberwiseClone", BindingFlags.NonPublic | BindingFlags.Instance);

        public static T GetAttribute<T>(this Enum value)
            where T : Attribute
        {
            var field = value.GetType().GetField(value.ToString());
            return field.GetAttribute<T>();
        }

        public static T GetAttribute<T>(this MemberInfo value)
            where T : Attribute
        {
            var atts = value.GetCustomAttributes(false);

            return atts?.Where(a => a is T).FirstOrDefault() as T;
        }

        public static bool IsNumeric(this object expression)
        {
            switch (expression)
            {
                case null:
                case DateTime _:
                    return false;
                case short _:
                case int _:
                case long _:
                case decimal _:
                case float _:
                case double _:
                case bool _:
                    return true;
            }

            try
            {
                double output;
                if (expression is string)
                    return double.TryParse(expression as string, out output);
                return double.TryParse(expression.ToString(), out output);
            }
            catch
            {
                // ignore
            }
            return false;
        }

        public static bool IsPrimitive(this Type type)
        {
            if (type == typeof(string)) return true;
            return type.IsValueType & type.IsPrimitive;
        }

        public static object Copy(this object originalObject)
        {
            return InternalCopy(originalObject, new Dictionary<object, object>(new ReferenceEqualityComparer()));
        }

        private static object InternalCopy(object originalObject, IDictionary<object, object> visited)
        {
            if (originalObject == null) return null;
            var typeToReflect = originalObject.GetType();
            if (IsPrimitive(typeToReflect)) return originalObject;
            if (visited.ContainsKey(originalObject)) return visited[originalObject];
            if (typeof(Delegate).IsAssignableFrom(typeToReflect)) return null;
            var cloneObject = CloneMethod.Invoke(originalObject, null);
            if (typeToReflect.IsArray)
            {
                var arrayType = typeToReflect.GetElementType();
                if (IsPrimitive(arrayType) == false)
                {
                    var clonedArray = (Array)cloneObject;
                    clonedArray.ForEach((array, indices) =>
                        array.SetValue(InternalCopy(clonedArray.GetValue(indices), visited), indices));
                }
            }
            visited.Add(originalObject, cloneObject);
            CopyFields(originalObject, visited, cloneObject, typeToReflect);
            RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect);
            return cloneObject;
        }

        private static void RecursiveCopyBaseTypePrivateFields(object originalObject,
            IDictionary<object, object> visited, object cloneObject, Type typeToReflect)
        {
            if (typeToReflect.BaseType != null)
            {
                RecursiveCopyBaseTypePrivateFields(originalObject, visited, cloneObject, typeToReflect.BaseType);
                CopyFields(originalObject, visited, cloneObject, typeToReflect.BaseType,
                    BindingFlags.Instance | BindingFlags.NonPublic, info => info.IsPrivate);
            }
        }

        private static void CopyFields(object originalObject, IDictionary<object, object> visited, object cloneObject,
            Type typeToReflect,
            BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                                        BindingFlags.FlattenHierarchy, Func<FieldInfo, bool> filter = null)
        {
            foreach (var fieldInfo in typeToReflect.GetFields(bindingFlags))
            {
                if (filter != null && filter(fieldInfo) == false) continue;
                if (IsPrimitive(fieldInfo.FieldType)) continue;
                var originalFieldValue = fieldInfo.GetValue(originalObject);
                var clonedFieldValue = InternalCopy(originalFieldValue, visited);
                fieldInfo.SetValue(cloneObject, clonedFieldValue);
            }
        }

        public static T Copy<T>(this T original)
        {
            return (T)Copy((object)original);
        }
    }

    public class ReferenceEqualityComparer : EqualityComparer<object>
    {
        public override bool Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        public override int GetHashCode(object obj)
        {
            if (obj == null) return 0;
            return obj.GetHashCode();
        }
    }

    namespace ArrayExtensions
    {
        public static class ArrayExtensions
        {
            public static void ForEach(this Array array, Action<Array, int[]> action)
            {
                if (array.LongLength == 0) return;
                var walker = new ArrayTraverse(array);
                do
                {
                    action(array, walker.Position);
                } while (walker.Step());
            }
        }

        internal class ArrayTraverse
        {
            private readonly int[] _maxLengths;
            public int[] Position;

            public ArrayTraverse(Array array)
            {
                _maxLengths = new int[array.Rank];
                for (var i = 0; i < array.Rank; ++i)
                    _maxLengths[i] = array.GetLength(i) - 1;
                Position = new int[array.Rank];
            }

            public bool Step()
            {
                for (var i = 0; i < Position.Length; ++i)
                    if (Position[i] < _maxLengths[i])
                    {
                        Position[i]++;
                        for (var j = 0; j < i; j++)
                            Position[j] = 0;
                        return true;
                    }
                return false;
            }
        }
    }
}
