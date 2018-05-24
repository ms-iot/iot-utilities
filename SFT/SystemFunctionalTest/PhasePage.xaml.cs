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
using System.Threading.Tasks;
using System.Diagnostics;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace SystemFunctionalTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PhasePage : Page
    {
        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PhasePage"/> class.
        /// </summary>
        public PhasePage()
        {
            this.InitializeComponent();

            InitializePhase();
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
        /// Initialize phase names and check whether PhasePage.xaml will be launched or not.
        /// </summary>
        private async void InitializePhase()
        {
            await App.LoadPhase();

            if (App.PhaseCount > 1)  // Launch PhasePage.xaml for users to select phase if more than 1 phase defined in SFTConfig.xml. 
            {
                // Set Phase names
                for (UInt16 i = 0; i < App.PhaseCount; i++)
                {
                    string name = "btnPhase" + i.ToString(CultureInfo.CurrentCulture);

                    object obj = FindName(name);
                    Button btn = obj as Button;
                    if (btn != null)
                    {
                        btn.Content = App.PhaseName[i];
                        btn.Visibility = Visibility.Visible;
                    }
                }
            }
            else // Won't launch PhasePage.xaml if only one phase defined in SFTConfig.xml or SFTConfig.xml doesn't exist.
            {
                await GotoPhase(0);
            }
        }

        private async Task GotoPhase(ushort index)
        {
            //read from XMl file

            await App.LoadMenuSettings(index);
            var ignored = App.AquireEarlyStartResource();
            if (App.IsModeHeadLess)
            {
                App.IsAuto = true;
                Frame.Navigate(App.GetTestPageType(App.TestName[0]),
                               App.GetTestPageParameter(App.TestName[0]));
            }
            else
            {
                Frame.Navigate(typeof(MainPage));
            }
        }

        /// <summary>
        /// <see cref="Button.Click"/> event handler.
        /// Select phase.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>

        private async void PhasePage_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null) return;

            try
            {
                UInt16 nIndexPhase = Convert.ToUInt16(btn.Name.Substring(8), CultureInfo.CurrentCulture);
                await GotoPhase(nIndexPhase);
                Debug.WriteLine("Select Phase " + nIndexPhase + " : " + btn.Name);
            }
            catch (FormatException)
            { }
            catch (OverflowException)
            { }
        }
    }
}
