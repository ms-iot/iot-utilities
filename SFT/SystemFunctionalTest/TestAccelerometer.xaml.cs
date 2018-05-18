//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using System;
using System.Globalization;
using Windows.Devices.Sensors;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SystemFunctionalTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestAccelerometer : Page
    {
        #region Fields

        private const uint _margin = 200;
        private Accelerometer _accelerometer;
        private bool isStarted = false;

        #endregion // Fields

        #region Constructor

        /// <summary>
        /// constructor of class TestAccelerometer
        /// </summary>
        public TestAccelerometer()
        {
            this.InitializeComponent();
        }

        #endregion // Constructor

        #region Override methods

        /// <summary>
        /// Called when a page becomes the active page in a frame
        /// </summary>
        /// <param name="e">An object that contains the event data</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Activates a display request
            DisplayRequest displayRequest = new DisplayRequest();
            displayRequest.RequestActive();

            isStarted = true;

            App.LogStart(this.Name);

            InitializeAccelerometer();
        }

        /// <summary>
        /// Called when a page is no longer the active page in a frame
        /// </summary>
        /// <param name="e">An object that contains the event data</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (isStarted)
            {
                isStarted = false;
                App.LogFinish(this.Name);
            }
            base.OnNavigatedFrom(e);
        }

        #endregion // Override methods

        /// <summary>
        /// Initialize settings for Accelerometer.
        /// </summary>
        private void InitializeAccelerometer()
        {
            txtTitle.Text = String.Format(CultureInfo.CurrentCulture, App.LoadString("Test"), App.LoadString("Accelerometer"));

            try
            {
                // Instantiate the Accelerometer.
                _accelerometer = Accelerometer.GetDefault();

                if (_accelerometer == null)
                {
                    // The device on which the application is running does not support the gyrometer sensor
                    txtStatus.Text = String.Format(CultureInfo.CurrentCulture, App.LoadString("NotSupported"), App.LoadString("Accelerometer"));
                    App.LogError(txtStatus.Text);
                }
                else
                {
                    // Add ReadingChanged event handler
                    _accelerometer.ReadingChanged += accelerometer_ReadingChanged;
                }
            }
            catch (NotSupportedException)
            {
                _accelerometer = null;
                txtStatus.Text = App.LoadString("Error");
                App.LogError(txtStatus.Text);
            }
            catch (Exception ex)
            {
                _accelerometer = null;
                txtStatus.Text = App.LoadString("Error") + " (" + ex.Message + ")";
                App.LogError(txtStatus.Text);
            }
        }

        /// <summary>
        /// <see cref="Accelerometer.ReadingChanged"/> event handler for accelerometer value changes.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">The <see cref="AccelerometerReadingChangedEventArgs"/> instance containing the event data.</param>
        private async void accelerometer_ReadingChanged(object sender, AccelerometerReadingChangedEventArgs e)
        {
            if (!isStarted) return;

            double x = Math.Min(1, e.Reading.AccelerationX);
            double y = Math.Min(1, e.Reading.AccelerationY);

            double square = x * x + y * y;
            if (square > 1)
            {
                x /= Math.Sqrt(square);
                y /= Math.Sqrt(square);
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                string reading = String.Format(CultureInfo.CurrentCulture, "X: {0,5:0.00} (g)\r\nY: {1,5:0.00} (g)\r\nZ: {2,5:0.00} (g)",
                                               e.Reading.AccelerationX,
                                               e.Reading.AccelerationY,
                                               e.Reading.AccelerationZ);
                txtStatus.Text = reading;
                canvasBall.Margin = new Thickness() { Left = _margin * x, Bottom = _margin * y };

                App.LogComment(reading.Replace("\r\n", " "));
            });
        }

        /// <summary>
        /// <see cref="Button.Click"/> event handler.
        /// Handle Pass/Fail/Retry test result conditions.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Result_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            App.ResultControl(button.Name, this.Name);
        }
    }
}
