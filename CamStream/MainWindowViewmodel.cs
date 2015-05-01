
namespace CamStream
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Reactive;
    using System.Reactive.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading.Tasks;
    using System.Windows.Media;
    using AForge.Video;
    using AForge.Video.DirectShow;
    using Annotations;
    using EasyNetQ;
    using EasyNetQ.Topology;

    public class MainWindowViewmodel : INotifyPropertyChanged, IDisposable
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoSource;
        private IObservable<Bitmap> frames;
        private IBus bus;
        private IExchange exchange;
        private IDisposable frameSubscription;

        public MainWindowViewmodel()
        {
            InitializeCam();
            Observable.FromAsync(InitializeComm).Subscribe(b => Debug.Print("Connected"));
        }

        public FilterInfoCollection VideoDevices
        {
            get { return videoDevices; }
            set
            {
                if (Equals(value, videoDevices)) return;
                videoDevices = value;
                OnPropertyChanged();
            }
        }

        public IObservable<Bitmap> Frames
        {
            get { return frames; }
            set
            {
                lock (this)
                {

                    if (Equals(value, frames)) return;
                    frames = value;
                    OnPropertyChanged();
                }
            }
        }

        public VideoCaptureDevice VideoSource
        {
            get { return videoSource; }
            set { videoSource = value; }
        }

        private void InitializeCam()
        {
            // enumerate video devices
            VideoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
        }

        private async Task<bool> InitializeComm()
        {
            return
                await
                    Task.Factory.StartNew(() =>
                    {
                        bus = RabbitHutch.CreateBus("host=lhtest1.cloudapp.net:5672;username=broker;password=Start123!");
                        exchange = bus.Advanced.ExchangeDeclare("'CamStream", "fanout");
                        return true;
                    });

        }

        public void StartCam(string monikerString)
        {
            VideoSource = new VideoCaptureDevice(monikerString);
            // set NewFrame event handler
            var vse = Observable.FromEventPattern<NewFrameEventArgs>(VideoSource, "NewFrame");
            Frames = from e in vse select e.EventArgs.Frame.Clone() as Bitmap;
            frameSubscription = Frames.Subscribe(f =>
            {
                if (bus != null && bus.IsConnected)
                    bus.Advanced.Publish(exchange, "", false, false, new Message<Bitmap>(f));
            });

            // start the video source
            VideoSource.Start();
        }

        public void StopCam()
        {
            if (videoSource != null)
                videoSource.SignalToStop();
        }


        //------------------------------------------------------------------------
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                bus.Dispose();
                if (videoSource != null)
                {
                    videoSource.SignalToStop();
                    videoSource.WaitForStop();
                }
                if (videoDevices != null) 
                    videoDevices.Clear();
                if (frameSubscription != null) 
                    frameSubscription.Dispose();
            }
        }
    }
}
