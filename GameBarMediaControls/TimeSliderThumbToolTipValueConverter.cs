using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace GameBarMediaControls
{
    public class TimeSliderThumbToolTipValueConverter : IValueConverter 
    {
        public object Convert(object value, Type targetType, object parameter, string language) {
            var span = TimeSpan.FromSeconds((double)value);

            if (span.TotalHours < 1.0) {
                return span.ToString(@"%m\:ss");
            }
            else {
                return span.ToString(@"%h\:mm\:ss");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            throw new NotImplementedException();
        }
    }
}
