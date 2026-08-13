/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace KeocGrabber
{
    public class ScoreLimiter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double dValue = 0.0;
            if (value != null)
            {
                double.TryParse(value.ToString(), out dValue);
            }
            return dValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                double dValue = 0.0;
                double.TryParse(value.ToString(), out dValue);
                if (dValue < 0)
                    dValue = 0;
                if (dValue > 1)
                    dValue = 1;
                return dValue;
            }
            catch
            {
                return 0.0;
            }
        }
    }

    public class AreaLimiter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int iValue = 0;
            if (value != null)
            {
                int.TryParse(value.ToString(), out iValue);
            }
            return iValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                int iValue = 0;
                int.TryParse(value.ToString(), out iValue);
                if (iValue < 0)
                    iValue = 0;
                if (iValue > 300)
                    iValue = 300;
                return iValue;
            }
            catch
            {
                return 0;
            }

        }
    }
    public class AreaMaxLimiter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int iValue = 0;
            if (value != null)
            {
                int.TryParse(value.ToString(), out iValue);
            }
            return iValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                int iValue = 0;
                int.TryParse(value.ToString(), out iValue);
                if (iValue < 800)
                    iValue = 800;
                if (iValue > 2000)
                    iValue = 2000;
                return iValue;
            }
            catch
            {
                return 0;
            }

        }
    }
    public class SizeXLimiter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double dValue = 0;
            if (value != null)
            {
                double.TryParse(value.ToString(), out dValue);
            }
            return dValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                double dValue = 0;
                double.TryParse(value.ToString(), out dValue);
                if (dValue < 0)
                    dValue = 0;
                if (dValue > 2)
                    dValue = 2;
                return dValue;
            }
            catch
            {
                return 0;
            }

        }
    }
    public class SizeYLimiter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double dValue = 0;
            if (value != null)
            {
                double.TryParse(value.ToString(), out dValue);
            }
            return dValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                double dValue = 0;
                double.TryParse(value.ToString(), out dValue);
                if (dValue < 0)
                    dValue = 0;
                if (dValue > 2)
                    dValue = 2;
                return dValue;
            }
            catch
            {
                return 0;
            }

        }
    }
}
