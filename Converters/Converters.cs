using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Orange_Music_Player.Converters
{

    public class PlayingToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object paramter, string language)
        {
            if (value is bool && (bool)value)
                return Symbol.Pause;
            return Symbol.Play;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (value is Symbol && (Symbol)value == Symbol.Pause);
        }
    }

    public class ZeroModeToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((int)value == 0)
            {
                return 0.3;
            }
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }

    public class RepeatModeToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object paramter, string language)
        {
            if (targetType != typeof(Symbol))
                throw new InvalidOperationException("The target must be a Symbol");

            switch ((int)value)
            {
                case 0:
                    return Symbol.RepeatAll;
                case 1:
                    return Symbol.RepeatAll;
                case 2:
                    return Symbol.RepeatOne;
                default:
                    return Symbol.RepeatAll;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
