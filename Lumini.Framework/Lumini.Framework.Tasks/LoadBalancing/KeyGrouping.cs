using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Lumini.Framework.Common;

namespace Lumini.Framework.Tasks.LoadBalancing
{
    public class KeyGrouping<T> : LeastLoaded
        where T : class
    {
        private readonly Dictionary<string, int> _groupingKeyDictionary;

        public KeyGrouping()
        {
            _groupingKeyDictionary = new Dictionary<string, int>();
        }

        public override async Task<bool> SendItemAsync(object expression)
        {
            if (!Workers.Any()) throw new Exception("No workers available!");
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (!(expression is Expression<Func<T, object>>)) throw new Exception("Expression was expected!");
            var predicate = (Expression<Func<T, object>>)expression;
            var worker = SelectWorker(predicate, out var itemToProcess);
            if (!worker.CanReceiveItems()) return false;
            await worker.ReceiveAsync(itemToProcess);
            return true;
        }

        protected IWorker SelectWorker(Expression<Func<T, object>> predicate, out T itemToProcess)
        {
            var chooseWorker = new Func<string, IWorker>(key =>
            {
                if (_groupingKeyDictionary.ContainsKey(key))
                    return Workers.ElementAt(_groupingKeyDictionary[key]);
                var worker = base.SelectWorker();
                _groupingKeyDictionary.Add(key, worker.WorkerId);
                return worker;
            });

            if (!(predicate.Body is NewExpression memberSelector)) throw new Exception($"Predicate is not valid");
            itemToProcess = GetUnderlyingParameterValue(memberSelector);
            var groupingKey = GetKey(predicate, itemToProcess);
            return chooseWorker(groupingKey);
        }

        public static string GetKey(Expression<Func<T, object>> predicate, T itemToProcess)
        {
            return Utils.GetHashCodeForSelectedKey(predicate).ToBase62();
        }

        private static T GetUnderlyingParameterValue(NewExpression memberSelector)
        {
            var values = new List<KeyValuePair<Type, T>>();
            foreach (var argument in memberSelector.Arguments)
            {
                var exp = ResolveMemberExpression(argument);
                var type1 = argument.Type;

                var value = GetValue(exp);

                values.Add(new KeyValuePair<Type, T>(type1, (T)value));
            }
            values = values.Distinct().ToList();
            return values.First().Value;
        }

        public static MemberExpression ResolveMemberExpression(Expression expression)
        {
            switch (expression)
            {
                case MemberExpression memberExpression:
                    return memberExpression;
                case UnaryExpression _:
                    return (MemberExpression)((UnaryExpression)expression).Operand;
            }
            throw new NotSupportedException(expression.ToString());
        }

        private static object GetValue(MemberExpression exp)
        {
            switch (exp.Expression)
            {
                case ConstantExpression expression:
                    return expression.Value
                        .GetType()
                        .GetField(exp.Member.Name)
                        .GetValue(expression.Value);
                case MemberExpression _:
                    return GetValue((MemberExpression)exp.Expression);
            }
            throw new NotImplementedException();
        }
    }
}