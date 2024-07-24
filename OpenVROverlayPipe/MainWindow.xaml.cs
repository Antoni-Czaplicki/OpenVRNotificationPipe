﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using CommandLine;
using EasyFramework;
using OpenVROverlayPipe.Properties;
using Application = System.Windows.Application;
using Brushes = System.Windows.Media.Brushes;
using WindowState = System.Windows.WindowState;

namespace OpenVROverlayPipe
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [SupportedOSPlatform("windows7.0")]
    public partial class MainWindow : Window
    {
        private readonly MainController _controller;
        private readonly Settings _settings = Settings.Default;
        private readonly GraphicsSingleton _graphics = GraphicsSingleton.Instance;
        private CommandLineOptions _parameters = new();


        public MainWindow()
        {
            // Parse command line arguments
            var args = Environment.GetCommandLineArgs();
            Parser.Default.ParseArguments<CommandLineOptions>(args)
                .WithParsed<CommandLineOptions>(opts => _parameters = opts)
                .WithNotParsed<CommandLineOptions>(HandleParseError);
            
            InitializeComponent();
            
            if (!_settings.LaunchMinimized && !_parameters.IgnoreUi && !_parameters.Minimized) {
                // Position in last known location, unless negative, then center on screen.
                var wa = Screen.PrimaryScreen?.WorkingArea;
                var b = Screen.PrimaryScreen?.Bounds;
                if (
                    wa != null 
                    && b != null 
                    && _settings.WindowTop >= wa?.Y
                    && _settings.WindowLeft >= wa?.X
                    && _settings.WindowTop < (b?.Height - Height)
                    && _settings.WindowLeft < (b?.Width - Width)
                ) {
                    Top = _settings.WindowTop; 
                    Left = _settings.WindowLeft; 
                }
                else WindowStartupLocation = WindowStartupLocation.CenterScreen; 
            }

            // Prevent multiple instances
            WindowUtils.CheckIfAlreadyRunning(Properties.Resources.AppName);

            // Tray icon
            WindowUtils.CreateTrayIcon(
                this, 
                Properties.Resources.Icon.Clone() as Icon,
                Properties.Resources.AppName,
                Properties.Resources.Version,
                _parameters.ClickUrl
            );
            Title = Properties.Resources.AppName;

            LoadSettings();
#if DEBUG
            LabelVersion.Content = $"{Properties.Resources.Version}d";
#else
            LabelVersion.Content = Properties.Resources.Version;
#endif
            // Controller
            _controller = new MainController(
                !_parameters.DisableWebSocket,
                _parameters.PipeName,
                _parameters.DontRegisterManifest,
                (status, value) =>
                {
                    if (_parameters.IgnoreUi) return;
                    Dispatcher.Invoke(() => { UpdateWSServerStatus(status, value); });
                },
                (status, value) =>
                {
                    if (_parameters.IgnoreUi) return;
                    Dispatcher.Invoke(() => { UpdatePipeClientStatus(status, value); });
                },
                (status) =>
                {
                    if (_parameters.IgnoreUi) return;
                    Dispatcher.Invoke(() => { UpdateOpenVrStatus(status); });
                }
            );

            GraphicsCompanion.StartOpenTk(this);
            _controller.SetPort(_settings.Port);

            if (_settings.LaunchMinimized || _parameters.Minimized) 
            {
                Loaded += (sender, args) =>
                {
                    var wa = Screen.PrimaryScreen?.WorkingArea;
                    var b = Screen.PrimaryScreen?.Bounds;
                    if ( // If we have a valid stored location, use it
                        wa != null
                        && b != null
                        && _settings.WindowTop >= wa?.Y
                        && _settings.WindowLeft >= wa?.X
                        && _settings.WindowTop < (b?.Height - Height)
                        && _settings.WindowLeft < (b?.Width - Width) 
                    ) {
                        Top = _settings.WindowTop;
                        Left = _settings.WindowLeft;
                    } else if(wa != null) { // Otherwise center on screen
                        Top = (wa?.Y ?? 0) + ((wa?.Height ?? 0.0) / 2 - Height / 2);
                        Left = (wa?.X ?? 0) + ((wa?.Width ?? 0.0) / 2 - Width / 2);
                    }
                    
                    WindowState = WindowState.Minimized;
                    ShowInTaskbar = !_settings.Tray;
                };
            }

            
        }

        private void LoadSettings()
        {
            CheckBoxMinimizeOnLaunch.IsChecked = _settings.LaunchMinimized;
            CheckBoxMinimizeToTray.IsChecked = _settings.Tray;
            CheckBoxExitWithSteamVr.IsChecked = _settings.ExitWithSteam;
            TextBoxPort.Text = _settings.Port.ToString();
        }
        
        private void HandleParseError(IEnumerable<Error> errs)
        {
            foreach (var error in errs)
            {
                Console.WriteLine(error.ToString());
            }
        }
        
        private void UpdateWSServerStatus(SuperServer.ServerStatus status, int value)
        {
            switch (status)
            {
                case SuperServer.ServerStatus.Connected:
                    LabelServerStatus.Background = Brushes.OliveDrab;
                    LabelServerStatus.Content = "Online";
                    break;
                case SuperServer.ServerStatus.Disconnected:
                    LabelServerStatus.Background = Brushes.Tomato;
                    LabelServerStatus.Content = "Offline";
                    break;
                case SuperServer.ServerStatus.Error:
                    LabelServerStatus.Background = Brushes.Gray;
                    LabelServerStatus.Content = "Error";
                    break;
                case SuperServer.ServerStatus.ReceivedCount:
                    LabelHandledRequests.Content = value;
                    break;
                case SuperServer.ServerStatus.SessionCount:
                    LabelConnectedClients.Content = value;
                    break;
            }
        }
        
        private void UpdatePipeClientStatus(NamedPipeClient.NamedPipeClientStatus status, int value)
        {
            switch (status)
            {
                case NamedPipeClient.NamedPipeClientStatus.Connected:
                    LabelPipeClientStatus.Background = Brushes.OliveDrab;
                    LabelPipeClientStatus.Content = "Connected";
                    break;
                case NamedPipeClient.NamedPipeClientStatus.Disconnected:
                    LabelPipeClientStatus.Background = Brushes.Tomato;
                    LabelPipeClientStatus.Content = "Disconnected";
                    break;
                case NamedPipeClient.NamedPipeClientStatus.Error:
                    LabelPipeClientStatus.Background = Brushes.Gray;
                    LabelPipeClientStatus.Content = "Error";
                    break;
                case NamedPipeClient.NamedPipeClientStatus.ReceivedCount:
                    LabelHandledRequests.Content = value;
                    break;
            }
        }
        
        private void UpdateOpenVrStatus(bool status)
        {
            if (status)
            {
                LabelOpenVrStatus.Background = Brushes.OliveDrab;
                LabelOpenVrStatus.Content = "Connected";
            }
            else
            {
                LabelOpenVrStatus.Background = Brushes.Tomato;
                LabelOpenVrStatus.Content = "Disconnected";
                if (_settings.ExitWithSteam && _parameters.DontExitWithSteamVR != true)
                {
                    _controller?.Shutdown();
                    WindowUtils.DestroyTrayIcon();
                    Application.Current.Shutdown();
                }
            }
        }

        #region interface
        private void Button_Edit_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new InputDialog(_settings.Port, "Port:")
            {
                Owner = this
            };
            dlg.ShowDialog();
            var result = dlg.DialogResult;
            if (result == true)
            {
                _settings.Port = dlg.value;
                _settings.Save();
                TextBoxPort.Text = _settings.Port.ToString();
                _controller.SetPort(_settings.Port);
            }
        }

        private void Button_Editor_Click(object sender, RoutedEventArgs e) {
            MiscUtils.OpenUrl("editor.html");
        }
        
        private void ClickedUrl(object sender, RoutedEventArgs e)
        {
            var link = (Hyperlink)sender;
            MiscUtils.OpenUrl(link.NavigateUri.ToString());
        }
        private bool CheckboxValue(RoutedEventArgs e)
        {
            var name = e.RoutedEvent.Name;
            return name == "Checked";
        }
        #endregion

        private void CheckBox_MinimizeOnLaunch_Checked(object sender, RoutedEventArgs e)
        {
            _settings.LaunchMinimized = CheckboxValue(e);
            _settings.Save();
        }

        private void CheckBox_MinimizeToTray_Checked(object sender, RoutedEventArgs e)
        {
            _settings.Tray = CheckboxValue(e);
            _settings.Save();
        }

        private void CheckBox_ExitWithSteamVR_Checked(object sender, RoutedEventArgs e)
        {
            _settings.ExitWithSteam = CheckboxValue(e);
            _settings.Save();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            WindowUtils.OnStateChange(this, !_settings.Tray);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            var wa = Screen.PrimaryScreen?.WorkingArea;
            var b = Screen.PrimaryScreen?.Bounds;
            if (Top >= wa?.Y && Top < (b?.Height - Height)) _settings.WindowTop = Top;
            if(Left >= wa?.X && Left < (b?.Width - Width)) _settings.WindowLeft = Left;
            _settings.Save();
            WindowUtils.DestroyTrayIcon();
        }
        
        // Open GL/TK stuff
        public static void FitToScreen(FrameworkElement screen, int width, int height)
        {
            GraphicsCompanion.SetViewportDimensions((int) screen.Width, (int) screen.Height, width, height);
        }

        private void OpenTKControl_OnRender(TimeSpan delta)
        {
            _graphics.OnRender(delta);
        }
        private void OpenTKControl_OnReady() {
            _graphics.OnReady();
        }
    }
}
