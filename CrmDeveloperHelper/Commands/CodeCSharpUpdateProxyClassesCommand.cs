using Microsoft.VisualStudio.Shell;
using Nav.Common.VSPackages.CrmDeveloperHelper.Helpers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Commands
{
    internal sealed class CodeCSharpUpdateProxyClassesCommand : AbstractCommand
    {
        private CodeCSharpUpdateProxyClassesCommand(Package package)
            : base(package, PackageGuids.guidCommandSet, PackageIds.CodeCSharpUpdateProxyClassesCommandId, ActionExecute, ActionBeforeQueryStatus) { }

        public static CodeCSharpUpdateProxyClassesCommand Instance { get; private set; }

        public static void Initialize(Package package)
        {
            Instance = new CodeCSharpUpdateProxyClassesCommand(package);
        }

        private static void ActionExecute(DTEHelper helper)
        {
            helper.HandleUpdateProxyClasses();
        }

        private static void ActionBeforeQueryStatus(IServiceProviderOwner command, OleMenuCommand menuCommand)
        {
            CommonHandlers.ActionBeforeQueryStatusActiveDocumentCSharp(command, menuCommand);

            CommonHandlers.CorrectCommandNameForConnectionName(command, menuCommand, Properties.CommandNames.CodeCSharpUpdateProxyClassesCommand);
        }
    }
}