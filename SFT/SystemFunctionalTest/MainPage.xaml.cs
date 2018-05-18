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
using Windows.UI.Xaml.Navigation;
using System.Globalization;
using Windows.System.Display;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SystemFunctionalTest
{
    /// <summary>
    /// Main menu page.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MainPage"/> class.
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();

            InitializeMenuItemName();
            
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
            // Initialize to Manual mode
            App.IsAuto = false;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }
        #endregion // Override methods

        /// <summary>
        /// Initialize each menu item name in main menu page.
        /// </summary>
        private void InitializeMenuItemName()
        {
            
            // Set menu item name
            for (UInt16 i = 0; i < App.MainMenuCount; i++)
            {
                string name = "btnMenu" + i.ToString(CultureInfo.CurrentCulture);
                string value = "";

                object obj = FindName(name);
                Button button = obj as Button;
                if (button != null)
                {
                    button.Name = "btn" + App.MainMenuName[i];

                    if (App.MenuItemName.TryGetValue(App.MainMenuName[i], out value))
                        button.Content = value;
                    else
                        button.Content = "";

                    button.Visibility = Visibility.Visible;
                }
            }
           
        }

        /// <summary>
        /// <see cref="Button.Click"/> event handler.
        /// Select main menu item.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void MainPage_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            switch (btn.Name)
            {
                case "btnAuto":
                    App.IsAuto = true;
                    Frame.Navigate(App.GetTestPageType(App.TestName[0]),
                                   App.GetTestPageParameter(App.TestName[0]));
                    break;

                case "btnManual":
                    App.IsAuto = false;
                    Frame.Navigate(typeof(MenuPage));
                    break;

                case "btnClearResult":
                    Frame.Navigate(typeof(ResetTestPage));
                    break;
            }
        }
    }
}
