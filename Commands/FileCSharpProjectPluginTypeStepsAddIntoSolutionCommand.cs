﻿using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Nav.Common.VSPackages.CrmDeveloperHelper.Helpers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using System.Linq;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Commands
{
    internal sealed class FileCSharpProjectPluginTypeStepsAddIntoSolutionCommand : AbstractCommand
    {
        private FileCSharpProjectPluginTypeStepsAddIntoSolutionCommand(Package package)
            : base(package, PackageGuids.guidCommandSet, PackageIds.FileCSharpProjectPluginTypeStepsAddIntoSolutionCommandId, ActionExecute, ActionBeforeQueryStatus) { }

        public static FileCSharpProjectPluginTypeStepsAddIntoSolutionCommand Instance { get; private set; }

        public static void Initialize(Package package)
        {
            Instance = new FileCSharpProjectPluginTypeStepsAddIntoSolutionCommand(package);
        }

        private static void ActionBeforeQueryStatus(IServiceProviderOwner command, OleMenuCommand menuCommand)
        {
            CommonHandlers.ActionBeforeQueryStatusSolutionExplorerAnyItemContainsProject(command, menuCommand, FileOperations.SupportsCSharpType);

            CommonHandlers.CorrectCommandNameForConnectionName(command, menuCommand, "Add Steps for PluginType into Crm Solution");
        }

        private static void ActionExecute(DTEHelper helper)
        {
            var list = helper.GetListSelectedItemInSolutionExplorer(FileOperations.SupportsCSharpType)
                .Select(e => GetName(e))
                .Where(s => !string.IsNullOrEmpty(s))
                ;

            if (list.Any())
            {
                helper.HandleAddingPluginAssemblyIntoSolutionByProjectCommand(null, true, list.ToArray());
            }
        }

        private static string GetName(SelectedItem item)
        {
            string selection = string.Empty;

            if (item.ProjectItem != null && item.ProjectItem.FileCount > 0)
            {
                selection = item.ProjectItem.Name.Split('.').FirstOrDefault();
            }
            else if (!string.IsNullOrEmpty(item.Name))
            {
                selection = item.Name;
            }

            return selection;
        }
    }
}