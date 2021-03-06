using Nav.Common.VSPackages.CrmDeveloperHelper.Controllers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Entities;
using Nav.Common.VSPackages.CrmDeveloperHelper.Helpers;
using Nav.Common.VSPackages.CrmDeveloperHelper.Interfaces;
using Nav.Common.VSPackages.CrmDeveloperHelper.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Views
{
    public partial class WindowSolutionImage : WindowBase
    {
        private readonly object sysObjectConnections = new object();

        private IWriteToOutput _iWriteToOutput;

        private CommonConfiguration _commonConfig;
        private ConnectionConfiguration _connectionConfig;

        private Dictionary<Guid, IOrganizationServiceExtented> _connectionCache = new Dictionary<Guid, IOrganizationServiceExtented>();
        private Dictionary<Guid, SolutionComponentDescriptor> _descriptorCache = new Dictionary<Guid, SolutionComponentDescriptor>();

        private SolutionImage _solutionImage = null;

        private ObservableCollection<SolutionImageComponent> _itemsSource;

        private bool _controlsEnabled = true;

        private int _init = 0;

        public WindowSolutionImage(
            IWriteToOutput iWriteToOutput
            , CommonConfiguration commonConfig
            , ConnectionData connectionData
            , string filePath
        )
        {
            BeginLoadConfig();

            InitializeComponent();

            InputLanguageManager.SetInputLanguage(this, CultureInfo.CreateSpecificCulture("en-US"));

            this._iWriteToOutput = iWriteToOutput;
            this._commonConfig = commonConfig;
            this._connectionConfig = connectionData.ConnectionConfiguration;

            BindingOperations.EnableCollectionSynchronization(_connectionConfig.Connections, sysObjectConnections);

            LoadFromConfig();

            LoadConfiguration();

            txtBFilter.SelectionLength = 0;
            txtBFilter.SelectionStart = txtBFilter.Text.Length;

            txtBFilter.Focus();

            this._itemsSource = new ObservableCollection<SolutionImageComponent>();

            this.lstVwComponents.ItemsSource = _itemsSource;

            cmBCurrentConnection.ItemsSource = _connectionConfig.Connections;
            cmBCurrentConnection.SelectedItem = connectionData;

            EndLoadConfig();

            if (!string.IsNullOrEmpty(filePath))
            {
                LoadSolutionImage(filePath);
            }
        }

        private void LoadFromConfig()
        {
            cmBFileAction.DataContext = _commonConfig;

            txtBFolder.DataContext = _commonConfig;
        }

        private const string paramComponentType = "ComponentType";

        private void LoadConfiguration()
        {
            WindowSettings winConfig = this.GetWindowsSettings();

            {
                var categoryValue = winConfig.GetValueInt(paramComponentType);

                if (categoryValue != -1)
                {
                    var item = cmBComponentType.Items.OfType<ComponentType?>().FirstOrDefault(e => (int)e == categoryValue);
                    if (item.HasValue)
                    {
                        cmBComponentType.SelectedItem = item.Value;
                    }
                }
            }
        }

        protected override void SaveConfigurationInternal(WindowSettings winConfig)
        {
            base.SaveConfigurationInternal(winConfig);

            var categoryValue = -1;

            if (cmBComponentType.SelectedItem != null
                && cmBComponentType.SelectedItem is ComponentType selected
                )
            {
                categoryValue = (int)selected;
            }

            winConfig.DictInt[paramComponentType] = categoryValue;
        }

        protected override void OnClosed(EventArgs e)
        {
            _commonConfig.Save();
            _connectionConfig.Save();

            BindingOperations.ClearAllBindings(cmBCurrentConnection);

            cmBCurrentConnection.Items.DetachFromSourceCollection();

            cmBCurrentConnection.DataContext = null;
            cmBCurrentConnection.ItemsSource = null;

            base.OnClosed(e);
        }

        private void BeginLoadConfig()
        {
            ++_init;
        }

        private void EndLoadConfig()
        {
            --_init;
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
                    _iWriteToOutput.WriteToOutput(Properties.OutputStrings.ConnectingToCRM);
                    _iWriteToOutput.WriteToOutput(connectionData.GetConnectionDescription());
                    var service = await QuickConnection.ConnectAsync(connectionData);
                    _iWriteToOutput.WriteToOutput(Properties.OutputStrings.CurrentServiceEndpointFormat1, service.CurrentServiceEndpoint);

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

        private async Task LoadSolutionImage(string filePath)
        {
            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            this._itemsSource.Clear();

            txtBFilePath.Text = string.Empty;

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                return;
            }
            
            ToggleControls(false, Properties.WindowStatusStrings.LoadingSolutionImage);

            try
            {
                this._solutionImage = await SolutionImage.LoadAsync(filePath);
            }
            catch (Exception ex)
            {
                DTEHelper.WriteExceptionToOutput(ex);

                this._solutionImage = null;
            }

            txtBFilePath.Dispatcher.Invoke(() =>
            {
                if (this._solutionImage != null)
                {
                    txtBFilePath.Text = filePath;
                }
                else
                {
                    txtBFilePath.Text = string.Empty;
                }
            });

            if (this._solutionImage == null)
            {
                ToggleControls(true, Properties.WindowStatusStrings.LoadingSolutionImageFailed);
                return;
            }
            
            ToggleControls(true, Properties.WindowStatusStrings.LoadingSolutionImageCompleted);

            FilteringSolutionImageComponents();
        }

        private void FilteringSolutionImageComponents()
        {
            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            this._itemsSource.Clear();
            
            ToggleControls(false, Properties.WindowStatusStrings.FilteringSolutionImageComponents);

            IEnumerable<SolutionImageComponent> filter = null;

            if (this._solutionImage != null)
            {
                filter = this._solutionImage.Components;
            }
            else
            {
                filter = Enumerable.Empty<SolutionImageComponent>();
            }

            string textName = string.Empty;

            txtBFilter.Dispatcher.Invoke(() =>
            {
                textName = txtBFilter.Text.Trim().ToLower();
            });


            if (!string.IsNullOrEmpty(textName))
            {
                filter = filter.Where(s => !string.IsNullOrEmpty(s.Description) && s.Description.IndexOf(textName, StringComparison.InvariantCultureIgnoreCase) > -1);
            }

            int? category = null;

            cmBComponentType.Dispatcher.Invoke(() =>
            {
                if (cmBComponentType.SelectedItem != null
                    && cmBComponentType.SelectedItem is ComponentType comp
                    )
                {
                    category = (int)comp;
                }
            });

            if (category.HasValue)
            {
                filter = filter.Where(e => e.ComponentType == category);
            }

            foreach (var component in filter)
            {
                _itemsSource.Add(component);
            }
            
            ToggleControls(true, Properties.WindowStatusStrings.FilteringSolutionImageComponentsCompletedFormat1, filter.Count());
        }

        private void UpdateStatus(string format, params object[] args)
        {
            string message = format;

            if (args != null && args.Length > 0)
            {
                message = string.Format(format, args);
            }

            _iWriteToOutput.WriteToOutput(message);

            this.stBIStatus.Dispatcher.Invoke(() =>
            {
                this.stBIStatus.Content = message;
            });
        }

        private void ToggleControls(bool enabled, string statusFormat, params object[] args)
        {
            this._controlsEnabled = enabled;

            UpdateStatus(statusFormat, args);

            ToggleControl(this.tSBLoadSolutionImage, enabled);

            ToggleProgressBar(enabled);

            if (enabled)
            {
                UpdateButtonsEnable();
            }
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
            this.lstVwComponents.Dispatcher.Invoke(() =>
            {
                try
                {
                    bool enabled = this.lstVwComponents.SelectedItems.Count > 0;

                    UIElement[] list = { };

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
                FilteringSolutionImageComponents();
            }
        }

        private SolutionImageComponent GetSelectedEntity()
        {
            return this.lstVwComponents.SelectedItems.OfType<SolutionImageComponent>().Count() == 1
                ? this.lstVwComponents.SelectedItems.OfType<SolutionImageComponent>().SingleOrDefault() : null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lstVwEntities_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                //var item = ((FrameworkElement)e.OriginalSource).DataContext as SolutionImageComponent;

                //if (item != null)
                //{
                //    ExecuteAction(item, PerformExportAllXml);
                //}
            }
        }

        private void lstVwEntities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsEnable();
        }

        private async Task ExecuteAction(SolutionImageComponent entity, Func<string, SolutionImageComponent, Task> action)
        {
            if (_init > 0 || !_controlsEnabled)
            {
                return;
            }

            string folder = txtBFolder.Text.Trim();

            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            if (!Directory.Exists(folder))
            {
                return;
            }

            await action(folder, entity);
        }

        private void btnExportAll_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            //ExecuteAction(entity, PerformExportAllXml);
        }

        //private async Task PerformExportAllXml(string folder, SolutionImageComponent pluginType)
        //{

        //}

        private void tSBLoadSolutionImage_Click(object sender, RoutedEventArgs e)
        {
            string selectedPath = string.Empty;
            var t = new Thread(() =>
            {
                try
                {
                    var openFileDialog1 = new Microsoft.Win32.OpenFileDialog
                    {
                        Filter = "SolutionImage (.xml)|*.xml",
                        FilterIndex = 1,
                        RestoreDirectory = true
                    };

                    if (openFileDialog1.ShowDialog().GetValueOrDefault())
                    {
                        selectedPath = openFileDialog1.FileName;
                    }
                }
                catch (Exception ex)
                {
                    DTEHelper.WriteExceptionToOutput(ex);
                }
            });

            t.SetApartmentState(ApartmentState.STA);
            t.Start();

            t.Join();

            if (!string.IsNullOrEmpty(selectedPath))
            {
                LoadSolutionImage(selectedPath);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                e.Handled = true;

                FilteringSolutionImageComponents();
            }

            base.OnKeyDown(e);
        }

        private void cmBComponentType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FilteringSolutionImageComponents();
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

            FillLastSolutionItems(connectionData, items, true, AddIntoSolutionLast_Click, "contMnAddIntoSolutionLast");
        }

        private async void mIOpenDependentComponentsInWeb_Click(object sender, RoutedEventArgs e)
        {
            var entity = GetSelectedEntity();

            if (entity == null)
            {
                return;
            }

            var service = await GetService();
            var descriptor = await GetDescriptor();

            var solutionComponents = await descriptor.GetSolutionComponentsListAsync(new[] { entity });

            if (!solutionComponents.Any())
            {
                _iWriteToOutput.WriteToOutput(Properties.OutputStrings.SolutionComponentNotFoundInConnectionFormat1, service.ConnectionData.Name);
                _iWriteToOutput.ActivateOutputWindow();
                return;
            }
            
            foreach (var item in solutionComponents)
            {
                service.ConnectionData.OpenSolutionComponentDependentComponentsInWeb((ComponentType)item.ComponentType.Value, item.ObjectId.Value);
            }
        }

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

            var solutionComponents = await descriptor.GetSolutionComponentsListAsync(new[] { entity });

            if (!solutionComponents.Any())
            {
                _iWriteToOutput.WriteToOutput(Properties.OutputStrings.SolutionComponentNotFoundInConnectionFormat1, service.ConnectionData.Name);
                _iWriteToOutput.ActivateOutputWindow();
                return;
            }

            foreach (var item in solutionComponents)
            {
                WindowHelper.OpenSolutionComponentDependenciesWindow(
                    _iWriteToOutput
                    , service
                    , descriptor
                    , _commonConfig
                    , item.ComponentType.Value
                    , item.ObjectId.Value
                    , null);
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
            var descriptor = await GetDescriptor();

            var solutionComponents = await descriptor.GetSolutionComponentsListAsync(new[] { entity });

            if (!solutionComponents.Any())
            {
                _iWriteToOutput.WriteToOutput(Properties.OutputStrings.SolutionComponentNotFoundInConnectionFormat1, service.ConnectionData.Name);
                _iWriteToOutput.ActivateOutputWindow();
                return;
            }

            foreach (var item in solutionComponents)
            {
                if (SolutionComponent.IsDefinedComponentType(item.ComponentType.Value))
                {
                    service.UrlGenerator.OpenSolutionComponentInWeb((ComponentType)item.ComponentType.Value, item.ObjectId.Value);
                }
            }
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
            var descriptor = await GetDescriptor();

            var solutionComponents = await descriptor.GetSolutionComponentsListAsync(new[] { entity });

            if (!solutionComponents.Any())
            {
                _iWriteToOutput.WriteToOutput(Properties.OutputStrings.SolutionComponentNotFoundInConnectionFormat1, service.ConnectionData.Name);
                _iWriteToOutput.ActivateOutputWindow();
                return;
            }

            foreach (var item in solutionComponents)
            {
                WindowHelper.OpenExplorerSolutionWindow(
                    _iWriteToOutput
                    , service
                    , _commonConfig
                    , item.ComponentType.Value
                    , item.ObjectId.Value
                    , null
                );
            }
        }

        private void AddIntoSolution_Click(object sender, RoutedEventArgs e)
        {
            AddIntoSolutionAsync(true, null);
        }

        private void AddIntoSolutionLast_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
              && menuItem.Tag != null
              && menuItem.Tag is string solutionUniqueName
              )
            {
                AddIntoSolutionAsync(false, solutionUniqueName);
            }
        }

        private async Task AddIntoSolutionAsync(bool withSelect, string solutionUniqueName)
        {
            var solutionImageComponents = lstVwComponents.SelectedItems.OfType<SolutionImageComponent>().ToList();

            if (!solutionImageComponents.Any())
            {
                return;
            }

            var service = await GetService();
            var descriptor = await GetDescriptor();

            var solutionComponents = await descriptor.GetSolutionComponentsListAsync(solutionImageComponents);

            if (!solutionComponents.Any())
            {
                _iWriteToOutput.WriteToOutput(Properties.OutputStrings.SolutionComponentNotFoundInConnectionFormat1, service.ConnectionData.Name);
                _iWriteToOutput.ActivateOutputWindow();
                return;
            }

            _commonConfig.Save();

            try
            {
                this._iWriteToOutput.ActivateOutputWindow();

                await SolutionController.AddSolutionComponentsCollectionIntoSolution(_iWriteToOutput, service, descriptor, _commonConfig, solutionUniqueName, solutionComponents, withSelect);
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);
            }
        }
    }
}