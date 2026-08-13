/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows.Data;
using KeocGrabber.Properties;

namespace MVVMBase
{
    public class LocExtension : Binding
    {
        public LocExtension(string name) : base("[" + name + "]")
        {
            this.Mode = BindingMode.OneWay;
            this.Source = TranslationSource.Instance;
        }
    }

    class TranslationSource : INotifyPropertyChanged
    {
        static public CultureInfo LANG_EN = new CultureInfo("en-us");
        static public CultureInfo LANG_KO = new CultureInfo("ko-kr");
        private const char KeySeparator = '_';
        private static readonly TranslationSource instance = new TranslationSource();

        public static TranslationSource Instance
        {
            get { return instance; }
        }

        private readonly ResourceManager resManager = Resources.ResourceManager;
        private CultureInfo currentCulture = null;

        public string this[string key]
        {

            get
            {
                if (key.IndexOf(KeySeparator) == -1)
                {
                    var res = this.resManager.GetString(key, this.currentCulture);
                    return !string.IsNullOrWhiteSpace(res) ? res : $"[{key}]";
                }
                else
                {
                    var keys = key.Split(KeySeparator);
                    var results = new List<string>();
                    foreach (var k in keys)
                    {
                        var res = this.resManager.GetString(k, this.currentCulture);
                        results.Add(!string.IsNullOrWhiteSpace(res) ? res : $"[{k}]");
                    }
                    return string.Join(" ", results);
                }
            }
        }

        public CultureInfo CurrentCulture
        {
            get { return this.currentCulture; }
            set
            {
                if (this.currentCulture != value)
                {
                    this.currentCulture = value;
                    var @event = this.PropertyChanged;
                    if (@event != null)
                    {
                        @event.Invoke(this, new PropertyChangedEventArgs(string.Empty));
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
