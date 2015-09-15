using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CSharpx;

namespace CommandLine
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    class ValidValuesAttribute: Attribute
    {
        private readonly Maybe<IEnumerable<string>> text;
        private readonly Predicate<object> isValid; 

        public Predicate<object> IsValid { get { return isValid; } }
        public IEnumerable<string> Text { get { return text.IsNothing()? new string[0] : text.FromJust(); } }

        /// <summary>
        /// Uses Type to compute valid values. Type must implment either IEnumerable or IValidValues.
        /// If both are implemented IValidValues is ignored in favor of IEnumerable.
        /// IEnumerable should be used when there is a (small) fixed set of valid values, in this case, text
        /// need not be given as the valid values will be computed from that set.
        /// IValidValues should be used when there is an infinite (or large fixed) set of valid values.
        /// In this case text should be used to describe these values in a finite way. 
        /// </summary>
        /// <param name="values"></param>
        public ValidValuesAttribute(Type values)
        {
            if (values == null)
            {
               throw new ArgumentNullException("values");    
            }

            if (!(IsAssignable<IEnumerable>(values) || IsAssignable<IValidValues>(values)))
            {
                throw new ArgumentException(string.Format("value must be assinable from {0} or {1}.", typeof(IEnumerable), typeof(IValidValues)), "values");
            }

            var vvt = values.GetCustomAttributes(typeof(ValidValuesTextAttribute), false).OfType<ValidValuesTextAttribute>();
            text = vvt.Any() ? ((IEnumerable<string>)vvt.FirstOrDefault().text).ToMaybe() : Maybe.Nothing<IEnumerable<string>>();

            var valuesObj = Activator.CreateInstance(values);
            var validValues = valuesObj as IValidValues;
            if (validValues != null)
            {
                isValid = GetPredicateFrom(validValues);
                return;
            }

            var tempValues = valuesObj as IEnumerable;
            if (tempValues == null)
            {
                throw new ArgumentException(string.Format("value must be assinable from {0} or {1}.", typeof(IEnumerable), typeof(IValidValues)), "values");
            }

            var valuesList = tempValues.Cast<object>().ToList();
            isValid = GetPredicateFrom(valuesList.ToSet());

            if (text.IsNothing())
            {
                text = GetMaybeEnumerableFrom(valuesList);
            }
        }

        private static Predicate<object> GetPredicateFrom(IEnumerable<object> values)
        {
            return values.Contains;
        }

        private static Predicate<object> GetPredicateFrom(IValidValues values)
        {
            return values.IsValid;
        }

        private static Maybe<IEnumerable<string>> GetMaybeEnumerableFrom(IEnumerable<object> values)
        {
            return values.Select(e => e.ToString()).ToMaybe();
        }

        private static bool IsAssignable<T>(Type value) where  T : class
        {
            var type = typeof (T);
            return type.IsAssignableFrom(value);
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class ValidValuesTextAttribute : Attribute
    {
        public string[] text { get; private set; }

        public ValidValuesTextAttribute(string[] text)
        {
            this.text = text;
        }

    }

    public interface IValidValues
    {
        bool IsValid(object value);
    }

    public interface IValidValues<in T> : IValidValues
    {
        bool IsValid(T value);
    }

    static class EnumerableExtentions
    {
        public static ISet<T> ToSet<T>(this IEnumerable<T> enumerable)
        {
            return new HashSet<T>(enumerable);
        }
    }
}
