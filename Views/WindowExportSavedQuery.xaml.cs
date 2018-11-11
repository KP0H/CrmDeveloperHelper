using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Nav.Common.VSPackages.CrmDeveloperHelper.Commands;
using Nav.Common.VSPackages.CrmDeveloperHelper.Controllers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Entities;
using Nav.Common.VSPackages.CrmDeveloperHelper.Helpers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using Nav.Common.VSPackages.CrmDeveloperHelper.Model;
using Nav.Common.VSPackages.CrmDeveloperHelper.Repository;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Resources;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Views
{
    public partial class WindowExportSavedQuery : WindowBase
    {
        private readonly object sysObjectConnections = new object();

        private IWriteToOutput _iWriteToOutput;

        private CommonConfiguration _commonConfig;
        private ConnectionConfiguration _connectionConfig;

        private string _filterEntity;

        private bool _controlsEnabled = true;

        private ObservableCollection<EntityViewItem> _itemsSource;

        private Dictionary<Guid, IOrganizationServiceExtented> _connectionCache = new Dictionary<Guid, IOrganizationServiceExtented>();
        private Dictionary<Guid, SolutionComponentDescriptor> _descriptorCache = new Dictionary<Guid, SolutionComponentDescriptor>();

        private int _init = 0;

        public WindowExportSavedQuery(
             IWriteToOutput iWriteToOutput
            , IOrganizationServiceExtented service
            , CommonConfiguration commonConfig
            , string filterEntity
            , string selection
            )
        {
            _init++;

            InputLanguageManager.SetInputLanguage(this, CultureInfo.CreateSpecificCulture("en-US"));

            this._iWriteToOutput = iWriteToOutput;
            this._commonConfig = commonConfig;
            this._connectionConfig = service.ConnectionData.ConnectionConfiguration;
            this._filterEntity = filterEntity;

            _connectionCache[service.ConnectionData.ConnectionId] = service;
            _descriptorCache[service.ConnectionData.ConnectionId] = new SolutionComponentDescriptor(service, true);

            BindingOperations.EnableCollectionSynchronization(_connectionConfig.Connections, sysObjectConnections);

            InitializeComponent();

            LoadFromConfig();

            if (!string.IsNullOrEmpty(selection))
            {
                txtBFilter.Text = selection;
            }

            if (string.IsNullOrEmpty(_filterEntity))
            {
                btnClearEntityFilter.IsEnabled = sepClearEntityFilter.IsEnabled = false;
                btnClearEntityFilter.Visibility = sepClearEntityFilter.Visibility = Visibility.Collapsed;
            }

            txtBFilter.SelectionLength = 0;
            txtBFilter.SelectionStart = txtBFilter.Text.Length;

            txtBFilter.Focus();

            this._itemsSource = new ObservableCollection<EntityViewItem>();

            this.lstVwSavedQueries.ItemsSource = _itemsSource;

            cmBCurrentConnection.ItemsSource = _connectionConfig.Connections;
            cmBCurrentConnection.SelectedItem = service.ConnectionData;

            _init--;

            if (service != null)
            {
                ShowExistingSavedQueries();
            }
        }

        private void LoadFromConfig()
        {
            cmBFileAction.DataContext = _commonConfig;

            txtBFolder.DataContext = _commonConfig;
        }

        protected override void OnClosed(EventArgs e)
        {
            _commonConfig.Save();
            _connectionConfig.Save();

            BindingOperations.ClearAllBindings(cmBCurrentConnection);

            cmBCurrentConnection.Items.DetachFromSourceCollection();

            cmBCurrentConnection.ItemsSource = null;

            base.OnClosed(e);
        }

        private async Task<IOrganizationServiceExtented> GetService()
        {
            ConnectionData connectionData = null;

            cmBCurrentConnection.Dispatcher.Invoke(() =>
            {
                connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;
            });

            if (connectionData != null)
            {
                if (!_connectionCache.ContainsKey(connectionData.ConnectionId))
                {
                    _iWriteToOutput.WriteToOutput("Connection to CRM.");
                    _iWriteToOutput.WriteToOutput(connectionData.GetConnectionDescription());
                    var service = await QuickConnection.ConnectAsync(connectionData);
                    _iWriteToOutput.WriteToOutput("Current Service Endpoint: {0}", service.CurrentServiceEndpoint);

                    _connectionCache[connectionData.ConnectionId] = service;
                }

                return _connectionCache[connectionData.ConnectionId];
            }

            return null;
        }

        private async Task<SolutionComponentDescriptor> GetDescriptor()
        {
            ConnectionData connectionData = null;

            cmBCurrentConnection.Dispatcher.Invoke(() =>
            {
                connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;
            });

            if (connectionData != null)
            {
                if (!_descriptorCache.ContainsKey(connectionData.ConnectionId))
                {
                    var service = await GetService();

                    _descriptorCache[connectionData.ConnectionId] = new SolutionComponentDescriptor(service, true);
                }

                return _descriptorCache[connectionData.ConnectionId];
            }

            return null;
        }

        private async Task ShowExistingSavedQueries()
        {
            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            ToggleControls(false, Properties.WindowStatusStrings.LoadingSavedQueries);

            this._itemsSource.Clear();

            string textName = string.Empty;

            txtBFilter.Dispatcher.Invoke(() =>
            {
                textName = txtBFilter.Text.Trim().ToLower();
            });

            IEnumerable<SavedQuery> list = Enumerable.Empty<SavedQuery>();

            try
            {
                var service = await GetService();

                if (service != null)
                {
                    var repository = new SavedQueryRepository(service);
                    list = await repository.GetListAsync(this._filterEntity, new ColumnSet(SavedQuery.Schema.Attributes.name, SavedQuery.Schema.Attributes.returnedtypecode, SavedQuery.Schema.Attributes.querytype, SavedQuery.Schema.Attributes.iscustomizable));
                }
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);
            }

            list = FilterList(list, textName);

            LoadSavedQueries(list);
        }

        private static IEnumerable<SavedQuery> FilterList(IEnumerable<SavedQuery> list, string textName)
        {
            if (!string.IsNullOrEmpty(textName))
            {
                textName = textName.ToLower();

                if (Guid.TryParse(textName, out Guid tempGuid))
                {
                    list = list.Where(ent => ent.Id == tempGuid || ent.SavedQueryIdUnique == tempGuid);
                }
                else
                {
                    list = list.Where(ent =>
                    {
                        var type = ent.ReturnedTypeCode.ToLower();
                        var name = ent.Name.ToLower();

                        return type.Contains(textName) || name.Contains(textName);
                    });
                }
            }

            return list;
        }

        private class EntityViewItem
        {
            public string EntityName { get; private set; }

            public string QueryName { get; private set; }

            public string QueryType { get; private set; }

            public SavedQuery SavedQuery { get; private set; }

            public EntityViewItem(string entityName, string queryName, string queryType, SavedQuery savedQuery)
            {
                this.EntityName = entityName;
                this.QueryName = queryName;
                this.QueryType = queryType;
                this.SavedQuery = savedQuery;
            }
        }

        private void LoadSavedQueries(IEnumerable<SavedQuery> results)
        {
            this._iWriteToOutput.WriteToOutput("Found {0} saved query(ies).", results.Count());

            this.lstVwSavedQueries.Dispatcher.Invoke(() =>
            {
                foreach (var entity in results
                    .OrderBy(ent => ent.ReturnedTypeCode)
                    .ThenBy(ent => ent.QueryType)
                    .ThenBy(ent => ent.Name)
                )
                {
                    int queryType = entity.QueryType.Value;

                    string queryTypeName = SavedQueryRepository.GetQueryTypeName(queryType);

                    var item = new EntityViewItem(entity.ReturnedTypeCode, entity.Name, queryTypeName, entity);

                    this._itemsSource.Add(item);
                }

                if (this.lstVwSavedQueries.Items.Count == 1)
                {
                    this.lstVwSavedQueries.SelectedItem = this.lstVwSavedQueries.Items[0];
                }
            });

            ToggleControls(true, Properties.WindowStatusStrings.LoadingSavedQueriesCompletedFormat, results.Count());
        }

        private void UpdateStatus(string format, params object[] args)
        {
            string message = format;

            if (args != null && args.Length > 0)
            {
                message = string.Format(format, args);
            }

            this.stBIStatus.Dispatcher.Invoke(() =>
            {
                this.stBIStatus.Content = message;
            });
        }

        private void ToggleControls(bool enabled, string statusFormat, params object[] args)
        {
            this._controlsEnabled = enabled;

            UpdateStatus(statusFormat, args);

            ToggleControl(cmBCurrentConnection, enabled);

            ToggleProgressBar(enabled);

            UpdateButtonsEnable();
        }

        private void ToggleProgressBar(bool enabled)
        {
            if (tSProgressBar == null)
            {
                return;
            }

            this.tSProgressBar.Dispatcher.Invoke(() =>
            {
                tSProgressBar.IsIndeterminate = !enabled;
            });
        }

        private void ToggleControl(Control c, bool enabled)
        {
            c.Dispatcher.Invoke(() =>
            {
                if (c is TextBox)
                {
                    ((TextBox)c).IsReadOnly = !enabled;
                }
                else
                {
                    c.IsEnabled = enabled;
                }
            });
        }

        private void UpdateButtonsEnable()
        {
            this.lstVwSavedQueries.Dispatcher.Invoke(() =>
            {
                try
                {
                    bool enabled = this._controlsEnabled && this.lstVwSavedQueries.SelectedItems.Count > 0;

                    UIElement[] list = { tSDDBExportSavedQuery, btnExportAll };

                    foreach (var button in list)
                    {
                        button.IsEnabled = enabled;
                    }
                }
                catch (Exception)
                {
                }
            });
        }

        private void txtBFilterEnitity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ShowExistingSavedQueries();
            }
        }

        private SavedQuery GetSelectedEntity()
        {
            return this.lstVwSavedQueries.SelectedItems.OfType<EntityViewItem>().Count() == 1
                ? this.lstVwSavedQueries.SelectedItems.OfType<EntityViewItem>().Select(e => e.SavedQuery).SingleOrDefault() : null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lstVwEntities_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (((FrameworkElement)e.OriginalSource).DataContext is EntityViewItem item)
                {
                    ExecuteAction(item.SavedQuery.Id, item.SavedQuery.ReturnedTypeCode, item.SavedQuery.Name, PerformExportMouseDoubleClick);
                }
            }
        }

        private async Task PerformExportMouseDoubleClick(string folder, Guid idSavedQuery, string entityName, string name)
        {
            //await PerformExportEntityDescription(folder, savedQuery);

            await PerformExportXmlToFile(folder, idSavedQuery, entityName, name, SavedQuery.Schema.Attributes.fetchxml, "FetchXml");

            await PerformExportXmlToFile(folder, idSavedQuery, entityName, name, SavedQuery.Schema.Attributes.layoutxml, "LayoutXml");

            await PerformExportXmlToFile(folder, idSavedQuery, entityName, name, SavedQuery.Schema.Attributes.columnsetxml, "ColumnSetXml");
        }

        private void lstVwEntities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsEnable();
        }

        private async Task ExecuteAction(Guid idSavedQuery, string entityName, string name, Func<string, Guid, string, string, Task> action)
        {
            string folder = txtBFolder.Text.Trim();

            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            if (!Directory.Exists(folder))
            {
                return;
            }

            await action(folder, idSavedQuery, entityName, name);
        }

        private Task<string> CreateFileAsync(string folder, string entityName, string name, string fieldTitle, string xmlContent, string extension)
        {
            return Task.Run(() => CreateFile(folder, entityName, name, fieldTitle, xmlContent, extension));
        }

        private string CreateFile(string folder, string entityName, string name, string fieldTitle, string xmlContent, string extension)
        {
            ConnectionData connectionData = null;

            cmBCurrentConnection.Dispatcher.Invoke(() =>
            {
                connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;
            });

            if (connectionData == null)
            {
                return null;
            }

            string fileName = EntityFileNameFormatter.GetSavedQueryFileName(connectionData.Name, entityName, name, fieldTitle, extension);
            string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

            if (!string.IsNullOrEmpty(xmlContent))
            {
                try
                {
                    if (string.Equals(extension, "xml", StringComparison.OrdinalIgnoreCase) && ContentCoparerHelper.TryParseXml(xmlContent, out var doc))
                    {
                        xmlContent = doc.ToString();
                    }

                    File.WriteAllText(filePath, xmlContent, new UTF8Encoding(false));

                    this._iWriteToOutput.WriteToOutput("{0} System View (Saved Query) {1} {2} exported to {3}", connectionData.Name, name, fieldTitle, filePath);
                }
                catch (Exception ex)
                {
                    this._iWriteToOutput.WriteErrorToOutput(ex);
                }
            }
            else
            {
                this._iWriteToOutput.WriteToOutput("System View (Saved Query) {0} {1} is empty.", name, fieldTitle);
                this._iWriteToOutput.ActivateOutputWindow();
            }

            return filePath;
        }

        private void btnExportAll_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteAction(entity.Id, entity.ReturnedTypeCode, entity.Name, PerformExportAllXml);
        }

        private async Task PerformExportAllXml(string folder, Guid idSavedQuery, string entityName, string name)
        {
            await PerformExportEntityDescription(folder, idSavedQuery, entityName, name);

            await PerformExportXmlToFile(folder, idSavedQuery, entityName, name, SavedQuery.Schema.Attributes.fetchxml, "FetchXml");

            await PerformExportXmlToFile(folder, idSavedQuery, entityName, name, SavedQuery.Schema.Attributes.layoutxml, "LayoutXml");

            await PerformExportXmlToFile(folder, idSavedQuery, entityName, name, SavedQuery.Schema.Attributes.columnsetxml, "ColumnSetXml");
        }

        private async Task ExecuteActionEntity(Guid idSavedQuery, string entityName, string name, string fieldName, string fieldTitle, Func<string, Guid, string, string, string, string, Task> action)
        {
            string folder = txtBFolder.Text.Trim();

            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            if (!Directory.Exists(folder))
            {
                return;
            }

            await action(folder, idSavedQuery, entityName, name, fieldName, fieldTitle);
        }

        private async Task PerformExportXmlToFile(string folder, Guid idSavedQuery, string entityName, string name, string fieldName, string fieldTitle)
        {
            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            ToggleControls(false, Properties.WindowStatusStrings.ExportingXmlFieldToFileFormat, fieldName);

            try
            {
                var service = await GetService();

                var repository = new SavedQueryRepository(service);

                var savedQuery = await repository.GetByIdAsync(idSavedQuery, new ColumnSet(fieldName));

                string xmlContent = savedQuery.GetAttributeValue<string>(fieldName);

                string filePath = await CreateFileAsync(folder, entityName, name, fieldTitle, xmlContent, "xml");

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);

                ToggleControls(true, Properties.WindowStatusStrings.ExportingXmlFieldToFileCompletedFormat, fieldName);
            }
            catch (Exception ex)
            {
                _iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, Properties.WindowStatusStrings.ExportingXmlFieldToFileFailedFormat, fieldName);
            }
        }

        private async Task PerformCopyToClipboardXmlToFileJavaScript(string folder, Guid idSavedQuery, string entityName, string name, string fieldName, string fieldTitle)
        {
            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            ToggleControls(false, "Coping Xml {0} to Clipboard...", fieldName);

            try
            {
                var service = await GetService();

                var repository = new SavedQueryRepository(service);

                var savedQuery = await repository.GetByIdAsync(idSavedQuery, new ColumnSet(fieldName));

                string xmlContent = savedQuery.GetAttributeValue<string>(fieldName);

                if (ContentCoparerHelper.TryParseXml(xmlContent, out var doc))
                {
                    xmlContent = doc.ToString();

                    ContentCoparerHelper.ClearXsdSchema(xmlContent, out xmlContent);

                    xmlContent = ContentCoparerHelper.FormatToJavaScript(fieldName, xmlContent);
                }

                Clipboard.SetText(xmlContent);

                ToggleControls(true, "Coping Xml {0} to Clipboard completed.", fieldName);
            }
            catch (Exception ex)
            {
                _iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Coping Xml {0} to Clipboard failed.", fieldName);
            }
        }

        private async Task PerformUpdateEntityField(string folder, Guid idSavedQuery, string entityName, string name, string fieldName, string fieldTitle)
        {
            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            ToggleControls(false, Properties.WindowStatusStrings.UpdatingFieldFormat, fieldName);

            try
            {
                var service = await GetService();

                var repository = new SavedQueryRepository(service);

                var savedQuery = await repository.GetByIdAsync(idSavedQuery, new ColumnSet(fieldName, SavedQuery.Schema.Attributes.querytype));

                string xmlContent = savedQuery.GetAttributeValue<string>(fieldName);

                {
                    if (ContentCoparerHelper.TryParseXml(xmlContent, out var tempDoc))
                    {
                        xmlContent = tempDoc.ToString();
                    }
                }

                string filePath = await CreateFileAsync(folder, entityName, name, fieldTitle + " BackUp", xmlContent, "xml");

                var newText = string.Empty;
                bool? dialogResult = false;

                this.Dispatcher.Invoke(() =>
                {
                    var form = new WindowTextField("Enter " + fieldTitle, fieldTitle, xmlContent);

                    dialogResult = form.ShowDialog();

                    newText = form.FieldText;
                });

                if (dialogResult.GetValueOrDefault() == false)
                {
                    ToggleControls(true, Properties.WindowStatusStrings.UpdatingFieldCanceledFormat, fieldName);
                    return;
                }

                ContentCoparerHelper.ClearXsdSchema(newText, out newText);

                _iWriteToOutput.WriteToOutput("Validating {0}...", fieldTitle);
                UpdateStatus("Validating {0}.", fieldTitle);

                if (!ContentCoparerHelper.TryParseXmlDocument(newText, out var doc))
                {
                    _iWriteToOutput.WriteToOutput(Properties.WindowStatusStrings.TextIsNotValidXml);
                    ToggleControls(true, Properties.WindowStatusStrings.TextIsNotValidXml);

                    _iWriteToOutput.ActivateOutputWindow();
                    return;
                }

                bool validateResult = await ValidateXmlDocumentAsync(doc);

                if (!validateResult)
                {
                    ToggleControls(true, Properties.WindowStatusStrings.ValidatingXmlForFieldFailedFormat, fieldName);

                    return;
                }

                if (string.Equals(fieldName, SavedQuery.Schema.Attributes.fetchxml, StringComparison.InvariantCulture))
                {
                    _iWriteToOutput.WriteToOutput("Executing ValidateSavedQueryRequest.");

                    var request = new ValidateSavedQueryRequest()
                    {
                        FetchXml = newText,
                        QueryType = savedQuery.QueryType.GetValueOrDefault()
                    };

                    service.Execute(request);
                }

                var updateEntity = new SavedQuery
                {
                    Id = idSavedQuery
                };
                updateEntity.Attributes[fieldName] = newText;

                service.Update(updateEntity);

                ToggleControls(true, Properties.WindowStatusStrings.UpdatingFieldCompletedFormat, fieldName);
            }
            catch (Exception ex)
            {
                _iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, Properties.WindowStatusStrings.UpdatingFieldFailedFormat, fieldName);
            }
        }

        private Task<bool> ValidateXmlDocumentAsync(XDocument doc)
        {
            return Task.Run(() => ValidateXmlDocument(doc));
        }

        private bool ValidateXmlDocument(XDocument doc)
        {
            XmlSchemaSet schemas = new XmlSchemaSet();

            {
                var schemasResources = CommonExportXsdSchemasCommand.ListXsdSchemas.FirstOrDefault(e => string.Equals(e.Item1, "Fetch.xsd", StringComparison.InvariantCultureIgnoreCase));

                if (schemasResources != null)
                {
                    foreach (var fileName in schemasResources.Item2)
                    {
                        Uri uri = FileOperations.GetSchemaResourceUri(fileName);
                        StreamResourceInfo info = Application.GetResourceStream(uri);

                        using (StreamReader reader = new StreamReader(info.Stream))
                        {
                            schemas.Add("", XmlReader.Create(reader));
                        }
                    }
                }
            }

            List<ValidationEventArgs> errors = new List<ValidationEventArgs>();

            doc.Validate(schemas, (o, e) =>
            {
                errors.Add(e);
            });

            if (errors.Count > 0)
            {
                _iWriteToOutput.WriteToOutput("Text is not valid.");

                foreach (var item in errors)
                {
                    _iWriteToOutput.WriteToOutput(string.Empty);
                    _iWriteToOutput.WriteToOutput(string.Empty);
                    _iWriteToOutput.WriteToOutput("Severity: {0}      Message: {1}", item.Severity, item.Message);
                    _iWriteToOutput.WriteErrorToOutput(item.Exception);
                }

                _iWriteToOutput.ActivateOutputWindow();
            }

            return errors.Count == 0;
        }

        private void mIExportSavedQueryFetchXml_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteActionEntity(entity.Id, entity.ReturnedTypeCode, entity.Name, SavedQuery.Schema.Attributes.fetchxml, "FetchXml", PerformExportXmlToFile);
        }

        private void mIExportSavedQueryFetchXmlIntoJavaScript_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteActionEntity(entity.Id, entity.ReturnedTypeCode, entity.Name, SavedQuery.Schema.Attributes.fetchxml, "FetchXml", PerformCopyToClipboardXmlToFileJavaScript);
        }

        private void mIExportSavedQueryLayoutXml_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteActionEntity(entity.Id, entity.ReturnedTypeCode, entity.Name, SavedQuery.Schema.Attributes.layoutxml, "LayoutXml", PerformExportXmlToFile);
        }

        private void mIExportSavedQueryLayoutXmlIntoJavaScript_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteActionEntity(entity.Id, entity.ReturnedTypeCode, entity.Name, SavedQuery.Schema.Attributes.layoutxml, "LayoutXml", PerformCopyToClipboardXmlToFileJavaScript);
        }

        private void mIExportSavedQueryColumnSetXml_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteActionEntity(entity.Id, entity.ReturnedTypeCode, entity.Name, SavedQuery.Schema.Attributes.columnsetxml, "ColumnSetXml", PerformExportXmlToFile);
        }

        private void mIExportSavedQueryColumnSetXmlIntoJavaScript_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteActionEntity(entity.Id, entity.ReturnedTypeCode, entity.Name, SavedQuery.Schema.Attributes.columnsetxml, "ColumnSetXml", PerformCopyToClipboardXmlToFileJavaScript);
        }

        private void mICreateEntityDescription_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteAction(entity.Id, entity.ReturnedTypeCode, entity.Name, PerformExportEntityDescription);
        }

        private async Task PerformExportEntityDescription(string folder, Guid idSavedQuery, string entityName, string name)
        {
            ToggleControls(false, Properties.WindowStatusStrings.CreatingEntityDescription);

            try
            {
                var service = await GetService();

                string fileName = EntityFileNameFormatter.GetSavedQueryFileName(service.ConnectionData.Name, entityName, name, "EntityDescription", "txt");
                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                var repository = new SavedQueryRepository(service);

                var savedQuery = await repository.GetByIdAsync(idSavedQuery, new ColumnSet(true));

                await EntityDescriptionHandler.ExportEntityDescriptionAsync(filePath, savedQuery, EntityFileNameFormatter.SavedQueryIgnoreFields, service.ConnectionData);

                this._iWriteToOutput.WriteToOutput("SavedQuery Entity Description exported to {0}", filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);

                this._iWriteToOutput.WriteToOutput("End creating file at {0}", DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

                ToggleControls(true, Properties.WindowStatusStrings.CreatingEntityDescriptionCompleted);
            }
            catch (Exception ex)
            {
                _iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, Properties.WindowStatusStrings.CreatingEntityDescriptionFailed);
            }
        }

        private void btnClearEntityFilter_Click(object sender, RoutedEventArgs e)
        {
            this._filterEntity = null;

            btnClearEntityFilter.IsEnabled = sepClearEntityFilter.IsEnabled = false;
            btnClearEntityFilter.Visibility = sepClearEntityFilter.Visibility = Visibility.Collapsed;

            ShowExistingSavedQueries();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                e.Handled = true;

                ShowExistingSavedQueries();
            }

            base.OnKeyDown(e);
        }

        private void mIExportSavedQueryDependentComponents_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteAction(entity.Id, entity.ReturnedTypeCode, entity.Name, PerformCreatingFileWithDependentComponents);
        }

        private void mIExportSavedQueryRequiredComponents_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteAction(entity.Id, entity.ReturnedTypeCode, entity.Name, PerformCreatingFileWithRequiredComponents);
        }

        private void mIExportSavedQueryDependenciesForDelete_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteAction(entity.Id, entity.ReturnedTypeCode, entity.Name, PerformCreatingFileWithDependenciesForDelete);
        }

        private async Task PerformCreatingFileWithDependentComponents(string folder, Guid idSavedQuery, string entityName, string name)
        {
            this._iWriteToOutput.WriteToOutput("Starting downloading {0}", name);

            var removeWrongFromName = FileOperations.RemoveWrongSymbols(name);

            var service = await GetService();
            var descriptor = await GetDescriptor();

            var dependencyRepository = new DependencyRepository(service);

            var descriptorHandler = new DependencyDescriptionHandler(descriptor);

            var coll = await dependencyRepository.GetDependentComponentsAsync((int)ComponentType.SavedQuery, idSavedQuery);

            string description = await descriptorHandler.GetDescriptionDependentAsync(coll);

            if (!string.IsNullOrEmpty(description))
            {
                string fileName = EntityFileNameFormatter.GetSavedQueryFileName(
                    service.ConnectionData.Name
                    , entityName
                    , removeWrongFromName
                    , "Dependent Components"
                    , "txt"
                    );

                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                File.WriteAllText(filePath, description, new UTF8Encoding(false));

                this._iWriteToOutput.WriteToOutput("SavedQuery {0} Dependent Components exported to {1}", name, filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);
            }
            else
            {
                this._iWriteToOutput.WriteToOutput("SavedQuery {0} has no Dependent Components.", name);
                this._iWriteToOutput.ActivateOutputWindow();
            }
        }

        private async Task PerformCreatingFileWithRequiredComponents(string folder, Guid idSavedQuery, string entityName, string name)
        {
            this._iWriteToOutput.WriteToOutput("Starting downloading {0}", name);

            var removeWrongFromName = FileOperations.RemoveWrongSymbols(name);

            var service = await GetService();
            var descriptor = await GetDescriptor();

            var dependencyRepository = new DependencyRepository(service);

            var descriptorHandler = new DependencyDescriptionHandler(descriptor);

            var coll = await dependencyRepository.GetRequiredComponentsAsync((int)ComponentType.SavedQuery, idSavedQuery);

            string description = await descriptorHandler.GetDescriptionRequiredAsync(coll);

            if (!string.IsNullOrEmpty(description))
            {
                string fileName = EntityFileNameFormatter.GetSavedQueryFileName(
                    service.ConnectionData.Name
                    , entityName
                    , removeWrongFromName
                    , "Required Components"
                    , "txt"
                    );

                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                File.WriteAllText(filePath, description, new UTF8Encoding(false));

                this._iWriteToOutput.WriteToOutput("SavedQuery {0} Required Components exported to {1}", name, filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);
            }
            else
            {
                this._iWriteToOutput.WriteToOutput("SavedQuery {0} has no Required Components.", name);
                this._iWriteToOutput.ActivateOutputWindow();
            }
        }

        private async Task PerformCreatingFileWithDependenciesForDelete(string folder, Guid idSavedQuery, string entityName, string name)
        {
            this._iWriteToOutput.WriteToOutput("Starting downloading {0}", name);

            var removeWrongFromName = FileOperations.RemoveWrongSymbols(name);

            var service = await GetService();
            var descriptor = await GetDescriptor();

            var dependencyRepository = new DependencyRepository(service);

            var descriptorHandler = new DependencyDescriptionHandler(descriptor);

            var coll = await dependencyRepository.GetDependenciesForDeleteAsync((int)ComponentType.SavedQuery, idSavedQuery);

            string description = await descriptorHandler.GetDescriptionDependentAsync(coll);

            if (!string.IsNullOrEmpty(description))
            {
                string fileName = EntityFileNameFormatter.GetSavedQueryFileName(
                    service.ConnectionData.Name
                    , entityName
                    , removeWrongFromName
                    , "Dependencies For Delete"
                    , "txt"
                    );

                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                File.WriteAllText(filePath, description, new UTF8Encoding(false));

                this._iWriteToOutput.WriteToOutput("SavedQuery {0} Dependencies For Delete exported to {1}", name, filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);
            }
            else
            {
                this._iWriteToOutput.WriteToOutput("SavedQuery {0} has no Dependencies For Delete.", name);
                this._iWriteToOutput.ActivateOutputWindow();
            }
        }

        private void mIOpenDependentComponentsInWeb_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ConnectionData connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

            if (connectionData != null)
            {
                connectionData.OpenSolutionComponentDependentComponentsInWeb(ComponentType.SavedQuery, entity.Id);
            }
        }

        private async void mIOpenInWeb_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            var service = await GetService();

            if (service != null)
            {
                service.UrlGenerator.OpenSolutionComponentInWeb(ComponentType.SavedQuery, entity.Id);
            }
        }

        private void mIOpenEntityInWeb_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null
                || string.IsNullOrEmpty(entity.ReturnedTypeCode)
                || string.Equals(entity.ReturnedTypeCode, "none", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                return;
            }

            ConnectionData connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

            if (connectionData != null)
            {
                connectionData.OpenEntityMetadataInWeb(entity.ReturnedTypeCode);
            }
        }

        private void mIOpenEntityListInWeb_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null
                || string.IsNullOrEmpty(entity.ReturnedTypeCode)
                || string.Equals(entity.ReturnedTypeCode, "none", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                return;
            }

            ConnectionData connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

            if (connectionData != null)
            {
                connectionData.OpenEntityListInWeb(entity.ReturnedTypeCode);
            }
        }

        private async void AddIntoCrmSolution_Click(object sender, RoutedEventArgs e)
        {
            await AddIntoSolution(true, null);
        }

        private async void AddIntoCrmSolutionLast_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
               && menuItem.Tag != null
               && menuItem.Tag is string solutionUniqueName
               )
            {
                await AddIntoSolution(false, solutionUniqueName);
            }
        }

        private async Task AddIntoSolution(bool withSelect, string solutionUniqueName)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            _commonConfig.Save();

            var service = await GetService();
            var descriptor = await GetDescriptor();

            try
            {
                this._iWriteToOutput.ActivateOutputWindow();

                await SolutionController.AddSolutionComponentsGroupIntoSolution(_iWriteToOutput, service, descriptor, _commonConfig, solutionUniqueName, ComponentType.SavedQuery, new[] { entity.Id }, null, withSelect);
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);
            }
        }

        private async void mIAddEntityIntoCrmSolution_Click(object sender, RoutedEventArgs e)
        {
            await AddEntityIntoSolution(true, null);
        }

        private async void mIAddEntityIntoCrmSolutionLast_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
               && menuItem.Tag != null
               && menuItem.Tag is string solutionUniqueName
               )
            {
                await AddEntityIntoSolution(false, solutionUniqueName);
            }
        }

        private async Task AddEntityIntoSolution(bool withSelect, string solutionUniqueName)
        {
            var entity = GetSelectedEntity();

            if (entity == null
                || string.IsNullOrEmpty(entity.ReturnedTypeCode)
                || string.Equals(entity.ReturnedTypeCode, "none", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                return;
            }

            ConnectionData connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

            if (connectionData != null)
            {
                var entityMetadataId = connectionData.GetEntityMetadataId(entity.ReturnedTypeCode);

                if (entityMetadataId.HasValue)
                {
                    _commonConfig.Save();

                    var service = await GetService();
                    var descriptor = await GetDescriptor();

                    try
                    {
                        this._iWriteToOutput.ActivateOutputWindow();

                        await SolutionController.AddSolutionComponentsGroupIntoSolution(_iWriteToOutput, service, descriptor, _commonConfig, solutionUniqueName, ComponentType.Entity, new[] { entityMetadataId.Value }, null, withSelect);
                    }
                    catch (Exception ex)
                    {
                        this._iWriteToOutput.WriteErrorToOutput(ex);
                    }
                }
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (!(sender is ContextMenu contextMenu))
            {
                return;
            }

            var items = contextMenu.Items.OfType<Control>();

            ConnectionData connectionData = null;

            cmBCurrentConnection.Dispatcher.Invoke(() =>
            {
                connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;
            });

            FillLastSolutionItems(connectionData, items, true, AddIntoCrmSolutionLast_Click, "contMnAddIntoSolutionLast");

            var nodeItem = ((FrameworkElement)e.OriginalSource).DataContext as EntityViewItem;

            ActivateControls(items, (nodeItem.SavedQuery.IsCustomizable?.Value).GetValueOrDefault(true), "controlChangeEntityAttribute");

            bool hasEntity = !string.IsNullOrEmpty(nodeItem.SavedQuery.ReturnedTypeCode) && !string.Equals(nodeItem.SavedQuery.ReturnedTypeCode, "none", StringComparison.InvariantCultureIgnoreCase);
            ActivateControls(items, hasEntity, "contMnAddEntityIntoSolution");
            FillLastSolutionItems(connectionData, items, hasEntity, mIAddEntityIntoCrmSolutionLast_Click, "contMnAddEntityIntoSolutionLast");
        }

        private void tSDDBExportSavedQuery_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            ConnectionData connectionData = null;

            cmBCurrentConnection.Dispatcher.Invoke(() =>
            {
                connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;
            });

            var nodeItem = ((FrameworkElement)e.OriginalSource).DataContext as EntityViewItem;

            ActivateControls(tSDDBExportSavedQuery.Items.OfType<Control>(), (nodeItem.SavedQuery.IsCustomizable?.Value).GetValueOrDefault(true), "controlChangeEntityAttribute");
        }

        #region Кнопки открытия других форм с информация о сущности.

        private async void btnCreateMetadataFile_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenEntityMetadataWindow(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode, null, null);
        }

        private async void btnEntityAttributeExplorer_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenEntityAttributeExplorer(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode);
        }

        private async void btnEntityRelationshipOneToManyExplorer_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenEntityRelationshipOneToManyExplorer(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode);
        }

        private async void btnEntityRelationshipManyToManyExplorer_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenEntityRelationshipManyToManyExplorer(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode);
        }

        private async void btnEntityKeyExplorer_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenEntityKeyExplorer(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode);
        }

        private async void btnExportRibbon_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenEntityRibbonWindow(this._iWriteToOutput, service, _commonConfig, entity?.LogicalName, null);
        }

        private async void btnSystemForms_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenSystemFormWindow(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode, string.Empty);
        }

        private async void btnSavedChart_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenSavedQueryVisualizationWindow(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode, string.Empty);
        }

        private async void btnWorkflows_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenWorkflowWindow(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode, string.Empty);
        }

        private async void btnAttributesDependentComponent_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenAttributesDependentComponentWindow(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode, null);
        }

        private async void btnPluginTree_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenPluginTreeWindow(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode, string.Empty, string.Empty);
        }

        private async void btnMessageTree_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenSdkMessageTreeWindow(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode, string.Empty);
        }

        private async void btnMessageRequestTree_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenSdkMessageRequestTreeWindow(this._iWriteToOutput, service, _commonConfig, entity?.ReturnedTypeCode, string.Empty);
        }

        private async void btnOrganizationComparer_Click(object sender, RoutedEventArgs e)
        {
            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenOrganizationComparerWindow(this._iWriteToOutput, service.ConnectionData.ConnectionConfiguration, _commonConfig);
        }

        private async void btnCompareMetadataFile_Click(object sender, RoutedEventArgs e)
        {
            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenOrganizationComparerEntityMetadataWindow(this._iWriteToOutput, _commonConfig, service.ConnectionData, service.ConnectionData);
        }

        private async void btnCompareRibbon_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenOrganizationComparerRibbonWindow(this._iWriteToOutput, _commonConfig, service.ConnectionData, service.ConnectionData);
        }

        private async void btnCompareGlobalOptionSets_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenOrganizationComparerGlobalOptionSetsWindow(this._iWriteToOutput, _commonConfig, service.ConnectionData, service.ConnectionData);
        }

        private async void btnCompareSystemForms_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenOrganizationComparerSystemFormWindow(this._iWriteToOutput, _commonConfig, service.ConnectionData, service.ConnectionData, entity?.ReturnedTypeCode);
        }

        private async void btnCompareSavedQuery_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenOrganizationComparerSavedQueryWindow(this._iWriteToOutput, _commonConfig, service.ConnectionData, service.ConnectionData, entity?.ReturnedTypeCode);
        }

        private async void btnCompareSavedChart_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenOrganizationComparerSavedQueryVisualizationWindow(this._iWriteToOutput, _commonConfig, service.ConnectionData, service.ConnectionData, entity?.ReturnedTypeCode);
        }

        private async void btnCompareWorkflows_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenOrganizationComparerWorkflowWindow(this._iWriteToOutput, _commonConfig, service.ConnectionData, service.ConnectionData, entity?.ReturnedTypeCode);
        }

        #endregion Кнопки открытия других форм с информация о сущности.

        private async void mIOpenDependentComponentsInWindow_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            _commonConfig.Save();

            var service = await GetService();
            var descriptor = await GetDescriptor();

            WindowHelper.OpenSolutionComponentDependenciesWindow(
                _iWriteToOutput
                , service
                , descriptor
                , _commonConfig
                , (int)ComponentType.SavedQuery
                , entity.Id
                , null
                );
        }

        private async void mIOpenSolutionsContainingComponentInWindow_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenExplorerSolutionWindow(
                _iWriteToOutput
                , service
                , _commonConfig
                , (int)ComponentType.SavedQuery
                , entity.Id
            );
        }

        private void cmBCurrentConnection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.Dispatcher.Invoke(() =>
            {
                this._itemsSource?.Clear();
            });

            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            if (cmBCurrentConnection.SelectedItem is ConnectionData connectionData)
            {
                ShowExistingSavedQueries();
            }
        }

        private void mIUpdateSavedQueryFetchXml_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteActionEntity(entity.Id, entity.ReturnedTypeCode, entity.Name, SavedQuery.Schema.Attributes.fetchxml, "FetchXml", PerformUpdateEntityField);
        }

        private void mIUpdateSavedQueryLayoutXml_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteActionEntity(entity.Id, entity.ReturnedTypeCode, entity.Name, SavedQuery.Schema.Attributes.layoutxml, "LayoutXml", PerformUpdateEntityField);
        }

        private void mIUpdateSavedQueryColumnSetXml_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            ExecuteActionEntity(entity.Id, entity.ReturnedTypeCode, entity.Name, SavedQuery.Schema.Attributes.columnsetxml, "ColumnSetXml", PerformUpdateEntityField);
        }

        private void btnPublishEntity_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null
                && !string.IsNullOrEmpty(entity.ReturnedTypeCode)
                && !string.Equals(entity.ReturnedTypeCode, "none", StringComparison.InvariantCultureIgnoreCase)
                )
            {
                return;
            }

            ExecuteAction(entity.Id, entity.ReturnedTypeCode, entity.Name, PerformPublishEntityAsync);
        }

        private async Task PerformPublishEntityAsync(string folder, Guid idSavedQuery, string entityName, string name)
        {
            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            ToggleControls(false, Properties.WindowStatusStrings.PublishingEntitiesFormat, entityName);

            this._iWriteToOutput.WriteToOutput("Start publishing Entity {0} at {1}", entityName, DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));

            try
            {
                var service = await GetService();

                var repository = new PublishActionsRepository(service);

                await repository.PublishEntitiesAsync(new[] { entityName });

                this._iWriteToOutput.WriteToOutput("End publishing Entity {0} at {1}", entityName, DateTime.Now.ToString("G", System.Globalization.CultureInfo.CurrentCulture));
                
                ToggleControls(true, Properties.WindowStatusStrings.PublishingEntitiesCompletedFormat, entityName);
            }
            catch (Exception ex)
            {
                _iWriteToOutput.WriteErrorToOutput(ex);
                
                ToggleControls(true, Properties.WindowStatusStrings.PublishingEntitiesFailedFormat, entityName);
            }
        }
    }
}