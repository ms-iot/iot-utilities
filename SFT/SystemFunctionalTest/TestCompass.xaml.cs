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
    public sealed partial class TestCompass : Page
    {
        #region Fields

        private const MagnetometerAccuracy ACCURACY_THRESHOLD = MagnetometerAccuracy.Approximate;
        private Compass _compass = null;
        private bool isStarted = false;

        #endregion // Fields

        #region Constructor

        /// <summary>
        /// Constructor of class TestCompass
        /// </summary>
        public TestCompass()
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

            InitializeCompass();
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
        /// Initialize settings for Compass.
        /// </summary>
        private void InitializeCompass()
        {
            txtTitle.Text = String.Format(CultureInfo.CurrentCulture, App.LoadString("Test"), App.LoadString("Compass"));

            try
            {
                // Instantiate the Compass.
                _compass = Compass.GetDefault();

                if (_compass == null)
                {
                    // The device on which the application is running does not support the compass sensor
                    txtStatus.Text = String.Format(CultureInfo.CurrentCulture, App.LoadString("NotSupported"), App.LoadString("Compass"));
                }
                else
                {
                    // Add ReadingChanged event handler
                    _compass.ReadingChanged += compass_ReadingChanged;
                }
            }
            catch
            {
                _compass = null;
                txtStatus.Text = App.LoadString("Error");
            }
        }

        /// <summary>
        /// <see cref="Compass.ReadingChanged"/> event handler for compass value changes.
        /// </summary>
        /// <param name="sender">source object that issue this event</param>
        /// <param name="e">The <see cref="CompassReadingChangedEventArgs"/> instance containing the event data.</param>
        private async void compass_ReadingChanged(object sender, CompassReadingChangedEventArgs e)
        {
            if (!isStarted) return;

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (e.Reading.HeadingAccuracy < ACCURACY_THRESHOLD)
                {
                    txtMessage.Text = App.LoadString("RequireCalibration");
                }
                else
                {
                    txtMessage.Text = "";
                }

                string reading = String.Format(CultureInfo.CurrentCulture,
                                               "{0}: {1,5:0.00} (°)\r\n{2}: {3,5:0.00} (°)\r\n{4}: {5}",
                                               App.LoadString("TrueHeading"),
                                               e.Reading.HeadingTrueNorth,
                                               App.LoadString("MagneticHeading"),
                                               e.Reading.HeadingMagneticNorth,
                                               App.LoadString("Accuracy"),
                                               e.Reading.HeadingAccuracy);

                txtStatus.Text = reading;
                rotateTransform.Angle = (-1) * Convert.ToDouble(e.Reading.HeadingTrueNorth, CultureInfo.CurrentCulture);
                rotateTransform.CenterX = imgPage.ActualWidth / 2;
                rotateTransform.CenterY = imgPage.ActualHeight / 2;

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
