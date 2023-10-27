using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Utilities
{
    public delegate void ValueChangedEventHandler();

    public static class EventHandlerExtensions
    {
        public static void Raise<T>(this PropertyChangedEventHandler handler, Expression<Func<T>> propertyExpression)
        {
            if (handler == null)
                return;

            var body = propertyExpression.Body as MemberExpression;
            var expression = body?.Expression as ConstantExpression;
            handler(expression?.Value, new PropertyChangedEventArgs(body?.Member.Name));
        }
        public static void RaiseAllProperties(this PropertyChangedEventHandler handler, object sender)
        {
            if (handler == null)
                return;

            handler(sender, new PropertyChangedEventArgs(null));
        }

        public static void Raise(this EventHandler handler, object sender)
        {
            if (handler == null)
                return;

            handler(sender, new EventArgs());
        }

        public static void Raise(this ValueChangedEventHandler handler)
        {
            if (handler == null)
                return;

            handler();
        }
    }
}
