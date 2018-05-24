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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Globalization;
using Windows.System.Display;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SystemFunctionalTest
{
    /// <summary>
    /// Test menu item selection page.
    /// </summary>
    public sealed partial class MenuPage : Page
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MenuPage"/> class.
        /// </summary>
        /// 
        public MenuPage()
        {
            this.InitializeComponent();

            InitializeTestItemName();
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

            SetTestResultColor();

        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }
        #endregion

        /// <summary>
        /// Initialize each test item name in test menu page.
        /// </summary>
        private void InitializeTestItemName()
        {
            // Set test item name
            for (UInt16 j = 0; j < App.TestCount; j++)
            {
                string name = "btnTest" + j.ToString(CultureInfo.CurrentCulture);
                string value = "";

                object obj = FindName(name);
                Button button = obj as Button;
                if (button != null)
                {
                    // Following executed if Button element was found.
                    if (App.MenuItemName.TryGetValue(App.TestName[j], out value))
                        button.Content = value;
                    else
                        button.Content = "";

                    button.Visibility = Visibility.Visible;
                }
            }
        }

        /// <summary>
        /// Set test result color for each test item.
        ///     Green color: Passed test item.
        ///     Red color: Failed test item.
        ///     No color: Not tested item.
        /// </summary>
        private void SetTestResultColor()
        {
            // Reset to Manual mode
            App.IsAuto = false;

            // Set buttons' Background & Forground color for Pass or Fail 
            SolidColorBrush colorPass = new SolidColorBrush(Windows.UI.Colors.Green);
            SolidColorBrush colorFail = new SolidColorBrush(Windows.UI.Colors.Red);

            // Set pass/fail color to each test item
            for (int i = 0; i < App.TestCount; i++)
            {
                string name = "btnTest" + i.ToString(CultureInfo.CurrentCulture);
                uint nIndex = (uint)1 << i;

                object obj = FindName(name);
                Button button = obj as Button;
                if (button != null)
                {
                    if ((App.TestStateValue & nIndex) != 0)  // this test item had been tested
                    {
                        if ((App.TestResultValue & nIndex) != 0)
                            button.Background = colorPass;
                        else
                            button.Background = colorFail;
                    }
                }
            }
        }

        /// <summary>
        /// <see cref="Button.Click"/> event handler.
        /// Select test item.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void MenuPage_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            if (btn.Name == "btnMain")
            {
                Frame.Navigate(typeof(MainPage));
                return;
            }

            try
            {
                uint nIndex = Convert.ToUInt16(btn.Name.Substring(7), CultureInfo.CurrentCulture);
                Frame.Navigate(App.GetTestPageType(App.TestName[(int)nIndex]),
                               App.GetTestPageParameter(App.TestName[(int)nIndex]));
            }
            catch (FormatException)
            { }
            catch (OverflowException)
            { }
        }
    }
}
