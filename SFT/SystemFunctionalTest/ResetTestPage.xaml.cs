//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.System.Display;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SystemFunctionalTest
{
    /// <summary>
    /// Test result clear confirmation page.
    /// </summary>
    public sealed partial class ResetTestPage : Page
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ResetTestPage"/> class.
        /// </summary>
        public ResetTestPage()
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

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        #endregion // Override methods


        /// <summary>
        /// <see cref="Button.Click"/> event handler.
        /// Clear all test result.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ResetTest_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (button == null) return;

            if (button.Name == "btnYes")
            {
                App.TestResultValue = App.TestStateValue = 0;
            }

            Frame.Navigate(typeof(MainPage));
        }
    }
}
