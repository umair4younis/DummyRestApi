using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Puma.MDE.Common.Utilities
{
    public static class ReactiveEx
    {
        public static IObservable<D> ObservableProperty<D>(Expression<Func<D>> propertyExtension)
        {
            if (propertyExtension == null)
                throw new ArgumentNullException("propertyExtension");

            MemberExpression memberExpression = null;
            if (propertyExtension.Body is MemberExpression)
            {
                memberExpression = propertyExtension.Body as MemberExpression;


            }
            else
            {
                var body = propertyExtension.Body;
                if (body.NodeType == ExpressionType.Convert)
                {
                    body = ((UnaryExpression)body).Operand;
                    if (body is MemberExpression)
                    {
                        memberExpression = body as MemberExpression;
                    }
                }
            }

            if (memberExpression == null)
                throw new ArgumentException("The expression is not a member access expression.", "propertyExtension");

            var member = memberExpression.Expression;

            if (member == null)
                throw new ArgumentNullException("propertyExtension", "member");

            var property = memberExpression.Member as PropertyInfo;

            if (property == null)
                throw new ArgumentException("The member access expression does not access a property.");

            var constantExpression = (ConstantExpression)member;

            if (constantExpression.Type.GetInterface("INotifyPropertyChanged", false) == null)
                throw new ArgumentException("The member doesn't implement INotifyPropertyChanged interface");

            return null;
        }

        public static IObservable<D> ObservableProperty<D>(object propertyValue, Expression<Func<D>> propertyExtension)
        {
            if (propertyExtension == null)
                throw new ArgumentNullException("propertyExtension");

            MemberExpression memberExpression = null;
            if (propertyExtension.Body is MemberExpression)
            {
                memberExpression = propertyExtension.Body as MemberExpression;
            }
            else
            {
                var body = propertyExtension.Body;
                if (body.NodeType == ExpressionType.Convert)
                {
                    body = ((UnaryExpression)body).Operand;
                    if (body is MemberExpression)
                    {
                        memberExpression = body as MemberExpression;
                    }
                }
            }

            if (memberExpression == null)
                throw new ArgumentException("The expression is not a member access expression.", "propertyExtension");

            var member = memberExpression.Expression;

            if (member == null)
                throw new ArgumentNullException("propertyExtension", "member");

            var property = memberExpression.Member as PropertyInfo;

            if (property == null)
                throw new ArgumentException("The member access expression does not access a property.");

            var constantExpression = (ConstantExpression)member;

            if (constantExpression.Type.GetInterface("INotifyPropertyChanged", false) == null)
                throw new ArgumentException("The member doesn't implement INotifyPropertyChanged interface");

            return null;

        }

        public static IObservable<D> ObservablePropertyFrom<D>(D fromObject, string propertyName)
        {
            if (fromObject.GetType().GetInterface("INotifyPropertyChanged", false) == null)
                throw new ArgumentException("The member doesn't implement INotifyPropertyChanged interface");

            var property = fromObject.GetPropertyInfo(propertyName);
            return null;
     
        }

        public static IObservable<D> ObservablePropertyFrom<D>(D fromObject, object propertyValue, string propertyName)
        {
            if (fromObject.GetType().GetInterface("INotifyPropertyChanged", false) == null)
                throw new ArgumentException("The member doesn't implement INotifyPropertyChanged interface");

            var property = fromObject.GetPropertyInfo(propertyName);
            return null;

        }
        
        //public static IObservable<D> ObservableProperty<D>(params Expression<Func<D>>[] propertyExtensions)
        //{
        //    IObservable<D> observable = null;
        //    // If there is only one observe on that property
        //    if (propertyExtensions.Length >= 1)
        //    {
        //        observable = ReactiveEx.ObservableProperty(propertyExtensions[0]);
        //    }

        //    // Combine with others if there are more than one property to observe, start from index 1 
        //    for (var index = 1; index < propertyExtensions.Length; index++)
        //    {
        //        observable = observable.CombineLatest(ReactiveEx.ObservableProperty(propertyExtensions[index]), (a, b) => b);
        //    }

        //    return observable;
        //}

        //public static IObservable<D> ObservableProperty<D>(D fromObject, params string[] propertyExtensions)
        //{
        //    IObservable<D> observable = null;
        //    // If there is only one observe on that property
        //    if (propertyExtensions.Length >= 1)
        //    {
        //        observable = ReactiveEx.ObservablePropertyFrom(fromObject, propertyExtensions[0]);
        //    }

        //    // Combine with others if there are more than one property to observe, start from index 1 
        //    for (var index = 1; index < propertyExtensions.Length; index++)
        //    {
        //        observable = observable.CombineLatest(ReactiveEx.ObservablePropertyFrom(fromObject, propertyExtensions[index]), (a, b) => b);
        //    }

        //    return observable;
        //}

        
    }
}
