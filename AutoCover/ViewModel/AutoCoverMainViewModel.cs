using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Messaging;

namespace AutoCover
{
    public class AutoCoverMainViewModel : ViewModelBase
    {
        private List<UnitTest> _tests;

        public AutoCoverMainViewModel()
        {
            Messenger.Default.Register<TestsResultsMessage>(this, m => Tests = m.Tests);
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
    }
}
