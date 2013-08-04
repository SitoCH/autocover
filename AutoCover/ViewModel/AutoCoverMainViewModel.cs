// Copyright (c) 2013
// Simone Grignola [http://www.grignola.ch]
//
// This file is part of AutoCover.
//
// AutoCover is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

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
        private List<ACUnitTest> _tests;
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
                    Tests = new List<ACUnitTest>();
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
                AutoCoverService.Reset();
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

        public List<ACUnitTest> Tests
        {
            get { return _tests; }
            set
            {
                _tests = value != null ? value.OrderByDescending(x => x.Result).ToList() : null;
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
