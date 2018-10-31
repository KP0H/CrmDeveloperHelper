﻿using Nav.Common.VSPackages.CrmDeveloperHelper.Helpers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using Nav.Common.VSPackages.CrmDeveloperHelper.Model;
using Nav.Common.VSPackages.CrmDeveloperHelper.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Controllers
{
    /// <summary>
    /// Контроллер для экспорта риббона
    /// </summary>
    public class EntityMetadataController
    {
        private readonly IWriteToOutput _iWriteToOutput = null;

        /// <summary>
        /// Конструктор контроллера
        /// </summary>
        /// <param name="outputWindow"></param>
        public EntityMetadataController(IWriteToOutput outputWindow)
        {
            this._iWriteToOutput = outputWindow;
        }

        #region Создание файла с мета-данными сущности.

        public async Task ExecuteCreatingFileWithEntityMetadata(string selection, ConnectionData connectionData, CommonConfiguration commonConfig)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Creating File with Entity Metadata at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
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

                WindowHelper.OpenEntityMetadataWindow(this._iWriteToOutput, service, commonConfig, selection, null, null);
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Creating File with Entity Metadata at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        #endregion Создание файла с мета-данными сущности.

        #region Открытие Entity Attribute Explorer.

        public async Task ExecuteOpeningEntityAttributeExplorer(string selection, ConnectionData connectionData, CommonConfiguration commonConfig)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Opening Entity Attribute Explorer at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
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

                WindowHelper.OpenEntityAttributeExplorer(this._iWriteToOutput, service, commonConfig, selection);
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Opening Entity Attribute Explorer at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        #endregion Открытие Entity Attribute Explorer.

        public async Task ExecuteOpeningEntityKeyExplorer(string selection, ConnectionData connectionData, CommonConfiguration commonConfig)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Opening Entity Key Explorer at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
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

                WindowHelper.OpenEntityKeyExplorer(this._iWriteToOutput, service, commonConfig, selection);
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Opening Entity Key Explorer at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        public async Task ExecuteOpeningEntityRelationshipOneToManyExplorer(string selection, ConnectionData connectionData, CommonConfiguration commonConfig)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Opening Entity Relationship One-To-Many at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
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

                WindowHelper.OpenEntityRelationshipOneToManyExplorer(this._iWriteToOutput, service, commonConfig, selection);
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Opening Entity Relationship One-To-Many Explorer at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        public async Task ExecuteOpeningEntityRelationshipManyToManyExplorer(string selection, ConnectionData connectionData, CommonConfiguration commonConfig)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Opening Entity Relationship Many-To-Many at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
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

                WindowHelper.OpenEntityRelationshipManyToManyExplorer(this._iWriteToOutput, service, commonConfig, selection);
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Opening Entity Relationship Many-To-Many Explorer at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        #region Создание файла с глобальными OptionSet-ами.

        public async Task ExecuteCreatingFileWithGlobalOptionSets(ConnectionData connectionData, CommonConfiguration commonConfig, string selection)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Creating File with Global OptionSets at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
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

                WindowHelper.OpenGlobalOptionSetsWindow(
                    this._iWriteToOutput
                    , service
                    , commonConfig
                    , null
                    , null
                    , selection
                    );
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Creating File with Global OptionSets at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        #endregion Создание файла с глобальными OptionSet-ами.

        #region Обновление файла с мета-данными сущности.

        public async Task ExecuteUpdateFileWithEntityMetadata(List<SelectedFile> selectedFiles, ConnectionData connectionData, CommonConfiguration commonConfig, bool selectEntity)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Updating File with Entity Metadata at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
            {
                await UpdatingFileWithEntityMetadata(selectedFiles, connectionData, commonConfig, selectEntity);
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Updating File with Entity Metadata at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        private async Task UpdatingFileWithEntityMetadata(List<SelectedFile> selectedFiles, ConnectionData connectionData, CommonConfiguration commonConfig, bool selectEntity)
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

            var descriptor = new SolutionComponentDescriptor(service, true);

            foreach (var selFile in selectedFiles)
            {
                var filePath = selFile.FilePath;

                if (!File.Exists(filePath))
                {
                    this._iWriteToOutput.WriteToOutput("File {0} not exists.", filePath);
                    continue;
                }

                var selection = Path.GetFileNameWithoutExtension(filePath);
                selection = selection.Split('.').FirstOrDefault();

                bool tempSelectEntity = selectEntity;

                if (!tempSelectEntity)
                {
                    var metadata = descriptor.MetadataSource.GetEntityMetadata(selection.ToLower());

                    if (metadata != null)
                    {
                        string tabSpacer = CreateFileHandler.GetTabSpacer(commonConfig.IndentType, commonConfig.SpaceCount);

                        var config = new CreateFileWithEntityMetadataCSharpConfiguration(
                            metadata.LogicalName
                            , Path.GetDirectoryName(filePath)
                            , tabSpacer
                            , commonConfig.GenerateAttributes
                            , commonConfig.GenerateStatus
                            , commonConfig.GenerateLocalOptionSet
                            , commonConfig.GenerateGlobalOptionSet
                            , commonConfig.GenerateOneToMany
                            , commonConfig.GenerateManyToOne
                            , commonConfig.GenerateManyToMany
                            , commonConfig.GenerateKeys
                            , commonConfig.AllDescriptions
                            , commonConfig.CreateGlobalOptionSetsWithDependentComponents
                            , commonConfig.GenerateIntoSchemaClass
                            , commonConfig.WithManagedInfo
                            , commonConfig.ConstantType
                            , commonConfig.OptionSetExportType
                            )
                        {
                            EntityMetadata = metadata
                        };

                        this._iWriteToOutput.WriteToOutput("Start creating file with Entity Metadata for {0} at {1}", config.EntityName, DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

                        using (var handler = new CreateFileWithEntityMetadataCSharpHandler(config, service, _iWriteToOutput))
                        {
                            string fileName = Path.GetFileName(filePath);

                            await handler.CreateFileAsync(fileName);
                        }

                        this._iWriteToOutput.WriteToOutput("For entity '{0}' created file with Metadata: {1}", config.EntityName, filePath);

                        this._iWriteToOutput.WriteToOutputFilePathUri(filePath);

                        this._iWriteToOutput.WriteToOutput("End creating file with Entity Metadata for {0} at {1}", config.EntityName, DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

                        continue;
                    }
                    else
                    {
                        tempSelectEntity = true;
                    }
                }

                if (tempSelectEntity)
                {
                    var tempService = await QuickConnection.ConnectAsync(connectionData);

                    WindowHelper.OpenEntityMetadataWindow(this._iWriteToOutput, tempService, commonConfig, selection, null, filePath);
                }
            }
        }

        #endregion Обновление файла с мета-данными сущности.

        #region Обновление файла с глобальными OptionSet-ами.

        public async Task ExecuteUpdatingFileWithGlobalOptionSets(ConnectionData connectionData, CommonConfiguration commonConfig, IEnumerable<SelectedFile> selectedFiles, bool withSelect)
        {
            this._iWriteToOutput.WriteToOutput("*********** Start Updating File with Global OptionSets at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
            {
                await UpdatingFileWithGlobalOptionSets(connectionData, commonConfig, selectedFiles, withSelect);
            }
            catch (Exception xE)
            {
                this._iWriteToOutput.WriteErrorToOutput(xE);
            }
            finally
            {
                this._iWriteToOutput.WriteToOutput("*********** End Updating File with Global OptionSets at {0} *******************************************************", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
            }
        }

        private async Task UpdatingFileWithGlobalOptionSets(ConnectionData connectionData, CommonConfiguration commonConfig, IEnumerable<SelectedFile> selectedFiles, bool withSelect)
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

            var descriptor = new SolutionComponentDescriptor(service, true);

            foreach (var selFile in selectedFiles)
            {
                var filePath = selFile.FilePath;

                if (!File.Exists(filePath))
                {
                    this._iWriteToOutput.WriteToOutput("File {0} not exists.", filePath);
                    continue;
                }

                var selection = Path.GetFileNameWithoutExtension(filePath);
                selection = selection.Split('.').FirstOrDefault();

                bool tempWithSelect = withSelect;

                if (!tempWithSelect)
                {
                    var metadata = descriptor.MetadataSource.GetOptionSetMetadata(selection.ToLower());

                    if (metadata != null)
                    {
                        this._iWriteToOutput.WriteToOutput("Start creating file with OptionSet Metadata for {0} at {1}", metadata.Name, DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

                        string tabSpacer = CreateFileHandler.GetTabSpacer(commonConfig.IndentType, commonConfig.SpaceCount);

                        using (var handler = new CreateGlobalOptionSetsFileCSharpHandler(
                                   service
                                   , _iWriteToOutput
                                   , tabSpacer
                                   , commonConfig.ConstantType
                                   , commonConfig.OptionSetExportType
                                   , commonConfig.CreateGlobalOptionSetsWithDependentComponents
                                   , commonConfig.WithManagedInfo
                                   , true
                                   ))
                        {
                            await handler.CreateFileAsync(filePath, new[] { metadata });
                        }

                        this._iWriteToOutput.WriteToOutput("For OptionSet '{0}' created file with Metadata: {1}", metadata.Name, filePath);

                        this._iWriteToOutput.WriteToOutputFilePathUri(filePath);

                        this._iWriteToOutput.WriteToOutput("End creating file with OptionSet Metadata for {0} at {1}", metadata.Name, DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

                        continue;
                    }
                    else
                    {
                        tempWithSelect = true;
                    }
                }

                if (tempWithSelect)
                {
                    var tempService = await QuickConnection.ConnectAsync(connectionData);

                    WindowHelper.OpenGlobalOptionSetsWindow(
                        this._iWriteToOutput
                        , tempService
                        , commonConfig
                        , null
                        , filePath
                        , selection
                        );
                }
            }
        }

        #endregion Обновление файла с глобальными OptionSet-ами.
    }
}
