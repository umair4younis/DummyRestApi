using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Puma.MDE.Common.Utilities
{
    public static class Expressions
    {
        public static string PropertyNameFor<T>(Expression<Func<T, object>> expression)
        {
            var body = expression.Body;
            return GetMemberName(body);
        }

        public static string PropertyNameFor(Expression<Func<object>> expression)
        {
            var body = expression.Body;
            return GetMemberName(body);
        }

        public static string PropertyNameFor<T>(Expression<Func<T>> property)
        {
            var lambda = (LambdaExpression)property;
            MemberExpression memberExpression;

            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                memberExpression = (MemberExpression)lambda.Body;
            }

            return memberExpression.Member.Name;
        }

        public static object InstanceContaining<T>(Expression<Func<T>> property)
        {
            var lambda = (LambdaExpression)property;
            MemberExpression memberExpression;

            if (lambda.Body is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)lambda.Body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                memberExpression = (MemberExpression)lambda.Body;
            }

            var constantExpression = (ConstantExpression)memberExpression.Expression;

            return constantExpression.Value;
        }

        public static string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> expression)
        {
            var memberExpression = expression.Body as MemberExpression;

            if (memberExpression == null)
                throw new ArgumentException("The given expression is not a MemberExpression.", "expression");

            var memberPropertyExpression = memberExpression.Member as PropertyInfo;

            if (memberPropertyExpression == null)
                new ArgumentException("The given expression is not a property.", "expression");


            if (memberPropertyExpression != null) 
                return memberPropertyExpression.Name;

            return null;
        }

        private static string GetMemberName(Expression expression)
        {
            if (expression is MemberExpression)
            {
                var memberExpression = (MemberExpression)expression;

                if (memberExpression.Expression.NodeType == ExpressionType.MemberAccess)
                {
                    return GetMemberName(memberExpression.Expression)
                        + "."
                        + memberExpression.Member.Name;
                }

                return memberExpression.Member.Name;
            }

            if (expression is UnaryExpression)
            {
                var unaryExpression = (UnaryExpression)expression;

                if (unaryExpression.NodeType != ExpressionType.Convert)
                {
                    throw new Exception(string.Format("Cannot interpret member from {0}", expression));
                }

                return GetMemberName(unaryExpression.Operand);
            }

            throw new Exception(string.Format(
                "Could not determine member from {0}",
                expression));
        }

        public static MethodInfo GetMethod<T, TMethod>(Expression<Func<T, TMethod>> expression)
        {
            var methodCall = expression.Body as MethodCallExpression;

            if (methodCall != null) 
                return methodCall.Method;

            return null;
        }

        //private static string GetPropertyNameFromExpression<T>(Expression<Func<T>> property)
        //{
        //    var lambda = (LambdaExpression)property;
        //    MemberExpression memberExpression;

        //    if (lambda.Body is UnaryExpression)
        //    {
        //        var unaryExpression = (UnaryExpression)lambda.Body;
        //        memberExpression = (MemberExpression)unaryExpression.Operand;
        //    }
        //    else
        //    {
        //        memberExpression = (MemberExpression)lambda.Body;
        //    }

        //    return memberExpression.Member.Name;
        //}
    }

     
}
