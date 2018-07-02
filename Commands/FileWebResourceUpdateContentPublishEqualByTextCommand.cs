﻿using Microsoft.VisualStudio.Shell;
using Nav.Common.VSPackages.CrmDeveloperHelper.Helpers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Model;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using System.Collections.Generic;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Commands
{
    internal sealed class FileWebResourceUpdateContentPublishEqualByTextCommand : AbstractCommand
    {
        private FileWebResourceUpdateContentPublishEqualByTextCommand(Package package)
            : base(package, PackageGuids.guidCommandSet, PackageIds.FileWebResourceUpdateContentPublishEqualByTextCommandId, ActionExecute, ActionBeforeQueryStatus) { }

        public static FileWebResourceUpdateContentPublishEqualByTextCommand Instance { get; private set; }

        public static void Initialize(Package package)
        {
            Instance = new FileWebResourceUpdateContentPublishEqualByTextCommand(package);
        }

        private static void ActionExecute(DTEHelper helper)
        {
            List<SelectedFile> selectedFiles = helper.GetSelectedFilesInSolutionExplorer(FileOperations.SupportsWebResourceTextType, false);

            helper.HandleUpdateContentWebResourcesAndPublishEqualByTextCommand(selectedFiles);
        }

        private static void ActionBeforeQueryStatus(IServiceProviderOwner command, OleMenuCommand menuCommand)
        {
            CommonHandlers.ActionBeforeQueryStatusConnectionIsNotReadOnly(command, menuCommand);

            CommonHandlers.ActionBeforeQueryStatusSolutionExplorerWebResourceTextAny(command, menuCommand);

            CommonHandlers.CorrectCommandNameForConnectionName(command, menuCommand, "Publish Files equal by Text");
        }
    }
}
