using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.TestTools.Common;
using Microsoft.VisualStudio.TestTools.Vsip;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using EnvDTE;
using EnvDTE80;
using AutoCover;
using System.Collections.Generic;
using GalaSoft.MvvmLight.Messaging;

namespace SimoneGrignola.AutoCover
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(MyToolWindow))]
    [Guid(GuidList.guidAutoCoverPkgString)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    public sealed class AutoCoverPackage : Package
    {
        private DTE2 _DTE;
        private DocumentEvents _documentEvents;
        private SolutionEvents _solutionEvents;
        private ITestManagement _testManagement;
        private WindowEvents _windowEvents;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public AutoCoverPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = this.FindToolWindow(typeof(MyToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }


        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                // Create the command for the tool window
                var toolwndCommandID = new CommandID(GuidList.guidAutoCoverCmdSet, (int)PkgCmdIDList.cmdidAutoCover);
                var menuToolWin = new MenuCommand(ShowToolWindow, toolwndCommandID);
                mcs.AddCommand(menuToolWin);
            }
            _DTE = GetGlobalService(typeof(SDTE)) as DTE2;
            _testManagement = GetService(typeof(STestManagement)) as ITestManagement;
            if (_DTE != null && _testManagement != null)
            {
                _DTE.SuppressUI = true;
                _documentEvents = _DTE.Events.DocumentEvents;
                _documentEvents.DocumentSaved += DocumentEvents_DocumentSaved;
                _windowEvents = _DTE.Events.WindowEvents;
                _windowEvents.WindowActivated += WindowEvents_WindowActivated;
                _solutionEvents = _DTE.Events.SolutionEvents;
                _solutionEvents.Opened += _solutionEvents_Opened;
                _solutionEvents.BeforeClosing += _solutionEvents_BeforeClosing;
                _solutionEvents.AfterClosing += _solutionEvents_AfterClosing;
            }
        }

        void _solutionEvents_BeforeClosing()
        {
            SettingsService.UnloadSettings(_DTE.Solution);
        }

        void _solutionEvents_AfterClosing()
        {
            AutoCoverEngine.Reset();
            Messenger.Default.Send(new SolutionStatusChangedMessage(SolutionStatus.Closed));
        }

        void _solutionEvents_Opened()
        {
            SettingsService.LoadSettingsForSolution(_DTE.Solution);
            AutoCoverEngine.Reset();
            Messenger.Default.Send(new SolutionStatusChangedMessage(SolutionStatus.Opened));
        }

        void WindowEvents_WindowActivated(Window GotFocus, Window LostFocus)
        {
            Messenger.Default.Send(new RefreshTaggerMessage());
        }

        void DocumentEvents_DocumentSaved(Document document)
        {
            if (_DTE.Solution == null)
                return;
            if (_DTE.Solution.SolutionBuild.BuildState == vsBuildState.vsBuildStateInProgress)
                return;
            if (_DTE.Debugger.DebuggedProcesses.Count > 0)
                return;

            var tmi = _testManagement.TmiInstance;
            if (tmi != null)
            {
                var config = tmi.GetTestRunConfiguration(tmi.ActiveTestRunConfigurationId);
                if (config != null)
                {
                    AutoCoverEngine.CheckSolution(_DTE.Solution, document, config.Storage);
                }
            }
        }
    }
}
