﻿using System.Windows;
using HuntAndPeck.ViewModels;
using System.Linq;
using HuntAndPeck.Services;
using HuntAndPeck.Views;
using HuntAndPeck.NativeMethods;

namespace HuntAndPeck
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private readonly SingleLaunchMutex _singleLaunchMutex = new SingleLaunchMutex();
        private readonly UiAutomationHintProviderService _hintProviderService = new UiAutomationHintProviderService();
        private readonly HintLabelService _hintLabelService = new HintLabelService();
        private KeyListenerService _keyListenerService;
        private KeysBeingHeld _keysBeingHeld;
        private ActionHandlerService _actionHandlerService;

        private void ShowOverlay(OverlayViewModel vm)
        {
            var view = new OverlayView
            {
                DataContext = vm
            };
            vm.CloseOverlay = () => view.Close();
            view.ShowDialog();
        }

        private void ShowDebugOverlay(DebugOverlayViewModel vm)
        {
            var view = new DebugOverlayView
            {
                DataContext = vm
            };
            view.ShowDialog();
        }

        private void ShowOptions(OptionsViewModel vm)
        {
            var view = new OptionsView
            {
                DataContext = vm
            };
            view.ShowDialog();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Contains("/hint"))
            {
                // support headless mode
                var session = _hintProviderService.EnumHints();
                var overlayWindow = new OverlayView()
                {
                    DataContext = new OverlayViewModel(session, _hintLabelService)
                };
                overlayWindow.Show();
            }
            else if (e.Args.Contains("/tray"))
            {
                // support headless tray mode
                var taskbarHWnd = User32.FindWindow("Shell_traywnd", "");
                var session = _hintProviderService.EnumHints(taskbarHWnd);
                var overlayWindow = new OverlayView()
                {
                    DataContext = new OverlayViewModel(session, _hintLabelService)
                };
                overlayWindow.Show();
            }
            else
            {
                // Prevent multiple startup in non-headless mode
                if (_singleLaunchMutex.AlreadyRunning)
                {
                    Current.Shutdown();
                    return;
                }

                // Create this as late as possible as it has a window
                _keyListenerService = new KeyListenerService();
                _keysBeingHeld = new KeysBeingHeld(_keyListenerService);
                _keysBeingHeld.IsActionKeyHeld[Action.MouseMoveUp].ValueChanged += HandleMouseMoveUpHeldChanged;
                _actionHandlerService = new ActionHandlerService(_keysBeingHeld);


                var shellViewModel = new ShellViewModel(
                    ShowOverlay,
                    ShowDebugOverlay,
                    ShowOptions,
                    _hintLabelService,
                    _hintProviderService,
                    _hintProviderService,
                    _keyListenerService);

                var shellView = new ShellView
                {
                    DataContext = shellViewModel
                };
                shellView.Show();
            }
            base.OnStartup(e);
        }

        private void HandleMouseMoveUpHeldChanged()
        {
            System.Console.WriteLine("Mouse move up changed!");
        }
    }
}
