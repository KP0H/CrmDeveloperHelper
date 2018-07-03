﻿using Nav.Common.VSPackages.CrmDeveloperHelper.Helpers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Model;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using Nav.Common.VSPackages.CrmDeveloperHelper.Repository;
using Nav.Common.VSPackages.CrmDeveloperHelper.Views;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Controllers
{
    public class PluginTypeDescriptionController
    {
        private IWriteToOutput _iWriteToOutput = null;

        /// <summary>
        /// Конструктор контроллера
        /// </summary>
        /// <param name="iWriteToOutput"></param>
        public PluginTypeDescriptionController(IWriteToOutput iWriteToOutput)
        {
            this._iWriteToOutput = iWriteToOutput;
        }

        #region Описание шагов плагинов для типа.

        public async void ExecuteCreatingPluginTypeDescription(ConnectionData connectionData, CommonConfiguration commonConfig, string selection)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Creating PluginType Description at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
            {
                await CreatingPluginTypeDescription(connectionData, commonConfig, selection);
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Creating PluginType Description at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        private async Task CreatingPluginTypeDescription(ConnectionData connectionData, CommonConfiguration commonConfig, string selection)
        {
            if (connectionData == null)
            {
                this._iWriteToOutput.WriteToOutput("No current CRM Connection.");
                return;
            }

            this._iWriteToOutput.WriteToOutput("Connect to CRM.");

            this._iWriteToOutput.WriteToOutput(connectionData.GetConnectionDescription());

            // Подключаемся к CRM.
            var service = await QuickConnection.ConnectAsync(connectionData);

            this._iWriteToOutput.WriteToOutput("Current Service Endpoint: {0}", service.CurrentServiceEndpoint);

            WindowHelper.OpenPluginTypeWindow(
                this._iWriteToOutput
                , service
                , commonConfig
                , selection
                );
        }

        #endregion Описание шагов плагинов для типа.

        #region Описание шагов плагинов сборки.

        public async void ExecuteExportingPluginAssembly(ConnectionData connectionData, CommonConfiguration commonConfig, string selection)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Exporting Plugin Assembly at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
            {
                await ExportingPluginAssembly(connectionData, commonConfig, selection);
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Start Exporting Plugin Assembly at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        private async Task ExportingPluginAssembly(ConnectionData connectionData, CommonConfiguration commonConfig, string selection)
        {
            if (connectionData == null)
            {
                this._iWriteToOutput.WriteToOutput("No current CRM Connection.");
                return;
            }

            this._iWriteToOutput.WriteToOutput("Connect to CRM.");

            this._iWriteToOutput.WriteToOutput(connectionData.GetConnectionDescription());

            // Подключаемся к CRM.
            var service = await QuickConnection.ConnectAsync(connectionData);

            this._iWriteToOutput.WriteToOutput("Current Service Endpoint: {0}", service.CurrentServiceEndpoint);

            WindowHelper.OpenPluginAssemblyWindow(
                this._iWriteToOutput
                , service
                , commonConfig
                , selection
                );
        }

        #endregion Описание шагов плагинов сборки.

        #region Сравнение сборки плагинов и локальной сборки.

        public async void ExecuteComparingAssemblyAndCrmSolution(ConnectionData connectionData, CommonConfiguration commonConfig, string projectName, string defaultFolder)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Comparing Crm PluginAssembly and Local Assembly into Solution at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
            {
                await ComparingAssemblyAndCrmSolution(connectionData, commonConfig, projectName, defaultFolder);
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Comparing Crm PluginAssembly and Local Assembly into Solution at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        private async Task ComparingAssemblyAndCrmSolution(ConnectionData connectionData, CommonConfiguration commonConfig, string projectName, string defaultFolder)
        {
            if (connectionData == null)
            {
                this._iWriteToOutput.WriteToOutput("No current CRM Connection.");
                return;
            }

            if (string.IsNullOrEmpty(projectName))
            {
                this._iWriteToOutput.WriteToOutput("Project Name is empty.");
                return;
            }

            this._iWriteToOutput.WriteToOutput("Connect to CRM.");

            this._iWriteToOutput.WriteToOutput(connectionData.GetConnectionDescription());

            // Подключаемся к CRM.
            var service = await QuickConnection.ConnectAsync(connectionData);

            this._iWriteToOutput.WriteToOutput("Current Service Endpoint: {0}", service.CurrentServiceEndpoint);

            var repositoryAssembly = new PluginAssemblyRepository(service);

            var assembly = await repositoryAssembly.FindAssemblyAsync(projectName);

            if (assembly == null)
            {
                this._iWriteToOutput.WriteToOutput("PluginAssembly not founded by name {0}.", projectName);

                WindowHelper.OpenPluginAssemblyWindow(
                    this._iWriteToOutput
                    , service
                    , commonConfig
                    , projectName
                    );

                return;
            }

            string filePath = await CreateFileWithAssemblyComparing(commonConfig.FolderForExport, connectionData, service, assembly.Id, assembly.Name, defaultFolder);

            this._iWriteToOutput.PerformAction(filePath, commonConfig);
        }

        public async Task<string> CreateFileWithAssemblyComparing(string folder, ConnectionData connection, IOrganizationServiceExtented service, Guid idPluginAssembly, string assemblyName, string defaultFolder)
        {
            var repositoryType = new PluginTypeRepository(service);

            var task = repositoryType.GetPluginTypesAsync(idPluginAssembly);

            string assemblyPath = connection.GetLastAssemblyPath(assemblyName);
            bool showDialog = false;

            var t = new Thread((ThreadStart)(() =>
            {
                try
                {
                    System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();

                    openFileDialog1.Filter = "Plugin Assebmly (.dll)|*.dll";
                    openFileDialog1.FilterIndex = 1;
                    openFileDialog1.RestoreDirectory = true;

                    if (!string.IsNullOrEmpty(assemblyPath))
                    {
                        openFileDialog1.InitialDirectory = Path.GetDirectoryName(assemblyPath);
                        openFileDialog1.FileName = Path.GetFileName(assemblyPath);
                    }
                    else if (!string.IsNullOrEmpty(defaultFolder))
                    {
                        openFileDialog1.InitialDirectory = defaultFolder;
                    }

                    if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        showDialog = true;
                        assemblyPath = openFileDialog1.FileName;
                    }
                }
                catch (Exception ex)
                {
                    DTEHelper.WriteExceptionToOutput(ex);
                }
            }));

            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            t.Join();

            if (!showDialog)
            {
                return string.Empty;
            }

            string filePath = string.Empty;

            connection.AddAssemblyMapping(assemblyName, assemblyPath);
            connection.ConnectionConfiguration.Save();

            HashSet<string> assemblyPlugins = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
            HashSet<string> assemblyWorkflow = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

            AppDomain childDomain = CreateChildDomain();

            try
            {
                childDomain.SetData("Assembly", assemblyPath);
                childDomain.DoCallBack(new CrossAppDomainDelegate(CheckAssembly));

                var check = childDomain.GetData("Result");

                if (check is Tuple<HashSet<string>, HashSet<string>> paths)
                {
                    assemblyPlugins = paths.Item1;
                    assemblyWorkflow = paths.Item2;
                }
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                AppDomain.Unload(childDomain);
            }

            var pluginTypes = await task;

            var crmPlugins = new HashSet<string>(pluginTypes.Where(e => !e.IsWorkflowActivity.GetValueOrDefault()).Select(e => e.TypeName), StringComparer.InvariantCultureIgnoreCase);
            var crmWorkflows = new HashSet<string>(pluginTypes.Where(e => e.IsWorkflowActivity.GetValueOrDefault()).Select(e => e.TypeName), StringComparer.InvariantCultureIgnoreCase);

            var content = new StringBuilder();

            content.AppendLine(connection.GetConnectionInfo()).AppendLine();
            content.AppendFormat("Description for PluginAssembly '{0}' at {1}", assemblyName, DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture)).AppendLine();

            content.AppendFormat("Local Assembly '{0}'", assemblyPath).AppendLine();

            var pluginsInCrm = crmPlugins.Except(assemblyPlugins, StringComparer.InvariantCultureIgnoreCase);
            var workflowInCrm = crmWorkflows.Except(assemblyWorkflow, StringComparer.InvariantCultureIgnoreCase);

            var pluginsInLocalAssembly = assemblyPlugins.Except(crmPlugins, StringComparer.InvariantCultureIgnoreCase);
            var workflowInLocalAssembly = assemblyWorkflow.Except(crmWorkflows, StringComparer.InvariantCultureIgnoreCase);

            const string tabspacer = "      ";

            FillInformation(content, "Plugins ONLY in Crm Assembly {0}", pluginsInCrm, tabspacer);
            FillInformation(content, "Workflows ONLY in Crm Assembly {0}", workflowInCrm, tabspacer);
            FillInformation(content, "Plugins ONLY in Local Assembly {0}", pluginsInLocalAssembly, tabspacer);
            FillInformation(content, "Workflows ONLY in Local Assembly {0}", workflowInLocalAssembly, tabspacer);

            if (!pluginsInCrm.Any()
                && !workflowInCrm.Any()
                && !pluginsInLocalAssembly.Any()
                && !workflowInLocalAssembly.Any()
                )
            {
                content.AppendLine().AppendLine().AppendLine();

                content.AppendLine("No difference in Assemblies.");
            }

            FillInformation(content, "Plugins in Crm Assembly {0}", crmPlugins, tabspacer);
            FillInformation(content, "Workflows in Crm Assembly {0}", crmWorkflows, tabspacer);
            FillInformation(content, "Plugins in Local Assembly {0}", assemblyPlugins, tabspacer);
            FillInformation(content, "Workflows in Local Assembly {0}", assemblyWorkflow, tabspacer);

            string fileName = EntityFileNameFormatter.GetPluginAssemblyFileName(connection.Name, assemblyName, "Comparing", "txt");
            filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

            File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);

            this._iWriteToOutput.WriteToOutput("Assembly {0} Comparing exported to {1}", assemblyName, filePath);

            return filePath;
        }

        private static AppDomain CreateChildDomain()
        {
            string path = typeof(PluginsAndWorkflowLoader).Assembly.Location;

            string directory = Path.GetDirectoryName(path);

            var setup = new AppDomainSetup
            {
                ApplicationBase = directory,
                CachePath = directory,
                LoaderOptimization = LoaderOptimization.MultiDomain,
            };

            AppDomain childDomain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), AppDomain.CurrentDomain.Evidence.Clone(), setup);

            return childDomain;
        }

        public static void CheckAssembly()
        {
            try
            {
                var path = (string)AppDomain.CurrentDomain.GetData("Assembly");

                var temp = new PluginsAndWorkflowLoader();
                var result = temp.LoadAssembly(path);

                AppDomain.CurrentDomain.SetData("Result", result);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.ToString());
            }
        }

        private static void FillInformation(StringBuilder content, string format, IEnumerable<string> collection, string tabspacer)
        {
            if (collection.Any())
            {
                content.AppendLine().AppendLine().AppendLine();
                content.AppendLine(new string('-', 150));
                content.AppendLine().AppendLine().AppendLine();

                content.AppendFormat(format, collection.Count()).AppendLine();
                foreach (var item in collection.OrderBy(s => s))
                {
                    content.AppendLine(tabspacer + item);
                }
            }
        }

        #endregion Сравнение сборки плагинов и локальной сборки.
    }
}