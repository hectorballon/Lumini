using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Lumini.Concurrent.LoadBalancing
{
    public abstract class KeyGrouping<TKey, TEntity> : RoundRobin
        where TEntity : class
    {
        private readonly Dictionary<TKey, int> _groupingKeyDictionary;

        protected KeyGrouping(ILogger logger) : base(logger)
        {
            _groupingKeyDictionary = new Dictionary<TKey, int>();
        }

        public override async Task<bool> SendItemAsync(object expression)
        {
            if (!Workers.Any()) throw new Exception("No workers available!");
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (!(expression is Expression<Func<TEntity, object>>)) throw new Exception("Expression was expected!");
            var predicate = (Expression<Func<TEntity, object>>)expression;
            var worker = SelectWorker(predicate, out var itemToProcess);
            if (!worker.CanReceiveItems()) return false;
            await worker.ReceiveAsync(itemToProcess);
            return true;
        }

        protected IWorker SelectWorker(Expression<Func<TEntity, object>> predicate, out TEntity itemToProcess)
        {
            var chooseWorker = new Func<TKey, IWorker>(key =>
            {
                lock (new object())
                {
                    if (_groupingKeyDictionary.ContainsKey(key))
                        return Workers.ElementAt(_groupingKeyDictionary[key]);
                    var worker = base.SelectWorker();
                    _groupingKeyDictionary.Add(key, worker.WorkerId);
                    return worker;
                }
            });

            if (!(predicate.Body is NewExpression memberSelector)) throw new Exception($"Predicate is not valid");
            itemToProcess = GetUnderlyingParameterValue(memberSelector);
            var groupingKey = GetKey(predicate, itemToProcess);
            return chooseWorker(groupingKey);
        }

        protected abstract TKey GetKey(Expression<Func<TEntity, object>> predicate, TEntity itemToProcess);

        private static TEntity GetUnderlyingParameterValue(NewExpression memberSelector)
        {
            var values = new List<KeyValuePair<Type, TEntity>>();
            foreach (var argument in memberSelector.Arguments)
            {
                var exp = ResolveMemberExpression(argument);
                var type1 = argument.Type;

                var value = GetValue(exp);

                values.Add(new KeyValuePair<Type, TEntity>(type1, (TEntity)value));
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
            return null;
        }
    }
}