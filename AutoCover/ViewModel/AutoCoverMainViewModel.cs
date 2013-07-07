using System;
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
        private bool _isEngineRunning;

        public AutoCoverMainViewModel()
        {
            Messenger.Default.Register<TestsResultsMessage>(this, m => Tests = m.Tests);
            Messenger.Default.Register<AutoCoverEngineStatusMessage>(this, m => IsEngineRunning = m.Status != AutoCoverEngineStatus.Idle);
        }

        public List<UnitTest> Tests
        {
            get { return _tests; }
            set
            {
                _tests = value;
                RaisePropertyChanged("Tests");
            }
        }

        public bool IsEngineRunning
        {
            get { return _isEngineRunning; }
            private set
            {
                _isEngineRunning = value;
                RaisePropertyChanged("IsEngineRunning");
                RaisePropertyChanged("IsEngineRunningVisibility");
            }
        }

        public Visibility IsEngineRunningVisibility
        {
            get { return _isEngineRunning ? Visibility.Visible : Visibility.Hidden; }
        }
    }
}
