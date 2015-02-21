namespace CamStream
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.Globalization;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Windows.Controls;
    using System.Windows.Media.Imaging;
    using System.Windows.Threading;
    using AForge.Video;
    using AForge.Video.DirectShow;
    using Annotations;
    using System.Windows.Media;
    using System.Reactive;
    using System.Reactive.Concurrency;
    using System.Reactive.Linq;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private MainWindowViewmodel vm;

        public MainWindow()
        {
            InitializeComponent();
            Vm = new MainWindowViewmodel();
            this.DataContext = Vm;
            this.UpdateLayout();
        }

        public MainWindowViewmodel Vm
        {
            get { return vm; }
            set { vm = value; }
        }

        private void Cameras_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var filterInfo = e.AddedItems[0] as FilterInfo;
            if (filterInfo != null)
            {
                string moniker = filterInfo.MonikerString;
                vm.StopCam();
                vm.StartCam(moniker);
            }

            Vm.Frames
                .ObserveOnDispatcher()
                .Subscribe(f =>
                {
                    Picture.Source = Imaging.CreateBitmapSourceFromBitmapDLL(f);
                });

        }

        protected override void OnClosing(CancelEventArgs e)
        {
            Vm.Dispose();
            base.OnClosing(e);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }



}
