using Android.App;
using Android.Widget;
using Android.OS;
using Android.Hardware;
using Android.Content;
using Java.Security;
using Java.Util;
using System;
using System.Threading.Tasks;
using Android.Media;
using Android.Content.Res;
using Android.Provider;
using Android.Gms.Common.Apis;
using Android.Gms.Common;
using Android.Gms.Games;
using Android.Gms.Games.MultiPlayer.RealTime;
using System.Collections.Generic;

namespace EarPong.Droid
{
    [Activity(Label = "EarPong", MainLauncher = true, Icon = "@mipmap/EarPongIcon")]
    public class MainActivity : Activity, ISensorEventListener, GoogleApiClient.IConnectionCallbacks, GoogleApiClient.IOnConnectionFailedListener, IRoomUpdateListener, IRealTimeMessageReceivedListener
    {
        static readonly object _syncLock = new object();
        SensorManager _sensorManager;
        Sensor _sensor;
        TextView _gyroText;
        private GoogleApiClient mGoogleApiClient;
        PlaybackParams playbackParams = new PlaybackParams();
        LinearLayout mainWindow;

        MediaPlayer ballHit, goingAway, comingBack;

        bool canHit = true;
        float hitSpeed = 1f;

        protected override void OnCreate (Bundle savedInstanceState)
        {
            base.OnCreate (savedInstanceState);
            // Set our view from the "main" layout resource
            mGoogleApiClient = new GoogleApiClient.Builder (this)
                                                  .AddConnectionCallbacks (this)
                                                  .AddOnConnectionFailedListener (this)
                                                  .AddApi (GamesClass.API).AddScope (GamesClass.ScopeGames)
                                                  .Build ();
            SetContentView (Resource.Layout.Main);
            _sensorManager = (SensorManager)GetSystemService (Context.SensorService);
            _sensor = _sensorManager.GetDefaultSensor (SensorType.Accelerometer);
            mainWindow = FindViewById<LinearLayout> (Resource.Id.mainWindow);
            Console.WriteLine("Connecting......");
            mGoogleApiClient.Connect();
            //player.SetDataSource(this, Settings.System.DefaultNotificationUri);
            mainWindow.Click+=delegate {
                startQuickGame ();
            };
            ballHit = MediaPlayer.Create(this, Resource.Raw.blop);
            goingAway = MediaPlayer.Create(this, Resource.Raw.goingAwayBloing);
            comingBack = MediaPlayer.Create(this, Resource.Raw.comingBackBloing);


            ballHit.Completion += (object sender, EventArgs e) => FlyAway();
            goingAway.Completion += (object sender, EventArgs e) => ComeBack();
            comingBack.Completion += (object sender, EventArgs e) => canHit = true;

        }

        private void ComeBack()
        {
            var timer = new System.Timers.Timer();
			timer.Interval = 1500;
			timer.Elapsed += delegate
			{
                comingBack.PlaybackParams.SetSpeed(hitSpeed);
				comingBack.Start();
				timer.Stop();
			};
			timer.Start();
        }

        private void FlyAway()
        {
			Console.WriteLine("Hit ball!");
			//Cannot hit ball while it is flying
			canHit = false;
            goingAway.PlaybackParams.SetSpeed(hitSpeed);
			//Set audio parameters to adjust for hit speed
			//SetParameters(1);

            //Set a timer to wait .5 seconds before playing flying away sound
            var timer = new System.Timers.Timer(); 
			timer.Interval = 500;
			timer.Elapsed += delegate {
				goingAway.Start();
				timer.Stop();
			};
			timer.Start();
        }
        private void startQuickGame ()
        {
            // auto-match criteria to invite one random automatch opponent.
            // You can also specify more opponents (up to 3).
            Bundle am = RoomConfig.CreateAutoMatchCriteria (1, 1, 0);

            // build the room config:
            RoomConfig.Builder roomConfigBuilder = RoomConfig.InvokeBuilder (this);
            roomConfigBuilder.SetAutoMatchCriteria (am);
            roomConfigBuilder.SetMessageReceivedListener (this);
            RoomConfig roomConfig = roomConfigBuilder.Build ();

            // create room:
            GamesClass.RealTimeMultiplayer.Create (mGoogleApiClient, roomConfig);

            // prevent screen from sleeping during handshake WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON
            Window.AddFlags (Android.Views.WindowManagerFlags.KeepScreenOn);

            // go to game screen
        }
        protected override void OnResume()
        {
            base.OnResume();
            _sensorManager.RegisterListener(this,
                                           _sensor,
                                           SensorDelay.Ui);
        }

        protected override void OnPause()
        {
            base.OnPause();
            _sensorManager.UnregisterListener(this);
        }

        private void SetTimer(int timeInMs)
        {
            var timer = new System.Timers.Timer();
			timer.Interval = timeInMs;
			timer.Elapsed += delegate
			{
                canHit = true;
                timer.Stop();
			};
			timer.Start();
        }

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
            System.Diagnostics.Debug.WriteLine("Accuracy Changed. Should probably do something about that. \nAccuracy is: " + accuracy);
        }

        public void OnSensorChanged(SensorEvent e)
        {
            lock (_syncLock)
            {
				
            }

			if (e.Values[0] >= 3 && e.Values[2] >= 15 && e.Values[1] <= -32 && canHit)
			{
				//System.Diagnostics.Debug.WriteLine("Good swing");
				//System.Diagnostics.Debug.WriteLine("Y IS: " + e.Values[1] + " \n");
				ballHit.Start();
			}
        }

        public void OnConnected (Bundle connectionHint)
        {
            Console.WriteLine("Connected!");
        }

        public void OnConnectionSuspended (int cause)
        {
            Console.WriteLine("onConnectionSuspended() called. Trying to reconnect.");
            mGoogleApiClient.Connect ();
        }

        public void OnConnectionFailed (ConnectionResult result)
        {
            Console.WriteLine("onConnectionFailed() called, result: " + result);
            mGoogleApiClient.Connect();
        }

        public void OnJoinedRoom (int statusCode, IRoom room)
        {
            Console.WriteLine("Joined room");
        }

        public void OnLeftRoom (int statusCode, string roomId)
        {
            Console.WriteLine("left room");
        }

        public void OnRoomConnected (int statusCode, IRoom room)
        {
            Console.WriteLine("connected room");
        }

        public void OnRoomCreated (int statusCode, IRoom room)
        {
            Console.WriteLine("created room");
        }

        public void OnRealTimeMessageReceived (RealTimeMessage message)
        {
            Console.WriteLine("MESSAGE: "+message.ToString());
        }
    }
}

