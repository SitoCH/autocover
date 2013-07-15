﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;
using System.Windows;

namespace AutoCover
{
    public class AutoCoverMainViewModel : ViewModelBase
    {
        private List<UnitTest> _tests;
        private AutoCoverEngineStatus _engineStatus;
        private string _engineMessage;

        public AutoCoverMainViewModel()
        {
            Messenger.Default.Register<TestsResultsMessage>(this, m => Tests = m.Tests);
            Messenger.Default.Register<AutoCoverEngineStatusMessage>(this, m =>
                {
                    _engineStatus = m.Status;
                    _engineMessage = m.Message;
                    RaisePropertyChanged("IsEngineRunning");
                    RaisePropertyChanged("IsEngineRunningVisibility");
                    RaisePropertyChanged("EngineStatusLabel");
                });
            Messenger.Default.Register<SolutionStatusChangedMessage>(this, m =>
                {
                    Tests = new List<UnitTest>();
                    RaisePropertyChanged("DisableRowHighlighting");
                    RaisePropertyChanged("IsAutoCoverEnabled");
                });
        }

        public bool IsAutoCoverEnabled
        {
            get { return SettingsService.Settings.EnableAutoCover; }
            set
            {
                SettingsService.Settings.EnableAutoCover = value;
                AutoCoverEngine.Reset();
                RaisePropertyChanged("IsAutoCoverEnabled");
            }
        }

        public bool DisableRowHighlighting
        {
            get { return SettingsService.Settings.DisableRowHighlighting; }
            set
            {
                SettingsService.Settings.DisableRowHighlighting = value;
                Messenger.Default.Send(new RefreshTaggerMessage());
                RaisePropertyChanged("DisableRowHighlighting");
            }
        }



        public List<UnitTest> Tests
        {
            get { return _tests; }
            set
            {
                _tests = value != null ? value.OrderByDescending(x => x.Result).ToList() : value;
                RaisePropertyChanged("Tests");
            }
        }

        public string EngineStatusLabel
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_engineMessage))
                    return _engineStatus.ToString();
                return string.Format("{0}: {1}", _engineStatus, _engineMessage);
            }
        }

        public bool IsEngineRunning
        {
            get { return _engineStatus != AutoCoverEngineStatus.Idle; }
        }

        public Visibility IsEngineRunningVisibility
        {
            get { return IsEngineRunning ? Visibility.Visible : Visibility.Hidden; }
        }
    }
}
