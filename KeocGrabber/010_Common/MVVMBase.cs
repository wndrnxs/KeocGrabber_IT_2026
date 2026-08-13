/*
 *******************************************************************************
 * 해당 소스는 현장 유지 보수 외에 다른 목적의 사용을 금합니다.
 * - 주식회사 태루 -
 *******************************************************************************
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;

namespace MVVMBase
{
    public class CommandBehavior
    {
        private class EventRaiseAttribute : Attribute
        {
        }

        #region Command 
        public static readonly DependencyProperty CommandProperty =
        DependencyProperty.RegisterAttached(
        "Command",
        typeof(ICommand),
        typeof(CommandBehavior),
        new PropertyMetadata(OnCommandChanged));

        public static ICommand GetCommand(DependencyObject d)
        {
            return d.GetValue(CommandProperty) as ICommand;
        }

        public static void SetCommand(DependencyObject d, ICommand value)
        {
            d.SetValue(CommandProperty, value);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        #endregion
        #region Event 
        public static readonly DependencyProperty EventProperty =
        DependencyProperty.RegisterAttached(
        "Event",
        typeof(string),
        typeof(CommandBehavior),
        new PropertyMetadata(OnEventChanged));

        private static void OnEventChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            BindEvent(d, e.NewValue as string);
        }

        public static string GetEvent(DependencyObject d)
        {
            return d.GetValue(EventProperty) as string;
        }

        public static void SetEvent(DependencyObject d, string value)
        {
            d.SetValue(EventProperty, value);
        }

        private static void BindEvent(DependencyObject owner, string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return;
            }

            var eventInfo = owner.GetType().GetEvent(eventName, BindingFlags.Public | BindingFlags.Instance);

            if (eventInfo == null)
            {
                throw new InvalidOperationException(String.Format("Could not resolve event name {0}", eventName));
            }

            var types = typeof(CommandBehavior).GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
            MethodInfo method = null;
            foreach (var type in types)
            {
                var attributes = type.GetCustomAttributes(true);
                if (attributes.OfType<EventRaiseAttribute>().Any())
                {
                    method = type;
                    break;
                }
            }

            if (method == null)
            {
                Debug.Assert(false, string.Format("invalid method type. type = {0}", eventName));
                return;
            }
            var eventHandler = Delegate.CreateDelegate(eventInfo.EventHandlerType, null, method);
            owner.SetValue(EventHandlerProperty, eventHandler);
            //Register the handler to the Event 
            eventInfo.AddEventHandler(owner, eventHandler);
        }

        [EventRaise]
        private void OnEventRaised(object sender, EventArgs e)
        {
            var dependencyObject = sender as DependencyObject;
            if (dependencyObject == null)
            {
                return;
            }
            var command = dependencyObject.GetValue(CommandProperty) as ICommand;
            if (command == null)
            {
                return;
            }

            if (command.CanExecute(null) == false)
            {
                return;
            }
            command.Execute(e);
        }
        #endregion
        #region EventHandler 
        public static readonly DependencyProperty EventHandlerProperty =
        DependencyProperty.RegisterAttached(
        "EventHandler",
        typeof(Delegate),
        typeof(CommandBehavior));

        public static Delegate GetEventHandler(DependencyObject d)
        {
            return d.GetValue(EventHandlerProperty) as Delegate;
        }
        public static void SetEventHandler(DependencyObject d, Delegate value)
        {
            d.SetValue(EventHandlerProperty, value);
        }
        #endregion
    }
    //---------------------------------------------------------------------------
    /*
<!--  Basic usage  -->
<Button Click="{data:MethodBinding OpenFromFile}" Content="Open" />

<!--  Pass in a binding as a method argument  -->
<Button Click="{data:MethodBinding Save, {Binding CurrentItem}}" Content="Save" />

<!--  Another example of a binding, but this time to a property on another element  -->
<ComboBox x:Name="ExistingItems" ItemsSource="{Binding ExistingItems}" />
<Button Click="{data:MethodBinding Edit, {Binding SelectedItem, ElementName=ExistingItems}}" />

<!--  Pass in a hard-coded method argument, XAML string automatically converted to the proper type  -->
<ToggleButton Checked="{data:MethodBinding SetWebServiceState, True}"
            Content="Web Service"
            Unchecked="{data:MethodBinding SetWebServiceState, False}" />

<!--  Pass in sender, and match method signature automatically -->
<Canvas PreviewMouseDown="{data:MethodBinding SetCurrentElement, {data:EventSender}, ThrowOnMethodMissing=False}">
<controls:DesignerElementTypeA />
<controls:DesignerElementTypeB />
<controls:DesignerElementTypeC />
</Canvas>

<!--  Pass in EventArgs  -->
<Canvas MouseDown="{data:MethodBinding StartDrawing, {data:EventArgs}}"
    MouseMove="{data:MethodBinding AddDrawingPoint, {data:EventArgs}}"
    MouseUp="{data:MethodBinding EndDrawing, {data:EventArgs}}" />

<!-- Support binding to methods further in a property path -->
<Button Content="SaveDocument" Click="{data:MethodBinding CurrentDocument.DocumentService.Save, {Binding CurrentDocument}}" />
//---------------------------------------------------------------------------
public void OpenFromFile();
public void Save(DocumentModel model);
public void Edit(DocumentModel model);

public void SetWebServiceState(bool state);

public void SetCurrentElement(DesignerElementTypeA element);
public void SetCurrentElement(DesignerElementTypeB element);
public void SetCurrentElement(DesignerElementTypeC element);

public void StartDrawing(MouseEventArgs e);
public void AddDrawingPoint(MouseEventArgs e);
public void EndDrawing(MouseEventArgs e);

public class Document
{
// Fetches the document service for handling this document
public DocumentService DocumentService { get; }
}

public class DocumentService
{
public void Save(Document document);
}
     */
    #region MethodBinding
    public class MethodBindingExtension : MarkupExtension
    {
        private static readonly List<DependencyProperty> StorageProperties = new List<DependencyProperty>();

        public string MethodName { get; }
        public PropertyPath MethodTargetPath { get; }

        private readonly object[] _methodArguments;
        private DependencyProperty _methodTargetProperty;
        private readonly List<DependencyProperty> _argumentProperties = new List<DependencyProperty>();

        public MethodBindingExtension()
        {
        }

        public MethodBindingExtension(string path) : this(path, null) { }
        public MethodBindingExtension(string path, object argument) : this(path, new object[] { argument }) { }
        public MethodBindingExtension(string path, object arg0, object arg1) : this(path, new object[] { arg0, arg1 }) { }
        public MethodBindingExtension(string path, object arg0, object arg1, object arg2) : this(path, new object[] { arg0, arg1, arg2 }) { }
        public MethodBindingExtension(string path, object arg0, object arg1, object arg2, object arg3) : this(path, new object[] { arg0, arg1, arg2, arg3 }) { }
        public MethodBindingExtension(string path, object arg0, object arg1, object arg2, object arg3, object arg4) : this(path, new object[] { arg0, arg1, arg2, arg3, arg4 }) { }

        public MethodBindingExtension(string path, object[] arguments)
        {
            if (path == null)
                throw new ArgumentNullException("path");

            _methodArguments = arguments ?? new object[0];

            int pathSeparatorIndex = path.LastIndexOf('.');

            if (pathSeparatorIndex != -1)
            {
                MethodTargetPath = new PropertyPath(path.Substring(0, pathSeparatorIndex), null);
            }

            MethodName = path.Substring(pathSeparatorIndex + 1);
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var provideValueTarget = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
            var target = provideValueTarget.TargetObject as FrameworkElement;
            
            if (target == null)
            {
                return this;
            }

            _methodTargetProperty = GetUnusedStorageProperty(target);

            var methodTargetBinding = new Binding();
            methodTargetBinding.Path = MethodTargetPath;
            target.SetBinding(_methodTargetProperty, methodTargetBinding);

            foreach (var argument in _methodArguments)
            {
                var argumentProperty = GetUnusedStorageProperty(target);
                var markupExtension = argument as MarkupExtension;

                if (markupExtension != null)
                {
                    var value = markupExtension.ProvideValue(new ServiceProvider(target, argumentProperty));
                    target.SetValue(argumentProperty, value);
                }
                else
                {
                    target.SetValue(argumentProperty, argument);
                }

                _argumentProperties.Add(argumentProperty);
            }

            return CreateEventHandler(target, provideValueTarget.TargetProperty);
        }

        private Delegate CreateEventHandler(FrameworkElement target, object objInfo)
        {
            EventHandler handler = (sender, eventArgs) =>
            {
                var methodTarget = target.GetValue(_methodTargetProperty);

                if (methodTarget == null)
                {
                    Console.WriteLine("[MethodBinding] Target could not be resolved.");
                    return;
                }

                var arguments = new object[_argumentProperties.Count];

                for (int i = 0; i < _argumentProperties.Count; i++)
                {
                    var argValue = target.GetValue(_argumentProperties[i]);

                    if (argValue is EventSenderExtension)
                    {
                        argValue = sender;
                    }
                    else if (argValue is EventArgsExtension)
                    {
                        argValue = eventArgs;
                    }

                    arguments[i] = argValue;
                }

                var methodTargetType = methodTarget.GetType();

                // Try invoking the method by resolving it based on the arguments provided

                try
                {
                    methodTargetType.InvokeMember(MethodName, BindingFlags.InvokeMethod, null, methodTarget, arguments);
                    return;
                }
                catch (MissingMethodException) { }

                // Couldn't match a method with the raw arguments, so check if we can find a method with the same name
                // and parameter count and try to convert any XAML string arguments to match the method parameter types

                var method = methodTargetType.GetMethods().SingleOrDefault(m => m.Name == MethodName && m.GetParameters().Length == arguments.Length);

                if (method != null)
                {
                    var parameters = method.GetParameters();

                    for (int i = 0; i < _methodArguments.Length; i++)
                    {
                        if (arguments[i] == null)
                        {
                            if (parameters[i].ParameterType.IsValueType)
                                method = null;
                            break;
                        }
                        else if (_methodArguments[i] is string && parameters[i].ParameterType != typeof(string))
                        {
                            // The original value provided for this argument was a XAML string so try to convert it

                            arguments[i] = TypeDescriptor.GetConverter(parameters[i].ParameterType).ConvertFromString((string)_methodArguments[i]);
                        }
                        else if (!parameters[i].ParameterType.IsInstanceOfType(arguments[i]))
                        {
                            method = null;
                            break;
                        }
                    }

                    method?.Invoke(methodTarget, arguments);
                }


                Console.WriteLine($"[MethodBinding] Could not find a method '{MethodName}' on target type '{methodTarget.GetType()}' that accepts the parameters provided.");
            };

            return Delegate.CreateDelegate(HandlerType(objInfo), handler.Target, handler.Method);
        }

        public static Type HandlerType(object handler)
        {
            var targetEvent = handler as EventInfo;
            if (targetEvent != null)
            {
                return targetEvent.EventHandlerType;
            }
            var targetMethod = handler as MethodInfo;
            if (targetMethod != null)
            {
                var ps = targetMethod.GetParameters();
                return ps[1].ParameterType;
            }
            var targetDelegate = handler as PropertyInfo;
            if (targetDelegate != null)
            {
                return targetDelegate.PropertyType;
            }
            return null;
        }

        private DependencyProperty GetUnusedStorageProperty(DependencyObject obj)
        {
            foreach (var property in StorageProperties)
            {
                if (obj.ReadLocalValue(property) == DependencyProperty.UnsetValue)
                    return property;
            }

            var newProperty = DependencyProperty.RegisterAttached("Storage" + StorageProperties.Count, typeof(object), typeof(MethodBindingExtension), new PropertyMetadata());
            StorageProperties.Add(newProperty);

            return newProperty;
        }

        private class ServiceProvider : IServiceProvider, IProvideValueTarget
        {
            public object TargetObject { get; set; }
            public object TargetProperty { get; set; }

            public ServiceProvider(object targetObject, object targetProperty)
            {
                TargetObject = targetObject;
                TargetProperty = targetProperty;
            }

            public object GetService(Type serviceType)
            {
                if (serviceType.IsInstanceOfType(this))
                    return this;

                return null;
            }
        }
    }

    public class EventSenderExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }

    public class EventArgsExtension : MarkupExtension
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
    #endregion

//MouseUp
//MouseDown
//MouseEnter
//MouseLeave
//MouseLeftButtonDown
//MouseLeftButtonUp
//MouseMove
//MouseRightButtonDown
//MouseRightButtonUp
//MouseWheel
    #region MouseEvent
    public class MouseBehaviour
    {
        public static readonly DependencyProperty MouseUpCommandProperty =
            DependencyProperty.RegisterAttached("MouseUpCommand", typeof(ICommand),
            typeof(MouseBehaviour), new FrameworkPropertyMetadata(
            new PropertyChangedCallback(MouseUpCommandChanged)));

        public static readonly DependencyProperty MouseMoveCommandProperty =
            DependencyProperty.RegisterAttached("MouseMoveCommand", typeof(ICommand),
            typeof(MouseBehaviour), new FrameworkPropertyMetadata(
            new PropertyChangedCallback(MouseMoveCommandChanged)));

        private static void MouseMoveCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            //element.MouseMove += new MouseButtonEventHandler(element_MouseMove);
            element.MouseMove += new MouseEventHandler(element_MouseMove);
        }

        private static void MouseUpCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)d;

            element.MouseUp += new MouseButtonEventHandler(element_MouseUp);
        }

        static void element_MouseMove(object sender, MouseEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseMoveCommand(element);

            command.Execute(e);
        }

        static void element_MouseUp(object sender, MouseButtonEventArgs e)
        {
            FrameworkElement element = (FrameworkElement)sender;

            ICommand command = GetMouseUpCommand(element);

            command.Execute(e);
        }

        public static void SetMouseUpCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseUpCommandProperty, value);
        }

        public static void SetMouseMoveCommand(UIElement element, ICommand value)
        {
            element.SetValue(MouseMoveCommandProperty, value);
        }

        public static ICommand GetMouseUpCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseUpCommandProperty);
        }

        public static ICommand GetMouseMoveCommand(UIElement element)
        {
            return (ICommand)element.GetValue(MouseMoveCommandProperty);
        }
    }
    #endregion
    #region WindowService
    interface IWindowService<T> where T : Window, new()
    {
        void ShowWindow(object dataContext);
    }

    public class WindowService<T> : IWindowService<T> where T : Window, new()
    {
        private double _width = 800.0;
        private double _height = 450.0;
        private WindowStyle _windowstyle = WindowStyle.SingleBorderWindow;
        private ResizeMode _resizemode = ResizeMode.CanResize;
        private WindowStartupLocation _location = WindowStartupLocation.CenterOwner;
        private string _title = "WindowService";
        private Window _owner = null;
        public double Width { get { return _width; } set { _width = value; } }
        public double Height { get { return _height; } set { _height = value; } }
        public WindowStyle windowStyle { get { return _windowstyle; } set { _windowstyle = value; } }
        public ResizeMode resizeMode { get { return _resizemode; } set { _resizemode = value; } }
        public WindowStartupLocation location { get { return _location; } set { _location = value; } }
        public string title { get { return _title; } set { _title = value; } }
        public Window Owner { get { return _owner; } set { _owner = value; win.Owner = Owner; } }

        T win = new T();

        bool _bWinHide = false;
        bool _bForceClose = false;

        public WindowService(bool bWinHide = true)
        {
            _bWinHide = bWinHide;
            if(_bWinHide)
                win.Closing += Win_Closing;
        }

        public void Close(bool forceClose = false)
        {
            _bForceClose = forceClose;
            win.Close();
        }

        private void Win_Closing(object sender, CancelEventArgs e)
        {
            if (!_bForceClose)
            {
                e.Cancel = true;
                win.Hide();
            }
        }

        public void ShowWindow(object viewModel)
        {
            if (!_bWinHide)
                win = new T();
            win.DataContext = viewModel;
            win.Show();
        }

        public bool ShowDialog(object viewModel)
        {
            bool bRet = false;
            win = new T();
            win.Owner = Owner;
            win.DataContext = viewModel;
            bRet = win.ShowDialog() == true ? true : false;
            return bRet;
        }
    }
    #endregion

    [Serializable]
    public class IPropertyChanged: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        
        protected void OnPropertyChanged(string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public static class ViewFactory
    {
        private static Dictionary<Type, Page> _viewCache = new Dictionary<Type, Page>();
        public static Page GetView(Type type)
        {
            if (_viewCache.ContainsKey(type) == false)
            {
                var userControl = Activator.CreateInstance(type) as Page;

                if (userControl == null)
                {
                    throw new InvalidOperationException("Couldn't create user control" + type);
                }
                _viewCache.Add(type, userControl);
            }
            return _viewCache[type];
        }
    }

    public class ViewCache : Page
    {
        public ViewCache()
        {
            this.Unloaded += this.ViewCache_Unloaded;
        }
        void ViewCache_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Unloaded -= this.ViewCache_Unloaded;
            this.Content = null;
        }

        private Type _contentType;
        public Type ContentType
        {
            get { return this._contentType; }
            set
            {
                if (this._contentType == value)
                {
                    return;
                }
                this._contentType = value;
                this.Content = ViewFactory.GetView(this._contentType);
            }
        }
    }

    public static class DialogCloser
    {
        public static readonly DependencyProperty DialogResultProperty =
            DependencyProperty.RegisterAttached(
                "DialogResult",
                typeof(bool?),
                typeof(DialogCloser),
                new PropertyMetadata(DialogResultChanged));

        private static void DialogResultChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var window = d as Window;
            if (window != null && window.IsLoaded)
                window.DialogResult = e.NewValue as bool?;
        }
        public static void SetDialogResult(Window target, bool? value)
        {
            target.SetValue(DialogResultProperty, value);
        }
    }

    #region DelegateCommand Class
    public class DelegateCommand<T> : ICommand
    {

        private readonly Func<T, bool> canExecute;
        private readonly Action<T> execute;

        /// <summary>
        /// Initializes a new instance of the DelegateCommand class.
        /// </summary>
        /// <param name="execute">indicate an execute function</param>
        public DelegateCommand(Action<T> execute) : this(execute, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the DelegateCommand class.
        /// </summary>
        /// <param name="execute">execute function </param>
        /// <param name="canExecute">can execute function</param>
        public DelegateCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException("execute");
            this.canExecute = canExecute;
        }
        /// <summary>
        /// can executes event handler
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// implement of icommand can execute method
        /// </summary>
        /// <param name="o">parameter by default of icomand interface</param>
        /// <returns>can execute or not</returns>
        public bool CanExecute(object o)
        {
            return canExecute == null || canExecute((T)o);
        }

        /// <summary>
        /// implement of icommand interface execute method
        /// </summary>
        /// <param name="o">parameter by default of icomand interface</param>
        public void Execute(object o)
        {

            this.execute((T)o);
            //T param;
            //if(o == null)
            //{
            //    param = default;
            //}
            //else
            //{
            //    param = (T)Convert.ChangeType(o, typeof(T));
            //}
            //this.execute(param);
        }

        /// <summary>
        /// raise ca excute changed when property changed
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            if (this.CanExecuteChanged != null)
            {
                this.CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
    #endregion

    public static class FocusExtension
    {
        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }

        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }

        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
                "IsFocused", typeof(bool), typeof(FocusExtension),
                new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

        private static void OnIsFocusedPropertyChanged(
            DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var uie = (UIElement)d;
            if ((bool)e.NewValue)
            {
                uie.Focus(); // Don't care about false values.
            }
        }
    }

    /// <summary>
    /// ListBox AutoScroll attached properties
    /// </summary>
    public static class ListBoxBehavior
    {
        public static readonly DependencyProperty AutoScrollProperty = DependencyProperty.RegisterAttached(
            "AutoScroll",
            typeof(bool),
            typeof(System.Windows.Controls.ListBox),
            new PropertyMetadata(false));

        public static readonly DependencyProperty AutoScrollHandlerProperty =
            DependencyProperty.RegisterAttached(
                "AutoScrollHandler",
                typeof(AutoScrollHandler),
                typeof(System.Windows.Controls.ListBox));

        public static bool GetAutoScroll(System.Windows.Controls.ListBox instance)
        {
            return (bool)instance.GetValue(AutoScrollProperty);
        }

        public static void SetAutoScroll(System.Windows.Controls.ListBox instance, bool value)
        {
            AutoScrollHandler OldHandler = (AutoScrollHandler)instance.GetValue(AutoScrollHandlerProperty);
            if (OldHandler != null)
            {
                OldHandler.Dispose();
                instance.SetValue(AutoScrollHandlerProperty, null);
            }

            instance.SetValue(AutoScrollProperty, value);
            if (value)
            {
                instance.SetValue(AutoScrollHandlerProperty, new AutoScrollHandler(instance));
            }
        }
    }

    /// <summary>
    /// Handle auto scroll functionality
    /// </summary>
    public class AutoScrollHandler : DependencyObject, IDisposable
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            "ItemsSource",
            typeof(IEnumerable),
            typeof(AutoScrollHandler),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.None,
                ItemsSourcePropertyChanged));

        private System.Windows.Controls.ListBox target;

        public AutoScrollHandler(System.Windows.Controls.ListBox target)
        {
            this.target = target;
            var binding = new Binding("ItemsSource") { Source = this.target };
            BindingOperations.SetBinding(this, ItemsSourceProperty, binding);
        }

        public void Dispose()
        {
            BindingOperations.ClearBinding(this, ItemsSourceProperty);
        }

        public IEnumerable ItemsSource
        {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { this.SetValue(ItemsSourceProperty, value); }
        }

        private static void ItemsSourcePropertyChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((AutoScrollHandler)o).ItemsSourceChanged((IEnumerable)e.OldValue, (IEnumerable)e.NewValue);
        }

        private void ItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            var collection = oldValue as INotifyCollectionChanged;
            if (collection != null)
            {
                collection.CollectionChanged -= this.CollectionChangedEventHandler;
            }

            collection = newValue as INotifyCollectionChanged;
            if (collection != null)
            {
                collection.CollectionChanged += this.CollectionChangedEventHandler;
            }
        }

        private void CollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action != NotifyCollectionChangedAction.Add || e.NewItems == null || e.NewItems.Count < 1)
            {
                return;
            }

            this.target.ScrollIntoView(e.NewItems[e.NewItems.Count - 1]);
        }
    }
}
