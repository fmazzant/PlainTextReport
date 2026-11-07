/// <summary>
/// 
/// The MIT License (MIT)
/// 
/// Copyright (c) 2024 Federico Mazzanti
/// 
/// Permission is hereby granted, free of charge, to any person
/// obtaining a copy of this software and associated documentation
/// files (the "Software"), to deal in the Software without
/// restriction, including without limitation the rights to use,
/// copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the
/// Software is furnished to do so, subject to the following
/// conditions:
/// 
/// The above copyright notice and this permission notice shall be
/// included in all copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
/// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
/// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
/// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
/// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
/// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
/// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
/// OTHER DEALINGS IN THE SOFTWARE.
/// 
/// </summary>

namespace Mafesoft.PlainTextReport.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Reads the properties and the fields selected by the column expressions
    /// </summary>
    internal static class ExpressionExtensions
    {
        /// <summary>
        /// Returns the name of the selected member: "Name" for m => m.Name,
        /// "Customer.Name" for m => m.Customer.Name
        /// </summary>
        /// <param name="expression">A lambda expression that selects a property or a field</param>
        /// <returns></returns>
        public static string GetPropertyName(this LambdaExpression expression)
        {
            return string.Join(".", GetMemberPath(expression).Select(member => member.Name));
        }

        /// <summary>
        /// Compiles a getter of the selected member. When an intermediate value is null
        /// (m.Customer in m => m.Customer.Name) the getter returns null.
        /// </summary>
        /// <typeparam name="M">The model type</typeparam>
        /// <param name="expression">A lambda expression that selects a property or a field</param>
        /// <returns></returns>
        public static Func<M, object> CreateGetter<M>(this LambdaExpression expression)
        {
            var path = GetMemberPath(expression);
            var model = Expression.Parameter(typeof(M), "model");
            var body = Access(model, path, 0);
            return Expression.Lambda<Func<M, object>>(body, model).Compile();
        }

        /// <summary>
        /// Returns the members selected by the expression, from the model to the value
        /// </summary>
        private static List<MemberInfo> GetMemberPath(LambdaExpression expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var path = new List<MemberInfo>();
            var current = StripConvert(expression.Body);
            while (current is MemberExpression member)
            {
                if (!(member.Member is PropertyInfo) && !(member.Member is FieldInfo))
                {
                    break;
                }
                path.Insert(0, member.Member);
                current = member.Expression == null ? null : StripConvert(member.Expression);
            }

            if (path.Count == 0 || current != expression.Parameters[0])
            {
                throw new ArgumentException($"The expression {expression} must select a property or a field of the model, for example m => m.Name or m => m.Customer.Name.", nameof(expression));
            }
            return path;
        }

        /// <summary>
        /// Builds the access to the members, with a null check on every intermediate value
        /// </summary>
        private static Expression Access(Expression instance, List<MemberInfo> path, int index)
        {
            var value = Expression.MakeMemberAccess(instance, path[index]);
            if (index == path.Count - 1)
            {
                return Expression.Convert(value, typeof(object));
            }

            var next = Access(value, path, index + 1);
            if (value.Type.IsValueType && Nullable.GetUnderlyingType(value.Type) == null)
            {
                return next;
            }
            return Expression.Condition(
                Expression.Equal(value, Expression.Constant(null, value.Type)),
                Expression.Constant(null, typeof(object)),
                next);
        }

        /// <summary>
        /// Removes the conversions, for example the boxing of m => (object)m.Id
        /// </summary>
        private static Expression StripConvert(Expression expression)
        {
            while (expression.NodeType == ExpressionType.Convert || expression.NodeType == ExpressionType.ConvertChecked)
            {
                expression = ((UnaryExpression)expression).Operand;
            }
            return expression;
        }
    }
}
