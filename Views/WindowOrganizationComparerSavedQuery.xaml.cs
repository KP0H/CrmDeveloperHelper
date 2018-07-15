﻿using Microsoft.Xrm.Sdk;
using Nav.Common.VSPackages.CrmDeveloperHelper.Controllers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Entities;
using Nav.Common.VSPackages.CrmDeveloperHelper.Helpers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Model;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using Nav.Common.VSPackages.CrmDeveloperHelper.Repository;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;
using System.Collections.ObjectModel;
using System.Windows.Data;
using Microsoft.Xrm.Sdk.Query;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Views
{
    public partial class WindowOrganizationComparerSavedQuery : WindowBase
    {
        private readonly object sysObjectConnections = new object();

        private IWriteToOutput _iWriteToOutput;

        private string _filterEntity;

        private Dictionary<Guid, IOrganizationServiceExtented> _cacheService = new Dictionary<Guid, IOrganizationServiceExtented>();

        private CommonConfiguration _commonConfig;
        private ConnectionConfiguration _connectionConfig;

        private ObservableCollection<EntityViewItem> _itemsSource;

        private bool _controlsEnabled = true;

        private int _init = 0;

        public WindowOrganizationComparerSavedQuery(
            IWriteToOutput iWriteToOutput
            , CommonConfiguration commonConfig
            , ConnectionData connection1
            , ConnectionData connection2
            , string filterEntity
        )
        {
            _init++;

            InputLanguageManager.SetInputLanguage(this, CultureInfo.CreateSpecificCulture("en-US"));

            this._iWriteToOutput = iWriteToOutput;
            this._commonConfig = commonConfig;
            this._connectionConfig = connection1.ConnectionConfiguration;
            this._filterEntity = filterEntity;

            BindingOperations.EnableCollectionSynchronization(_connectionConfig.Connections, sysObjectConnections);

            InitializeComponent();

            tSDDBConnection1.Header = string.Format("Export from {0}", connection1.Name);
            tSDDBConnection2.Header = string.Format("Export from {0}", connection2.Name);

            this.Resources["ConnectionName1"] = string.Format("Create from {0}", connection1.Name);
            this.Resources["ConnectionName2"] = string.Format("Create from {0}", connection2.Name);

            if (string.IsNullOrEmpty(_filterEntity))
            {
                btnClearEntityFilter.IsEnabled = sepClearEntityFilter.IsEnabled = false;
                btnClearEntityFilter.Visibility = sepClearEntityFilter.Visibility = Visibility.Collapsed;
            }

            LoadFromConfig();

            txtBFilterEnitity.SelectionLength = 0;
            txtBFilterEnitity.SelectionStart = txtBFilterEnitity.Text.Length;

            txtBFilterEnitity.Focus();

            this._itemsSource = new ObservableCollection<EntityViewItem>();

            this.lstVwSavedQueries.ItemsSource = _itemsSource;

            cmBConnection1.ItemsSource = _connectionConfig.Connections;
            cmBConnection1.SelectedItem = connection1;

            cmBConnection2.ItemsSource = _connectionConfig.Connections;
            cmBConnection2.SelectedItem = connection2;

            _init--;

            ShowExistingSavedQueries();
        }

        private void LoadFromConfig()
        {
            cmBFileAction.DataContext = _commonConfig;
        }

        protected override void OnClosed(EventArgs e)
        {
            _commonConfig.Save();
            _connectionConfig.Save();

            BindingOperations.ClearAllBindings(cmBConnection1);
            cmBConnection1.Items.DetachFromSourceCollection();
            cmBConnection1.DataContext = null;
            cmBConnection1.ItemsSource = null;

            BindingOperations.ClearAllBindings(cmBConnection2);
            cmBConnection2.Items.DetachFromSourceCollection();
            cmBConnection2.DataContext = null;
            cmBConnection2.ItemsSource = null;

            base.OnClosed(e);
        }

        private async Task<IOrganizationServiceExtented> GetService1()
        {
            ConnectionData connectionData = null;

            cmBConnection1.Dispatcher.Invoke(() =>
            {
                connectionData = cmBConnection1.SelectedItem as ConnectionData;
            });

            if (connectionData != null)
            {
                if (!_cacheService.ContainsKey(connectionData.ConnectionId))
                {
                    _iWriteToOutput.WriteToOutput("Connection to CRM.");
                    _iWriteToOutput.WriteToOutput(connectionData.GetConnectionDescription());
                    var service = await QuickConnection.ConnectAsync(connectionData);
                    _iWriteToOutput.WriteToOutput("Current Service Endpoint: {0}", service.CurrentServiceEndpoint);

                    _cacheService[connectionData.ConnectionId] = service;
                }

                return _cacheService[connectionData.ConnectionId];
            }

            return null;
        }

        private async Task<IOrganizationServiceExtented> GetService2()
        {
            ConnectionData connectionData = null;

            cmBConnection2.Dispatcher.Invoke(() =>
            {
                connectionData = cmBConnection2.SelectedItem as ConnectionData;
            });

            if (connectionData != null)
            {
                if (!_cacheService.ContainsKey(connectionData.ConnectionId))
                {
                    _iWriteToOutput.WriteToOutput("Connection to CRM.");
                    _iWriteToOutput.WriteToOutput(connectionData.GetConnectionDescription());
                    var service = await QuickConnection.ConnectAsync(connectionData);
                    _iWriteToOutput.WriteToOutput("Current Service Endpoint: {0}", service.CurrentServiceEndpoint);

                    _cacheService[connectionData.ConnectionId] = service;
                }

                return _cacheService[connectionData.ConnectionId];
            }

            return null;
        }

        private async Task ShowExistingSavedQueries()
        {
            if (!_controlsEnabled)
            {
                return;
            }

            ToggleControls(false);

            UpdateStatus("Loading saved queries...");

            this._itemsSource.Clear();

            IEnumerable<LinkedEntities<SavedQuery>> list = Enumerable.Empty<LinkedEntities<SavedQuery>>();

            try
            {
                var service1 = await GetService1();
                var service2 = await GetService2();

                if (service1 != null && service2 != null)
                {
                    var columnsSet = new ColumnSet(SavedQuery.Schema.Attributes.name, SavedQuery.Schema.Attributes.returnedtypecode, SavedQuery.Schema.Attributes.querytype);

                    var temp = new List<LinkedEntities<SavedQuery>>();

                    if (service1.ConnectionData.ConnectionId != service2.ConnectionData.ConnectionId)
                    {
                        SavedQueryRepository repository1 = new SavedQueryRepository(service1);
                        SavedQueryRepository repository2 = new SavedQueryRepository(service2);

                        var task1 = repository1.GetListAsync(_filterEntity, columnsSet);
                        var task2 = repository2.GetListAsync(_filterEntity, columnsSet);

                        var list1 = await task1;
                        var list2 = await task2;

                        foreach (var query1 in list1)
                        {
                            var query2 = list2.FirstOrDefault(query => query.Id == query1.Id);

                            if (query2 == null)
                            {
                                continue;
                            }

                            temp.Add(new LinkedEntities<SavedQuery>(query1, query2));
                        }
                    }
                    else
                    {
                        SavedQueryRepository repository1 = new SavedQueryRepository(service1);

                        var task1 = repository1.GetListCustomableAsync();

                        var list1 = await task1;

                        foreach (var query1 in list1)
                        {
                            temp.Add(new LinkedEntities<SavedQuery>(query1, null));
                        }
                    }

                    list = temp;
                }
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);
            }

            var textName = string.Empty;

            txtBFilterEnitity.Dispatcher.Invoke(() =>
            {
                textName = txtBFilterEnitity.Text.Trim().ToLower();
            });

            list = FilterList(list, textName);

            this._iWriteToOutput.WriteToOutput("Found {0} saved query(ies)", list.Count());

            LoadEntities(list);

            UpdateStatus(string.Format("{0} saved query(ies) loaded.", list.Count()));

            ToggleControls(true);
        }

        private static IEnumerable<LinkedEntities<SavedQuery>> FilterList(IEnumerable<LinkedEntities<SavedQuery>> list, string textName)
        {
            if (!string.IsNullOrEmpty(textName))
            {
                textName = textName.ToLower();

                if (Guid.TryParse(textName, out Guid tempGuid))
                {
                    list = list.Where(ent =>
                        ent.Entity1?.Id == tempGuid
                        || ent.Entity2?.Id == tempGuid
                        || ent.Entity1?.SavedQueryIdUnique == tempGuid
                        || ent.Entity2?.SavedQueryIdUnique == tempGuid
                    );
                }
                else
                {
                    list = list.Where(ent =>
                    {
                        var type1 = ent.Entity1?.ReturnedTypeCode.ToLower() ?? string.Empty;
                        var name1 = ent.Entity1?.Name.ToLower() ?? string.Empty;

                        var type2 = ent.Entity2?.ReturnedTypeCode.ToLower() ?? string.Empty;
                        var name2 = ent.Entity2?.Name.ToLower() ?? string.Empty;

                        return type1.Contains(textName)
                            || name1.Contains(textName)
                            || type2.Contains(textName)
                            || name2.Contains(textName)
                            ;
                    });
                }
            }

            return list;
        }

        private class EntityViewItem
        {
            public string EntityName { get; private set; }

            public string QueryType { get; private set; }

            public string QueryName1 { get; private set; }

            public string QueryName2 { get; private set; }

            public LinkedEntities<SavedQuery> Link { get; private set; }

            public EntityViewItem(string entityName, string queryType, LinkedEntities<SavedQuery> link, string queryName1, string queryName2)
            {
                this.EntityName = entityName;
                this.QueryName1 = queryName1;
                this.QueryName2 = queryName2;
                this.QueryType = queryType;
                this.Link = link;
            }
        }

        private void LoadEntities(IEnumerable<LinkedEntities<SavedQuery>> results)
        {
            this.lstVwSavedQueries.Dispatcher.Invoke(() =>
            {
                foreach (var link in results
                      .OrderBy(ent => ent.Entity1.ReturnedTypeCode)
                      .ThenBy(ent => ent.Entity1.QueryType)
                      .ThenBy(ent => ent.Entity1.Name)
                      .ThenBy(ent => ent.Entity2?.Name)
                  )
                {
                    string queryTypeName = SavedQueryRepository.GetQueryTypeName(link.Entity1.QueryType.Value);

                    var item = new EntityViewItem(link.Entity1.ReturnedTypeCode, queryTypeName, link, link.Entity1.Name, link.Entity2?.Name);

                    this._itemsSource.Add(item);
                }

                if (this.lstVwSavedQueries.Items.Count == 1)
                {
                    this.lstVwSavedQueries.SelectedItem = this.lstVwSavedQueries.Items[0];
                }
            });
        }

        private void UpdateStatus(string msg)
        {
            this.statusBar.Dispatcher.Invoke(() =>
            {
                this.tSSLStatusMessage.Content = msg;
            });
        }

        private void ToggleControls(bool enabled)
        {
            this._controlsEnabled = enabled;

            ToggleControl(this.tSDDBShowDifference, enabled);
            ToggleControl(this.tSDDBConnection1, enabled);
            ToggleControl(this.tSDDBConnection2, enabled);

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

                    var item = (this.lstVwSavedQueries.SelectedItems[0] as EntityViewItem);

                    tSDDBShowDifference.IsEnabled = enabled && item.Link.Entity1 != null && item.Link.Entity2 != null;
                    tSDDBConnection1.IsEnabled = enabled && item.Link.Entity1 != null;
                    tSDDBConnection2.IsEnabled = enabled && item.Link.Entity2 != null;
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

        private EntityViewItem GetSelectedEntity()
        {
            EntityViewItem result = null;

            if (this.lstVwSavedQueries.SelectedItems.Count == 1
                && this.lstVwSavedQueries.SelectedItems[0] != null
                && this.lstVwSavedQueries.SelectedItems[0] is EntityViewItem
                )
            {
                result = (this.lstVwSavedQueries.SelectedItems[0] as EntityViewItem);
            }

            return result;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lstVwEntities_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var item = ((FrameworkElement)e.OriginalSource).DataContext as EntityViewItem;

                if (item != null)
                {
                    ExecuteAction(item.Link, false, PerformShowingDifferenceAllAsync);
                }
            }
        }

        private void lstVwEntities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsEnable();
        }

        private void ExecuteAction(LinkedEntities<SavedQuery> linked, bool showAllways, Func<LinkedEntities<SavedQuery>, bool, Task> action)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            if (linked.Entity1 == null || linked.Entity2 == null || linked.Entity1 == linked.Entity2)
            {
                return;
            }

            if (string.IsNullOrEmpty(_commonConfig.FolderForExport))
            {
                return;
            }

            if (!Directory.Exists(_commonConfig.FolderForExport))
            {
                return;
            }

            action(linked, showAllways);
        }

        private Task<string> CreateFileAsync(string connectionName, string entityName, string name, string fieldTitle, string xmlContent)
        {
            return Task.Run(() => CreateFile(connectionName, entityName, name, fieldTitle, xmlContent));
        }

        private string CreateFile(string connectionName, string entityName, string name, string fieldTitle, string xmlContent)
        {
            string fileName = EntityFileNameFormatter.GetSavedQueryFileName(connectionName, entityName, name, fieldTitle, "xml");
            string filePath = Path.Combine(_commonConfig.FolderForExport, FileOperations.RemoveWrongSymbols(fileName));

            if (!string.IsNullOrEmpty(xmlContent))
            {
                try
                {
                    if (ContentCoparerHelper.TryParseXml(xmlContent, out var doc))
                    {
                        xmlContent = doc.ToString();
                    }

                    File.WriteAllText(filePath, xmlContent, Encoding.UTF8);

                    this._iWriteToOutput.WriteToOutput("{0} System View (Saved Query) {1} {2} exported to {3}", connectionName, name, fieldTitle, filePath);
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

        private Task<string> CreateDescriptionFileAsync(string connectionName, string entityName, string name, string fieldName, string description)
        {
            return Task.Run(() => CreateDescriptionFile(connectionName, entityName, name, fieldName, description));
        }

        private string CreateDescriptionFile(string connectionName, string entityName, string name, string fieldName, string description)
        {
            string fileName = EntityFileNameFormatter.GetSavedQueryFileName(connectionName, entityName, name, fieldName, "txt");
            string filePath = Path.Combine(_commonConfig.FolderForExport, FileOperations.RemoveWrongSymbols(fileName));

            if (!string.IsNullOrEmpty(description))
            {
                try
                {
                    File.WriteAllText(filePath, description, Encoding.UTF8);

                    this._iWriteToOutput.WriteToOutput("{0} Saved Query Entity Description {1} {2} exported to {3}", connectionName, name, fieldName, filePath);
                }
                catch (Exception ex)
                {
                    this._iWriteToOutput.WriteErrorToOutput(ex);
                }
            }
            else
            {
                filePath = string.Empty;
                this._iWriteToOutput.WriteToOutput("{0} Saved Query Entity Description {1} {2} is empty.", connectionName, name, fieldName);
                this._iWriteToOutput.ActivateOutputWindow();
            }

            return filePath;
        }

        private void btnShowDifferenceAll_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null)
            {
                return;
            }

            ExecuteAction(link.Link, false, PerformShowingDifferenceAllAsync);
        }

        private async Task PerformShowingDifferenceAllAsync(LinkedEntities<SavedQuery> linked, bool showAllways)
        {
            await PerformShowingDifferenceEntityDescriptionAsync(linked, showAllways);

            await PerformShowingDifferenceSingleXmlAsync(linked, showAllways, SavedQuery.Schema.Attributes.fetchxml, "FetchXml");

            await PerformShowingDifferenceSingleXmlAsync(linked, showAllways, SavedQuery.Schema.Attributes.layoutxml, "LayoutXml", ContentCoparerHelper.RemoveLayoutObjectCode);

            await PerformShowingDifferenceSingleXmlAsync(linked, showAllways, SavedQuery.Schema.Attributes.columnsetxml, "ColumnSetXml");
        }

        private void mIShowDifferenceFetchXml_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null)
            {
                return;
            }

            ExecuteActionLinked(link.Link, true, SavedQuery.Schema.Attributes.fetchxml, "FetchXml", PerformShowingDifferenceSingleXmlAsync);
        }

        private void mIShowDifferenceLayoutXml_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null)
            {
                return;
            }

            ExecuteActionLinked(link.Link, true, SavedQuery.Schema.Attributes.layoutxml, "LayoutXml", PerformShowingDifferenceSingleXmlAsync, ContentCoparerHelper.RemoveLayoutObjectCode);
        }

        private void mIShowDifferenceColumnSetXml_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null)
            {
                return;
            }

            ExecuteActionLinked(link.Link, true, SavedQuery.Schema.Attributes.columnsetxml, "ColumnSetXml", PerformShowingDifferenceSingleXmlAsync);
        }

        private void ExecuteActionLinked(LinkedEntities<SavedQuery> linked, bool showAllways, string fieldName, string fieldTitle, Func<LinkedEntities<SavedQuery>, bool, string, string, Action<XElement>, Task> action, Action<XElement> actionXml = null)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            if (linked.Entity1 == null || linked.Entity2 == null || linked.Entity1 == linked.Entity2)
            {
                return;
            }

            if (string.IsNullOrEmpty(_commonConfig.FolderForExport))
            {
                return;
            }

            if (!Directory.Exists(_commonConfig.FolderForExport))
            {
                return;
            }

            action(linked, showAllways, fieldName, fieldTitle, actionXml);
        }

        private async Task PerformShowingDifferenceSingleXmlAsync(LinkedEntities<SavedQuery> linked, bool showAllways, string fieldName, string fieldTitle, Action<XElement> action = null)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            ToggleControls(false);

            var service1 = await GetService1();
            var service2 = await GetService2();

            if (service1 != null && service2 != null)
            {
                var repository1 = new SavedQueryRepository(service1);
                var repository2 = new SavedQueryRepository(service2);

                var savedQuery1 = await repository1.GetByIdAsync(linked.Entity1.Id, new ColumnSet(true));
                var savedQuery2 = await repository2.GetByIdAsync(linked.Entity2.Id, new ColumnSet(true));

                string xml1 = savedQuery1.GetAttributeValue<string>(fieldName);
                string xml2 = savedQuery2.GetAttributeValue<string>(fieldName);

                if (showAllways || !ContentCoparerHelper.CompareXML(xml1, xml2, false, action).IsEqual)
                {
                    string filePath1 = await CreateFileAsync(service1.ConnectionData.Name, savedQuery1.ReturnedTypeCode, savedQuery1.Name, fieldTitle, xml1);
                    string filePath2 = await CreateFileAsync(service2.ConnectionData.Name, savedQuery2.ReturnedTypeCode, savedQuery2.Name, fieldTitle, xml2);

                    if (File.Exists(filePath1) && File.Exists(filePath2))
                    {
                        this._iWriteToOutput.ProcessStartProgramComparer(this._commonConfig, filePath1, filePath2, Path.GetFileName(filePath1), Path.GetFileName(filePath2));
                    }
                    else
                    {
                        this._iWriteToOutput.PerformAction(filePath1, _commonConfig);

                        this._iWriteToOutput.PerformAction(filePath2, _commonConfig);
                    }
                }
            }

            ToggleControls(true);
        }

        private void mIExportSavedQuery1FetchXml_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null && link.Link.Entity1 != null)
            {
                return;
            }

            ExecuteActionEntity(link.Link.Entity1.Id, GetService1, SavedQuery.Schema.Attributes.fetchxml, "FetchXml", PerformExportXmlToFileAsync);
        }

        private void mIExportSavedQuery2FetchXml_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null && link.Link.Entity2 != null)
            {
                return;
            }

            ExecuteActionEntity(link.Link.Entity2.Id, GetService2, SavedQuery.Schema.Attributes.fetchxml, "FetchXml", PerformExportXmlToFileAsync);
        }

        private void mIExportSavedQuery1LayoutXml_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null && link.Link.Entity1 != null)
            {
                return;
            }

            ExecuteActionEntity(link.Link.Entity1.Id, GetService1, SavedQuery.Schema.Attributes.layoutxml, "LayoutXml", PerformExportXmlToFileAsync);
        }

        private void mIExportSavedQuery2LayoutXml_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null && link.Link.Entity2 != null)
            {
                return;
            }

            ExecuteActionEntity(link.Link.Entity2.Id, GetService2, SavedQuery.Schema.Attributes.layoutxml, "LayoutXml", PerformExportXmlToFileAsync);
        }

        private void mIExportSavedQuery1ColumnSetXml_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null && link.Link.Entity1 != null)
            {
                return;
            }

            ExecuteActionEntity(link.Link.Entity1.Id, GetService1, SavedQuery.Schema.Attributes.columnsetxml, "ColumnSetXml", PerformExportXmlToFileAsync);
        }

        private void mIExportSavedQuery2ColumnSetXml_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null && link.Link.Entity2 != null)
            {
                return;
            }

            ExecuteActionEntity(link.Link.Entity2.Id, GetService2, SavedQuery.Schema.Attributes.columnsetxml, "ColumnSetXml", PerformExportXmlToFileAsync);
        }

        private void ExecuteActionEntity(Guid idsavedquery, Func<Task<IOrganizationServiceExtented>> getService, string fieldName, string fieldTitle, Func<Guid, Func<Task<IOrganizationServiceExtented>>, string, string, Task> action)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            if (string.IsNullOrEmpty(_commonConfig.FolderForExport))
            {
                return;
            }

            if (!Directory.Exists(_commonConfig.FolderForExport))
            {
                return;
            }

            action(idsavedquery, getService, fieldName, fieldTitle);
        }

        private async Task PerformExportXmlToFileAsync(Guid idSavedQuery, Func<Task<IOrganizationServiceExtented>> getService, string fieldName, string fieldTitle)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            ToggleControls(false);

            var service = await getService();

            if (service != null)
            {
                var repository = new SavedQueryRepository(service);

                var savedQuery = await repository.GetByIdAsync(idSavedQuery, new ColumnSet(true));

                string xmlContent = savedQuery.GetAttributeValue<string>(fieldName);

                string filePath = await CreateFileAsync(service.ConnectionData.Name, savedQuery.ReturnedTypeCode, savedQuery.Name, fieldTitle, xmlContent);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);
            }

            ToggleControls(true);
        }

        private void mIShowDifferenceEntityDescription_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null)
            {
                return;
            }

            ExecuteAction(link.Link, true, PerformShowingDifferenceEntityDescriptionAsync);
        }

        private async Task PerformShowingDifferenceEntityDescriptionAsync(LinkedEntities<SavedQuery> linked, bool showAllways)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            ToggleControls(false);

            var service1 = await GetService1();
            var service2 = await GetService2();

            if (service1 != null && service2 != null)
            {
                var repository1 = new SavedQueryRepository(service1);
                var repository2 = new SavedQueryRepository(service2);

                var savedQuery1 = await repository1.GetByIdAsync(linked.Entity1.Id, new ColumnSet(true));
                var savedQuery2 = await repository2.GetByIdAsync(linked.Entity2.Id, new ColumnSet(true));

                var desc1 = await EntityDescriptionHandler.GetEntityDescriptionAsync(savedQuery1, EntityFileNameFormatter.SavedQueryIgnoreFields);
                var desc2 = await EntityDescriptionHandler.GetEntityDescriptionAsync(savedQuery2, EntityFileNameFormatter.SavedQueryIgnoreFields);

                if (showAllways || desc1 != desc2)
                {
                    string filePath1 = await CreateDescriptionFileAsync(service1.ConnectionData.Name, savedQuery1.ReturnedTypeCode, savedQuery1.Name, "EntityDescription", desc1);
                    string filePath2 = await CreateDescriptionFileAsync(service2.ConnectionData.Name, savedQuery2.ReturnedTypeCode, savedQuery2.Name, "EntityDescription", desc2);

                    if (File.Exists(filePath1) && File.Exists(filePath2))
                    {
                        this._iWriteToOutput.ProcessStartProgramComparer(this._commonConfig, filePath1, filePath2, Path.GetFileName(filePath1), Path.GetFileName(filePath2));
                    }
                    else
                    {
                        this._iWriteToOutput.PerformAction(filePath1, _commonConfig);

                        this._iWriteToOutput.PerformAction(filePath2, _commonConfig);
                    }
                }
            }

            ToggleControls(true);
        }

        private void ExecuteActionDescription(Guid idsavedquery, Func<Task<IOrganizationServiceExtented>> getService, Func<Guid, Func<Task<IOrganizationServiceExtented>>, Task> action)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            if (string.IsNullOrEmpty(_commonConfig.FolderForExport))
            {
                return;
            }

            if (!Directory.Exists(_commonConfig.FolderForExport))
            {
                return;
            }

            action(idsavedquery, getService);
        }

        private async Task PerformExportDescriptionToFileAsync(Guid idSavedQuery, Func<Task<IOrganizationServiceExtented>> getService)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            ToggleControls(false);

            var service = await getService();

            if (service != null)
            {
                var repository = new SavedQueryRepository(service);

                var savedQuery = await repository.GetByIdAsync(idSavedQuery, new ColumnSet(true));

                var description = await EntityDescriptionHandler.GetEntityDescriptionAsync(savedQuery, EntityFileNameFormatter.SavedQueryIgnoreFields, service.ConnectionData);

                string filePath = await CreateDescriptionFileAsync(service.ConnectionData.Name, savedQuery.ReturnedTypeCode, savedQuery.Name, "EntityDescription", description);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);
            }

            ToggleControls(true);
        }

        private void mIExportSavedQuery1EntityDescription_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null && link.Link.Entity1 != null)
            {
                return;
            }

            ExecuteActionDescription(link.Link.Entity1.Id, GetService1, PerformExportDescriptionToFileAsync);
        }

        private void mIExportSavedQuery2EntityDescription_Click(object sender, RoutedEventArgs e)
        {
            var link = GetSelectedEntity();

            if (link == null && link.Link.Entity2 != null)
            {
                return;
            }

            ExecuteActionDescription(link.Link.Entity2.Id, GetService2, PerformExportDescriptionToFileAsync);
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

        private void btnClearEntityFilter_Click(object sender, RoutedEventArgs e)
        {
            this._filterEntity = null;

            btnClearEntityFilter.IsEnabled = sepClearEntityFilter.IsEnabled = false;
            btnClearEntityFilter.Visibility = sepClearEntityFilter.Visibility = Visibility.Collapsed;

            ShowExistingSavedQueries();
        }

        private void cmBCurrentConnection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init > 0)
            {
                return;
            }

            this.Dispatcher.Invoke(() =>
            {
                this._itemsSource.Clear();

                ConnectionData connection1 = cmBConnection1.SelectedItem as ConnectionData;
                ConnectionData connection2 = cmBConnection2.SelectedItem as ConnectionData;

                if (connection1 != null && connection2 != null)
                {
                    tSDDBConnection1.Header = string.Format("Export from {0}", connection1.Name);
                    tSDDBConnection2.Header = string.Format("Export from {0}", connection2.Name);

                    this.Resources["ConnectionName1"] = string.Format("Create from {0}", connection1.Name);
                    this.Resources["ConnectionName2"] = string.Format("Create from {0}", connection2.Name);

                    UpdateButtonsEnable();

                    ShowExistingSavedQueries();
                }
            });
        }
        private async void btnOrganizationComparer_Click(object sender, RoutedEventArgs e)
        {
            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenOrganizationComparerWindow(this._iWriteToOutput, service.ConnectionData.ConnectionConfiguration, _commonConfig);
        }

        private async void btnCompareMetadataFile_Click(object sender, RoutedEventArgs e)
        {
            _commonConfig.Save();

            var service1 = await GetService1();
            var service2 = await GetService2();

            WindowHelper.OpenOrganizationComparerEntityMetadataWindow(this._iWriteToOutput, _commonConfig, service1.ConnectionData, service2.ConnectionData);
        }

        private async void btnCompareRibbon_Click(object sender, RoutedEventArgs e)
        {
            _commonConfig.Save();

            var service1 = await GetService1();
            var service2 = await GetService2();

            WindowHelper.OpenOrganizationComparerRibbonWindow(this._iWriteToOutput, _commonConfig, service1.ConnectionData, service2.ConnectionData);
        }

        private async void btnCompareGlobalOptionSets_Click(object sender, RoutedEventArgs e)
        {
            _commonConfig.Save();

            var service1 = await GetService1();
            var service2 = await GetService2();

            WindowHelper.OpenOrganizationComparerGlobalOptionSetsWindow(this._iWriteToOutput, _commonConfig, service1.ConnectionData, service2.ConnectionData);
        }

        private async void btnCompareSystemForms_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service1 = await GetService1();
            var service2 = await GetService2();

            WindowHelper.OpenOrganizationComparerSystemFormWindow(this._iWriteToOutput, _commonConfig, service1.ConnectionData, service2.ConnectionData, entity.EntityName);
        }

        private async void btnCompareSavedChart_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service1 = await GetService1();
            var service2 = await GetService2();

            WindowHelper.OpenOrganizationComparerSavedQueryVisualizationWindow(this._iWriteToOutput, _commonConfig, service1.ConnectionData, service2.ConnectionData, entity.EntityName);
        }

        private async void btnCompareWorkflows_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service1 = await GetService1();
            var service2 = await GetService2();

            WindowHelper.OpenOrganizationComparerWorkflowWindow(this._iWriteToOutput, _commonConfig, service1.ConnectionData, service2.ConnectionData, entity.EntityName);
        }

        private async void btnCreateMetadataFile1_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenEntityMetadataWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, null, null);
        }

        private async void btnExportRibbon1_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenEntityRibbonWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, null);
        }

        private async void btnGlobalOptionSets1_Click(object sender, RoutedEventArgs e)
        {
            var service = await GetService1();

            _commonConfig.Save();

            WindowHelper.OpenGlobalOptionSetsWindow(
                this._iWriteToOutput
                , service
                , _commonConfig
                , null
                , string.Empty
                , string.Empty
                );
        }

        private async void btnSystemForms1_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenSystemFormWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, string.Empty);
        }

        private async void btnSavedQuery1_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenSavedQueryWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, string.Empty);
        }

        private async void btnSavedChart1_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenSavedQueryVisualizationWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, string.Empty);
        }

        private async void btnWorkflows1_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenWorkflowWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, string.Empty);
        }

        private async void btnAttributesDependentComponent1_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenAttributesDependentComponentWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, null);
        }

        private async void btnPluginTree1_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenPluginTreeWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName);
        }

        private async void btnMessageTree1_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenSdkMessageTreeWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName);
        }

        private async void btnMessageRequestTree1_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService1();

            WindowHelper.OpenSdkMessageRequestTreeWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName);
        }

        private async void btnCreateMetadataFile2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService2();

            WindowHelper.OpenEntityMetadataWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, null, null);
        }

        private async void btnExportRibbon2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService2();

            WindowHelper.OpenEntityRibbonWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, null);
        }

        private async void btnGlobalOptionSets2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            var service = await GetService2();

            _commonConfig.Save();

            WindowHelper.OpenGlobalOptionSetsWindow(
                this._iWriteToOutput
                , service
                , _commonConfig
                , null
                , string.Empty
                , string.Empty
                );
        }

        private async void btnSystemForms2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService2();

            WindowHelper.OpenSystemFormWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, string.Empty);
        }

        private async void btnSavedQuery2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService2();

            WindowHelper.OpenSavedQueryWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, string.Empty);
        }

        private async void btnSavedChart2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService2();

            WindowHelper.OpenSavedQueryVisualizationWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, string.Empty);
        }

        private async void btnWorkflows2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService2();

            WindowHelper.OpenWorkflowWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, string.Empty);
        }

        private async void btnAttributesDependentComponent2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService2();

            WindowHelper.OpenAttributesDependentComponentWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName, null);
        }

        private async void btnPluginTree2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService2();

            WindowHelper.OpenPluginTreeWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName);
        }

        private async void btnMessageTree2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService2();

            WindowHelper.OpenSdkMessageTreeWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName);
        }

        private async void btnMessageRequestTree2_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            _commonConfig.Save();

            var service = await GetService2();

            WindowHelper.OpenSdkMessageRequestTreeWindow(this._iWriteToOutput, service, _commonConfig, entity?.EntityName);
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (!(sender is ContextMenu contextMenu))
            {
                return;
            }

            var linkedEntityMetadata = ((FrameworkElement)e.OriginalSource).DataContext as EntityViewItem;

            var items = contextMenu.Items.OfType<Control>();

            foreach (var menuContextDifference in items.Where(i => string.Equals(i.Uid, "menuContextDifference", StringComparison.InvariantCultureIgnoreCase)))
            {
                menuContextDifference.IsEnabled = false;
                menuContextDifference.Visibility = Visibility.Collapsed;

                if (linkedEntityMetadata != null
                     && linkedEntityMetadata.Link != null
                     && linkedEntityMetadata.Link.Entity1 != null
                     && linkedEntityMetadata.Link.Entity2 != null
                )
                {
                    menuContextDifference.IsEnabled = true;
                    menuContextDifference.Visibility = Visibility.Visible;
                }
            }

            foreach (var menuContextConnection2 in items.Where(i => string.Equals(i.Uid, "menuContextConnection2", StringComparison.InvariantCultureIgnoreCase)))
            {
                menuContextConnection2.IsEnabled = false;
                menuContextConnection2.Visibility = Visibility.Collapsed;

                if (linkedEntityMetadata != null
                    && linkedEntityMetadata.Link != null
                    && linkedEntityMetadata.Link.Entity2 != null
                )
                {
                    menuContextConnection2.IsEnabled = true;
                    menuContextConnection2.Visibility = Visibility.Visible;
                }
            }
        }
    }
}