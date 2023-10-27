using System.ComponentModel;

namespace Utilities
{
    public interface IValueHolderReadOnly<T> : INotifyPropertyChanged
    {
        T Value { get; }

        event ValueChangedEventHandler ValueChanged;
    }

    public interface IValueHolder<T> : IValueHolderReadOnly<T>
    {
        new T Value { get; set; }
    }

    public class ValueHolder<T> : IValueHolder<T>
    {
        private T _value;
        public T Value
        {
            get { return _value; }
            set
            {
                if (value == null && _value == null)
                    return;

                if (value != null && value.Equals(_value))
                    return;

                _value = value;
                ValueChanged?.Raise();
                PropertyChanged?.Raise(() => Value);
            }
        }

        T IValueHolderReadOnly<T>.Value { get { return _value; } }

        public ValueHolder(T val)
        {
            _value = val;
        }

        public ValueHolder() { }

        public event ValueChangedEventHandler ValueChanged;
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public static class ValueHolder
    {
        public static ValueHolder<T> Create<T>(T val)
        {
            return new ValueHolder<T>(val);
        }
    }
}
