using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace AutoCover
{
    public static class VsOutputService
    {
        public static void Output(string msg)
        {
            // Output the message
            GetPane().OutputString(string.Format("{0}: {1}\n", DateTime.Now, msg));
        }

        public static void OutputEmptyLine()
        {
            // Output the message
            GetPane().OutputString("\n");
        }

        private static IVsOutputWindowPane GetPane()
        {
            // Get the output window
            var outputWindow = Package.GetGlobalService(typeof(SVsOutputWindow)) as IVsOutputWindow;
            // Ensure that the desired pane is visible
            var paneGuid = Microsoft.VisualStudio.VSConstants.OutputWindowPaneGuid.GeneralPane_guid;
            IVsOutputWindowPane pane;
            outputWindow.CreatePane(paneGuid, "AutoCover", 1, 0);
            outputWindow.GetPane(paneGuid, out pane);
            return pane;
        }
    }
}
