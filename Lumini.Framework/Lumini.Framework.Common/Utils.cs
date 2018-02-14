using System;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using Base62;

namespace Lumini.Framework.Common
{
    public static class Utils
    {
        public static Expression<Func<T, object>> Express<T>(Expression<Func<T, object>> expression)
        {
            return expression;
        }

        public static string GenerateKey(object compoundKey)
        {
            return compoundKey.GetCustomHashCode().ToBase62();
        }

        private static int GetCustomHashCode(this object p)
        {
            var type = p.GetType();
            var props = type.GetProperties();
            var elements = (from prop in props
                            let value = prop.GetValue(p, null).ToString()
                            select value).ToArray();
            var key = elements[0].ExtractNumericId();
            for (var i = 1; i < elements.Length; i++)
                key ^= elements[i].ExtractNumericId();
            return key;
        }

        private static int ExtractNumericId(this string id)
        {
            return int.Parse(Regex.Replace(id, "[^0-9]", ""));
        }

        public static int GetHashCodeForSelectedKey<T>(Expression<Func<T, object>> predicate)
        {
            if (!(predicate.Body is NewExpression memberSelector)) throw new Exception("Expression is not valid!");
            var lambda = Expression.Lambda(memberSelector,
                predicate.Parameters);
            var d = lambda.Compile();
            var compoundKey = d.DynamicInvoke(new object[1]);
            return compoundKey.GetCustomHashCode();
        }

        public static string ToBase62(this int number)
        {
            var converter = new Base62Converter();
            return converter.Encode(number.ToString());
        }
    }
}