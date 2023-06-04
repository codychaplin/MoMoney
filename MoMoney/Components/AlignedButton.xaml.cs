using System.Windows.Input;

namespace MoMoney.Components
{
    public partial class AlignedButton : ContentView
    {
        public static readonly BindableProperty CommandProperty =
                BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(AlignedButton), null);

        public static readonly BindableProperty TextProperty =
            BindableProperty.Create(nameof(Text), typeof(string), typeof(AlignedButton), string.Empty);

        public static readonly BindableProperty ShowArrowProperty =
            BindableProperty.Create(nameof(ShowArrow), typeof(bool), typeof(AlignedButton), false);

        public ICommand Command
        {
            get => (ICommand)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public bool ShowArrow
        {
            get => (bool)GetValue(ShowArrowProperty);
            set => SetValue(ShowArrowProperty, value);
        }

        public AlignedButton()
        {
            InitializeComponent();
        }
    }
}