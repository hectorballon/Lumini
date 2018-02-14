using System;
using System.ComponentModel.DataAnnotations;
using Cauldron.Interception;

namespace Lumini.Framework.Tasks.Scheduled
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = false)]
    public sealed class StringCheckerAttribute : Attribute, IPropertySetterInterceptor
    {
        [AssignMethod("{CtorArgument:0}", true)] public Func<string, string, bool> OnSetMethod = null;

        public StringCheckerAttribute(string methodName)
        {
        }

        public void OnException(Exception e)
        {
        }

        public void OnExit()
        {
        }

        public bool OnSet(PropertyInterceptionInfo propertyInterceptionInfo, object oldValue, object newValue)
        {
            var pattern = string.Empty;
            var attributes = propertyInterceptionInfo.ToPropertyInfo().GetCustomAttributes(false);
            foreach (var attribute in attributes)
            {
                if (!(attribute is RegularExpressionAttribute regularExpressionAttribute)) continue;
                pattern = regularExpressionAttribute.Pattern;
                break;
            }

            if (!string.IsNullOrEmpty(pattern) && OnSetMethod != null)
                return OnSetMethod.Invoke(pattern, (string) newValue);
            return false;
        }
    }
}