﻿using Nav.Common.VSPackages.CrmDeveloperHelper.Entities;
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

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Views
{
    public partial class WindowExplorerSolution : WindowBase
    {
        private readonly object sysObjectConnections = new object();

        private IWriteToOutput _iWriteToOutput;

        private CommonConfiguration _commonConfig;
        private ConnectionConfiguration _connectionConfig;

        private Guid? _objectId;
        private int? _componentType;

        private bool _controlsEnabled = true;

        private ObservableCollection<EntityViewItem> _itemsSource;

        private Dictionary<Guid, IOrganizationServiceExtented> _connectionCache = new Dictionary<Guid, IOrganizationServiceExtented>();
        private Dictionary<Guid, SolutionComponentDescriptor> _descriptorCache = new Dictionary<Guid, SolutionComponentDescriptor>();

        private Dictionary<Guid, object> _syncCacheObjects = new Dictionary<Guid, object>();

        private int _init = 0;

        public WindowExplorerSolution(
            IWriteToOutput iWriteToOutput
            , IOrganizationServiceExtented service
            , CommonConfiguration commonConfig
            , int? componentType
            , Guid? objectId
            )
        {
            _init++;

            InputLanguageManager.SetInputLanguage(this, CultureInfo.CreateSpecificCulture("en-US"));

            this._iWriteToOutput = iWriteToOutput;
            this._commonConfig = commonConfig;
            this._connectionConfig = service.ConnectionData.ConnectionConfiguration;
            this._objectId = objectId;
            this._componentType = componentType;

            var descriptor = new SolutionComponentDescriptor(service, true);

            _connectionCache[service.ConnectionData.ConnectionId] = service;
            _descriptorCache[service.ConnectionData.ConnectionId] = descriptor;
            _syncCacheObjects.Add(service.ConnectionData.ConnectionId, new object());

            BindingOperations.EnableCollectionSynchronization(_connectionConfig.Connections, sysObjectConnections);
            BindingOperations.EnableCollectionSynchronization(service.ConnectionData.LastSelectedSolutionsUniqueName, _syncCacheObjects[service.ConnectionData.ConnectionId]);

            InitializeComponent();

            LoadFromConfig();

            sepClearSolutionComponentFilter.IsEnabled = btnClearSolutionComponentFilter.IsEnabled = _objectId.HasValue && _componentType.HasValue;
            sepClearSolutionComponentFilter.Visibility = btnClearSolutionComponentFilter.Visibility = (_objectId.HasValue && _componentType.HasValue) ? Visibility.Visible : Visibility.Collapsed;

            this._itemsSource = new ObservableCollection<EntityViewItem>();

            this.lstVwSolutions.ItemsSource = _itemsSource;

            cmBCurrentConnection.ItemsSource = _connectionConfig.Connections;
            cmBCurrentConnection.SelectedItem = service.ConnectionData;

            service.ConnectionData.LastSelectedSolutionsUniqueName.CollectionChanged -= LastSelectedSolutionsUniqueName_CollectionChanged;
            service.ConnectionData.LastSelectedSolutionsUniqueName.CollectionChanged += LastSelectedSolutionsUniqueName_CollectionChanged;

            _init--;

            cmBFilter.Focus();

            if (service != null)
            {
                ShowExistingSolutions();
            }
        }

        private void LastSelectedSolutionsUniqueName_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            this.Dispatcher.Invoke(FillCopyComponentsToLastSelectedSolutions);
        }

        private void LoadFromConfig()
        {
            txtBFolder.DataContext = _commonConfig;

            cmBFileAction.DataContext = _commonConfig;

            cmBGroupBy.DataContext = _commonConfig;

            cmBFilter.DataContext = cmBCurrentConnection;

            cmBFilter.Text = (cmBCurrentConnection.SelectedItem as ConnectionData)?.ExplorerSolutionFilter;
        }

        protected override void OnClosed(EventArgs e)
        {
            _commonConfig.Save();
            _connectionConfig.Save();

            cmBCurrentConnection.DataContext = null;
            cmBFilter.DataContext = null;

            BindingOperations.ClearAllBindings(cmBCurrentConnection);
            BindingOperations.ClearAllBindings(cmBFilter);

            cmBFilter.Items.DetachFromSourceCollection();
            cmBCurrentConnection.Items.DetachFromSourceCollection();

            cmBFilter.ItemsSource = null;
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

        private async Task ShowExistingSolutions()
        {
            if (!_controlsEnabled)
            {
                return;
            }

            ToggleControls(false, "Loading solutions...");

            this._itemsSource.Clear();

            string textName = string.Empty;

            cmBFilter.Dispatcher.Invoke(() =>
            {
                textName = cmBFilter.Text?.Trim().ToLower();
            });

            IEnumerable<Solution> list = Enumerable.Empty<Solution>();

            try
            {
                var service = await GetService();

                if (service != null)
                {
                    SolutionRepository repository = new SolutionRepository(service);

                    list = await repository.GetSolutionsAllAsync(textName, _componentType, _objectId);
                }
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);
            }

            this._iWriteToOutput.WriteToOutput("Found {0} solutions.", list.Count());

            LoadSolutions(list);

            ToggleControls(true, "{0} solutions loaded.", list.Count());
        }

        private class EntityViewItem
        {
            public Solution Solution { get; private set; }

            public string SolutionName => Solution.UniqueName;

            public string DisplayName => Solution.FriendlyName;

            public string SolutionType => Solution.FormattedValues[Solution.Schema.Attributes.ismanaged];

            public string Visible => Solution.FormattedValues[Solution.Schema.Attributes.isvisible];

            public DateTime? InstalledOn => Solution.InstalledOn?.ToLocalTime();

            public string PublisherName => Solution.PublisherId?.Name;

            public string Prefix => Solution.PublisherCustomizationPrefix;

            public EntityViewItem(Solution Solution)
            {
                this.Solution = Solution;
            }
        }

        private void LoadSolutions(IEnumerable<Solution> results)
        {
            this.lstVwSolutions.Dispatcher.Invoke(() =>
            {
                foreach (var entity in results.OrderByDescending(ent => ent.InstalledOn).ThenBy(ent => ent.UniqueName))
                {
                    var item = new EntityViewItem(entity);

                    this._itemsSource.Add(item);
                }

                if (this.lstVwSolutions.Items.Count == 1)
                {
                    this.lstVwSolutions.SelectedItem = this.lstVwSolutions.Items[0];
                }
            });
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
            this.lstVwSolutions.Dispatcher.Invoke(() =>
            {
                try
                {
                    {
                        bool enabled = this._controlsEnabled;

                        menuAnalyzeSolutions.IsEnabled = tSDDBAnalyzeSolutions.IsEnabled = enabled;

                        tSDDBAnalyzeSolutions.Items.Clear();

                        if (enabled)
                        {
                            var list1 = lstVwSolutions.SelectedItems
                                .OfType<EntityViewItem>()
                                .Select(e => e.Solution)
                                .OrderBy(e => e.UniqueName)
                                .ToList();

                            if (list1.Count > 1)
                            {
                                foreach (var solution1 in list1)
                                {
                                    if (tSDDBAnalyzeSolutions.Items.Count > 0)
                                    {
                                        tSDDBAnalyzeSolutions.Items.Add(new Separator());
                                    }

                                    MenuItem menuItemSolutuion1 = new MenuItem()
                                    {
                                        Header = string.Format("Solution {0}", solution1.UniqueNameEscapeUnderscore),
                                    };

                                    tSDDBAnalyzeSolutions.Items.Add(menuItemSolutuion1);

                                    var list2 = list1.Where(en => !string.Equals(en.UniqueName, solution1.UniqueName, StringComparison.InvariantCultureIgnoreCase));

                                    foreach (var solution2 in list2)
                                    {
                                        if (menuItemSolutuion1.Items.Count > 0)
                                        {
                                            menuItemSolutuion1.Items.Add(new Separator());
                                        }

                                        MenuItem menuItemSolutuion2 = new MenuItem()
                                        {
                                            Header = string.Format("Solution {0}", solution2.UniqueNameEscapeUnderscore),
                                        };

                                        menuItemSolutuion1.Items.Add(menuItemSolutuion2);

                                        MenuItem mIAnalyzeSolutions = new MenuItem()
                                        {
                                            Header = string.Format("Analyze Solutions {0} and {1}", solution1.UniqueNameEscapeUnderscore, solution2.UniqueNameEscapeUnderscore),
                                            Tag = Tuple.Create(solution1, solution2),
                                        };
                                        mIAnalyzeSolutions.Click += mIAnalyzeSolutions_Click;

                                        MenuItem mIShowUniqueComponentsIn1 = new MenuItem()
                                        {
                                            Header = string.Format("Show Unique Components in {0} compared to {1}", solution1.UniqueNameEscapeUnderscore, solution2.UniqueNameEscapeUnderscore),
                                            Tag = Tuple.Create(solution1, solution2),
                                        };
                                        mIShowUniqueComponentsIn1.Click += mIShowUniqueComponentsInSolution_Click;

                                        MenuItem mIShowUniqueComponentsIn2 = new MenuItem()
                                        {
                                            Header = string.Format("Show Unique Components in {0} compared to {1}", solution2.UniqueNameEscapeUnderscore, solution1.UniqueNameEscapeUnderscore),
                                            Tag = Tuple.Create(solution2, solution1),
                                        };
                                        mIShowUniqueComponentsIn2.Click += mIShowUniqueComponentsInSolution_Click;

                                        menuItemSolutuion2.Items.Add(mIAnalyzeSolutions);
                                        menuItemSolutuion2.Items.Add(new Separator());
                                        menuItemSolutuion2.Items.Add(mIShowUniqueComponentsIn1);
                                        menuItemSolutuion2.Items.Add(new Separator());
                                        menuItemSolutuion2.Items.Add(mIShowUniqueComponentsIn2);
                                    }
                                }
                            }
                            else
                            {
                                menuAnalyzeSolutions.IsEnabled = tSDDBAnalyzeSolutions.IsEnabled = false;
                            }
                        }
                    }

                    {
                        bool enabled = this._controlsEnabled;

                        menuShow.IsEnabled = tSDDBShow.IsEnabled = enabled;

                        tSDDBShow.Items.Clear();

                        if (enabled)
                        {
                            var list = lstVwSolutions.SelectedItems
                                .OfType<EntityViewItem>()
                                .Select(e => e.Solution)
                                .OrderBy(e => e.UniqueName)
                                .ToList();

                            if (list.Any())
                            {
                                if (list.Count == 1)
                                {
                                    var solution = list.First();

                                    FillSolutionButtons(solution, tSDDBShow.Items);
                                }
                                else
                                {
                                    foreach (var solution in list)
                                    {
                                        if (tSDDBShow.Items.Count > 0)
                                        {
                                            tSDDBShow.Items.Add(new Separator());
                                        }

                                        MenuItem menuItem = new MenuItem()
                                        {
                                            Header = string.Format("Solution {0}", solution.UniqueNameEscapeUnderscore),
                                        };

                                        tSDDBShow.Items.Add(menuItem);

                                        FillSolutionButtons(solution, menuItem.Items);
                                    }
                                }
                            }
                            else
                            {
                                menuShow.IsEnabled = tSDDBShow.IsEnabled = false;
                            }
                        }
                    }

                    {
                        bool enabled = this._controlsEnabled;

                        tSDDBCopyComponents.IsEnabled = enabled;

                        tSDDBCopyComponents.Items.Clear();

                        if (enabled)
                        {
                            var listFrom = lstVwSolutions.SelectedItems
                                .OfType<EntityViewItem>()
                                .Where(e => e.Solution != null)
                                .Select(e => e.Solution)
                                .OrderBy(e => e.UniqueName)
                                .ToList();

                            var listTo = listFrom.Where(e => e.IsManaged.GetValueOrDefault() == false).OrderBy(e => e.UniqueName).ToList();

                            var total = listTo.Count * (listFrom.Count - 1);

                            if (total > 0)
                            {
                                FillCopyComponentsMenuItems(tSDDBCopyComponents, listFrom, listTo);
                            }
                        }

                        tSDDBCopyComponents.IsEnabled = tSDDBCopyComponents.Items.Count > 0;

                        menuItemCopyComponents.IsEnabled = tSDDBCopyComponents.Items.Count > 0 || tSDDBCopyComponentsLastSolution.Items.Count > 0;
                    }

                    {
                        bool enabled = this._controlsEnabled;

                        tSDDBClearUnmanagedSolution.IsEnabled = enabled;

                        tSDDBClearUnmanagedSolution.Items.Clear();

                        if (enabled)
                        {
                            var list = lstVwSolutions.SelectedItems
                                .OfType<EntityViewItem>()
                                .Where(e => e.Solution.IsManaged.GetValueOrDefault() == false)
                                .Select(e => e.Solution)
                                .OrderBy(e => e.UniqueName)
                                .ToList();

                            if (list.Any())
                            {
                                foreach (var solution in list)
                                {
                                    if (tSDDBClearUnmanagedSolution.Items.Count > 0)
                                    {
                                        tSDDBClearUnmanagedSolution.Items.Add(new Separator());
                                    }

                                    MenuItem menuItem = new MenuItem()
                                    {
                                        Header = string.Format("Clear solution {0}", solution.UniqueNameEscapeUnderscore),
                                        Tag = solution,
                                    };

                                    menuItem.Click += mIClearUnmanagedSolution_Click;

                                    tSDDBClearUnmanagedSolution.Items.Add(menuItem);
                                }
                            }
                        }

                        tSDDBClearUnmanagedSolution.IsEnabled = tSDDBClearUnmanagedSolution.Items.Count > 0;
                    }

                    FillCopyComponentsToLastSelectedSolutions();
                }
                catch (Exception ex)
                {
                    this._iWriteToOutput.WriteErrorToOutput(ex);
                }
            });
        }

        private void FillCopyComponentsToLastSelectedSolutions()
        {
            bool enabled = this._controlsEnabled;

            tSDDBCopyComponentsLastSolution.IsEnabled = enabled;

            tSDDBCopyComponentsLastSolution.Items.Clear();

            if (enabled)
            {
                var connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

                if (connectionData != null
                    && connectionData.LastSelectedSolutionsUniqueName != null
                    )
                {
                    var listSolutionNames = connectionData.LastSelectedSolutionsUniqueName.ToList();

                    var listFrom = lstVwSolutions.SelectedItems
                            .OfType<EntityViewItem>()
                            .Where(e => e.Solution != null)
                            .Select(e => e.Solution)
                            .OrderBy(e => e.UniqueName)
                            .ToList();

                    if (listSolutionNames.Any() && listFrom.Any())
                    {
                        var serviceTask = GetService();

                        serviceTask.Wait();

                        var service = serviceTask.Result;

                        SolutionRepository repository = new SolutionRepository(service);

                        var solutionsTask = repository.GetSolutionsVisibleUnmanagedAsync(listSolutionNames);

                        solutionsTask.Wait();

                        var lastSolutions = solutionsTask.Result.ToDictionary(s => s.UniqueName, StringComparer.InvariantCultureIgnoreCase);

                        var listTo = listSolutionNames.Where(s => lastSolutions.ContainsKey(s)).Select(s => lastSolutions[s]).ToList();

                        FillCopyComponentsMenuItems(tSDDBCopyComponentsLastSolution, listFrom, listTo);
                    }
                }
            }

            tSDDBCopyComponentsLastSolution.IsEnabled = tSDDBCopyComponentsLastSolution.Items.Count > 0;

            menuItemCopyComponents.IsEnabled = tSDDBCopyComponents.Items.Count > 0 || tSDDBCopyComponentsLastSolution.Items.Count > 0;
        }

        private void FillCopyComponentsMenuItems(MenuItem parentMenuItem, List<Solution> listFrom, List<Solution> listTo)
        {
            foreach (var solutionTo in listTo)
            {
                var listFromFiltered = listFrom.Where(en => !string.Equals(en.UniqueName, solutionTo.UniqueName, StringComparison.InvariantCultureIgnoreCase));

                if (!listFromFiltered.Any())
                {
                    continue;
                }

                MenuItem menuItemSolution = new MenuItem()
                {
                    Header = string.Format("Solution {0}", solutionTo.UniqueNameEscapeUnderscore),
                };

                if (parentMenuItem.Items.Count > 0)
                {
                    parentMenuItem.Items.Add(new Separator());
                }

                parentMenuItem.Items.Add(menuItemSolution);

                if (listFromFiltered.Count() > 1)
                {
                    MenuItem menuItem = new MenuItem()
                    {
                        Header = string.Format("Copy Components from All Selected Solutions to {0}", solutionTo.UniqueNameEscapeUnderscore),
                        Tag = Tuple.Create(listFromFiltered.ToArray(), solutionTo),
                    };
                    menuItem.Click += mICopyComponentsFromSolutionCollectionToSolution_Click;

                    menuItemSolution.Items.Add(menuItem);
                }

                foreach (var solutionFrom in listFromFiltered)
                {
                    MenuItem menuItem = new MenuItem()
                    {
                        Header = string.Format("Copy Components from {0} to {1}", solutionFrom.UniqueNameEscapeUnderscore, solutionTo.UniqueNameEscapeUnderscore),
                        Tag = Tuple.Create(solutionFrom, solutionTo),
                    };
                    menuItem.Click += mICopyComponentsFromSolution1ToSolution2_Click;

                    if (menuItemSolution.Items.Count > 0)
                    {
                        menuItemSolution.Items.Add(new Separator());
                    }

                    menuItemSolution.Items.Add(menuItem);
                }
            }
        }

        private void FillSolutionButtons(Solution solution, ItemCollection itemCollection)
        {
            MenuItem mIOpenSolutionInWeb = new MenuItem()
            {
                Header = string.Format("Open in Web Solution {0}", solution.UniqueNameEscapeUnderscore),
                Tag = solution,
            };
            mIOpenSolutionInWeb.Click += mIOpenSolutionInWeb_Click;

            MenuItem mIOpenComponentsInWindow = new MenuItem()
            {
                Header = string.Format("Open Components in Window for {0}", solution.UniqueNameEscapeUnderscore),
                Tag = solution,
            };
            mIOpenComponentsInWindow.Click += mIOpenComponentsInWindow_Click;

            MenuItem mIUsedEntitiesInWorkflows = new MenuItem()
            {
                Header = string.Format("Used Entities in Workflows in {0}", solution.UniqueNameEscapeUnderscore),
                Tag = solution,
            };
            mIUsedEntitiesInWorkflows.Click += mIUsedEntitiesInWorkflows_Click;

            MenuItem mIUsedNotExistsEntitiesInWorkflows = new MenuItem()
            {
                Header = string.Format("Used Not Exists Entities in Workflows in {0}", solution.UniqueNameEscapeUnderscore),
                Tag = solution,
            };
            mIUsedNotExistsEntitiesInWorkflows.Click += mIUsedNotExistsEntitiesInWorkflows_Click;

            MenuItem mICreateSolutionImageIn = new MenuItem()
            {
                Header = string.Format("Create Solution Image in {0}", solution.UniqueNameEscapeUnderscore),
                Tag = solution,
            };
            mICreateSolutionImageIn.Click += mICreateSolutionImageIn_Click; ;

            MenuItem mIComponentsIn = new MenuItem()
            {
                Header = string.Format("Components in {0}", solution.UniqueNameEscapeUnderscore),
                Tag = solution,
            };
            mIComponentsIn.Click += mIComponentsIn_Click;

            MenuItem mIMissingComponentsIn = new MenuItem()
            {
                Header = string.Format("Missing Components for {0}", solution.UniqueNameEscapeUnderscore),
                Tag = solution,
            };
            mIMissingComponentsIn.Click += mIMissingComponentsIn_Click;

            MenuItem mIUninstallComponentsIn = new MenuItem()
            {
                Header = string.Format("Uninstall Components for {0}", solution.UniqueNameEscapeUnderscore),
                Tag = solution,
            };
            mIUninstallComponentsIn.Click += mIUninstallComponentsIn_Click;



            itemCollection.Add(mIOpenComponentsInWindow);
            itemCollection.Add(new Separator());
            itemCollection.Add(mIOpenSolutionInWeb);

            if (solution.IsManaged.GetValueOrDefault() == false)
            {
                MenuItem mISelectSolutionAsLast = new MenuItem()
                {
                    Header = string.Format("Select Solution {0} as Last Selected", solution.UniqueNameEscapeUnderscore),
                    Tag = solution,
                };
                mISelectSolutionAsLast.Click += miSelectSolutionAsLast_Click;

                itemCollection.Add(new Separator());
                itemCollection.Add(mISelectSolutionAsLast);
            }

            itemCollection.Add(new Separator());
            itemCollection.Add(mIUsedEntitiesInWorkflows);
            itemCollection.Add(mIUsedNotExistsEntitiesInWorkflows);
            itemCollection.Add(new Separator());
            itemCollection.Add(mICreateSolutionImageIn);
            itemCollection.Add(new Separator());
            itemCollection.Add(mIComponentsIn);
            itemCollection.Add(new Separator());
            itemCollection.Add(mIMissingComponentsIn);
            itemCollection.Add(mIUninstallComponentsIn);
        }

        private void miSelectSolutionAsLast_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
                && menuItem.Tag is Solution solution
                && solution.IsManaged.GetValueOrDefault() == false
                )
            {
                cmBCurrentConnection.Dispatcher.Invoke(() =>
                {
                    ConnectionData connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

                    if (connectionData != null)
                    {
                        var text = cmBFilter.Text;

                        connectionData.AddLastSelectedSolution(solution.UniqueName);

                        _iWriteToOutput.WriteToOutputSolutionUri(connectionData.ConnectionId, solution.UniqueName, connectionData.GetSolutionUrl(solution.Id));

                        cmBFilter.Text = text;
                    }
                });
            }
        }

        private void cmBFilterEnitity_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ShowExistingSolutions();
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void lstVwEntities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsEnable();
        }

        private void ExecuteActionOnSingleSolution(Solution solution, Func<string, Solution, Task> action)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            if (solution == null)
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

            try
            {
                action(folder, solution);
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);
            }
        }

        private void mIComponentsIn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
                && menuItem.Tag is Solution solution
                )
            {
                ExecuteActionOnSingleSolution(solution, PerformCreateFileWithSolutionComponents);
            }
        }

        private void mICreateSolutionImageIn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
                && menuItem.Tag is Solution solution
                )
            {
                ExecuteActionOnSingleSolution(solution, PerformCreateSolutionImage);
            }
        }

        private void mIOpenComponentsInWindow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
                && menuItem.Tag is Solution solution
                )
            {
                ExecuteActionOnSingleSolution(solution, PerformOpenSolutionComponentsInWindow);
            }
        }

        private void mIMissingComponentsIn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
               && menuItem.Tag is Solution solution
               )
            {
                ExecuteActionOnSingleSolution(solution, PerformShowingMissingDependencies);
            }
        }

        private void mIUninstallComponentsIn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
               && menuItem.Tag is Solution solution
               )
            {
                ExecuteActionOnSingleSolution(solution, PerformShowingDependenciesForUninstall);
            }
        }

        private async Task PerformCreateSolutionImage(string folder, Solution solution)
        {
            try
            {
                ToggleControls(false, "Creating file with Solution Image...");

                var service = await GetService();
                var descriptor = await GetDescriptor();

                SolutionDescriptor solutionDescriptor = new SolutionDescriptor(_iWriteToOutput, service, descriptor);

                string fileName = EntityFileNameFormatter.GetSolutionFileName(
                    service.ConnectionData.Name
                    , solution.UniqueName
                    , "Components"
                    , "xml"
                );

                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                await solutionDescriptor.CreateFileWithSolutionImageAsync(filePath, solution.Id);

                this._iWriteToOutput.WriteToOutput("Solution Image was export into file '{0}'", filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);

                ToggleControls(true, "Creating file with Solution Image completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Creating file with Solution Image failed.");
            }
        }

        private async Task PerformCreateFileWithSolutionComponents(string folder, Solution solution)
        {
            try
            {
                ToggleControls(false, "Creating file with Components...");

                var service = await GetService();
                var descriptor = await GetDescriptor();

                SolutionDescriptor solutionDescriptor = new SolutionDescriptor(_iWriteToOutput, service, descriptor);

                string fileName = EntityFileNameFormatter.GetSolutionFileName(
                    service.ConnectionData.Name
                    , solution.UniqueName
                    , "Components"
                );

                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                await solutionDescriptor.CreateFileWithSolutionComponentsAsync(filePath, solution.Id);

                this._iWriteToOutput.WriteToOutput("Solution Components was export into file '{0}'", filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);

                ToggleControls(true, "Creating file with Components completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Creating file with Components failed.");
            }
        }

        private async Task PerformOpenSolutionComponentsInWindow(string folder, Solution solution)
        {
            try
            {
                ToggleControls(false, "Analizing solutions...");

                _commonConfig.Save();

                var service = await GetService();

                var descriptor = await GetDescriptor();

                WindowHelper.OpenSolutionComponentDependenciesWindow(this._iWriteToOutput, service, descriptor, _commonConfig, solution.UniqueName, null);

                ToggleControls(true, "Analizing solutions completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Analizing solutions failed.");
            }
        }

        private async Task PerformShowingMissingDependencies(string folder, Solution solution)
        {
            try
            {
                ToggleControls(false, "Creating file with Missing Dependencies...");

                var service = await GetService();
                var descriptor = await GetDescriptor();

                SolutionDescriptor solutionDescriptor = new SolutionDescriptor(_iWriteToOutput, service, descriptor);

                ComponentsGroupBy showComponents = _commonConfig.ComponentsGroupBy;

                string showString = null;

                if (showComponents == ComponentsGroupBy.DependentComponents)
                {
                    showString = "dependent";
                }
                else
                {
                    showString = "required";
                }

                showString = string.Format("Missing Dependencies {0}", showString);

                string fileName = EntityFileNameFormatter.GetSolutionFileName(
                    service.ConnectionData.Name
                    , solution.UniqueName
                    , showString
                    );

                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                await solutionDescriptor.CreateFileWithSolutionMissingDependenciesAsync(filePath, solution.Id, showComponents, showString);

                this._iWriteToOutput.WriteToOutput("Solution {0} was export into file '{1}'", showString, filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);

                ToggleControls(true, "Creating file with Missing Dependencies completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Creating file with Missing Dependencies failed.");
            }
        }

        private async Task PerformShowingDependenciesForUninstall(string folder, Solution solution)
        {
            try
            {
                ToggleControls(false, "Creating file with Dependencies for Uninstall...");

                var service = await GetService();
                var descriptor = await GetDescriptor();

                SolutionDescriptor solutionDescriptor = new SolutionDescriptor(_iWriteToOutput, service, descriptor);

                ComponentsGroupBy showComponents = _commonConfig.ComponentsGroupBy;

                string showString = null;

                if (showComponents == ComponentsGroupBy.DependentComponents)
                {
                    showString = "dependent";
                }
                else
                {
                    showString = "required";
                }

                showString = string.Format("Dependencies for Uninstall {0}", showString);

                string fileName = EntityFileNameFormatter.GetSolutionFileName(
                    service.ConnectionData.Name
                    , solution.UniqueName
                    , showString
                    );

                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                await solutionDescriptor.CreateFileWithSolutionDependenciesForUninstallAsync(filePath, solution.Id, showComponents, showString);

                this._iWriteToOutput.WriteToOutput("Solution {0} was export into file '{1}'", showString, filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);

                ToggleControls(true, "Creating file with Dependencies for Uninstall completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Creating file with Dependencies for Uninstall failed.");
            }
        }

        private void mIAnalyzeSolutions_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
               && menuItem.Tag is Tuple<Solution, Solution> solutionPair
               && solutionPair.Item1 != null
               && solutionPair.Item2 != null
               && !string.Equals(solutionPair.Item1.UniqueName, solutionPair.Item2.UniqueName, StringComparison.InvariantCultureIgnoreCase)
               )
            {
                ExecuteActionOnSolutionPair(solutionPair.Item1, solutionPair.Item2, PerformAnalizeSolutions);
            }
        }

        private void mIShowUniqueComponentsInSolution_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
                && menuItem.Tag is Tuple<Solution, Solution> solutionPair
                && solutionPair.Item1 != null
                && solutionPair.Item2 != null
                && !string.Equals(solutionPair.Item1.UniqueName, solutionPair.Item2.UniqueName, StringComparison.InvariantCultureIgnoreCase)
                )
            {
                ExecuteActionOnSolutionPair(solutionPair.Item1, solutionPair.Item2, PerformAnalizeSolutionsAndShowUnique);
            }
        }

        private void ExecuteActionOnSolutionPair(Solution solution1, Solution solution2, Func<string, Solution, Solution, Task> action)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            if (solution1 == null || solution2 == null)
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

            try
            {
                action(folder, solution1, solution2);
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);
            }
        }

        private void ExecuteActionOnSolutionAndSolutionCollection(Solution[] solutions, Solution solution, Func<string, Solution[], Solution, Task> action)
        {
            if (!_controlsEnabled)
            {
                return;
            }

            if (solutions == null || solution == null)
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

            try
            {
                action(folder, solutions, solution);
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);
            }
        }

        private async Task PerformAnalizeSolutions(string folder, Solution solution1, Solution solution2)
        {
            try
            {
                ToggleControls(false, "Analizing solutions...");

                this._iWriteToOutput.WriteToOutput(string.Empty);
                this._iWriteToOutput.WriteToOutput(string.Empty);
                this._iWriteToOutput.WriteToOutput("Analyzing Solution Components '{0}' and '{1}'.", solution1.UniqueName, solution2.UniqueName);

                var service = await GetService();
                var descriptor = await GetDescriptor();

                SolutionDescriptor solutionDescriptor = new SolutionDescriptor(_iWriteToOutput, service, descriptor);

                await solutionDescriptor.FindUniqueComponentsInSolutionsAsync(solution1.Id, solution2.Id);

                ToggleControls(true, "Analizing solutions completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Analizing solutions failed.");
            }
        }

        private async Task PerformAnalizeSolutionsAndShowUnique(string folder, Solution solution1, Solution solution2)
        {
            try
            {
                ToggleControls(false, "Analizing solutions...");

                var service = await GetService();
                var descriptor = await GetDescriptor();

                SolutionDescriptor solutionDescriptor = new SolutionDescriptor(_iWriteToOutput, service, descriptor);

                string fileName = EntityFileNameFormatter.GetSolutionMultipleFileName(
                    service.ConnectionData.Name
                    , solution1.UniqueName
                    , solution2.UniqueName
                    , "Unique Components"
                    );

                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                await solutionDescriptor.CreateFileWithUniqueComponentsInSolution1Async(filePath, solution1.Id, solution2.Id);

                this._iWriteToOutput.WriteToOutput("Unique Solution Components '{0}' was export into file '{1}'", solution1.UniqueName, filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);

                ToggleControls(true, "Analizing solutions completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Analizing solutions failed.");
            }
        }

        private void mICopyComponentsFromSolution1ToSolution2_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
               && menuItem.Tag is Tuple<Solution, Solution> solutionPair
               && solutionPair.Item1 != null
               && solutionPair.Item2 != null
               && !string.Equals(solutionPair.Item1.UniqueName, solutionPair.Item2.UniqueName, StringComparison.InvariantCultureIgnoreCase)
               && solutionPair.Item2.IsManaged.GetValueOrDefault() == false
               )
            {
                string question = string.Format("Are you sure want to copy components from {0} to {1}?", solutionPair.Item1.UniqueName, solutionPair.Item2.UniqueName);

                if (MessageBox.Show(question, "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                {
                    return;
                }

                ExecuteActionOnSolutionPair(solutionPair.Item1, solutionPair.Item2, PerformCopyFromSolution1ToSolution2);
            }
        }

        private void mICopyComponentsFromSolutionCollectionToSolution_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
               && menuItem.Tag is Tuple<Solution[], Solution> solutionPair
               && solutionPair.Item1 != null
               && solutionPair.Item2 != null
               && solutionPair.Item2.IsManaged.GetValueOrDefault() == false
               )
            {
                foreach (var item in solutionPair.Item1)
                {
                    if (string.Equals(item.UniqueName, solutionPair.Item2.UniqueName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return;
                    }
                }

                string sourceName = string.Join(",", solutionPair.Item1.Select(en => en.UniqueName).OrderBy(s => s));

                string question = string.Format("Are you sure want to copy components from {0} to {1}?", sourceName, solutionPair.Item2.UniqueName);

                if (MessageBox.Show(question, "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                {
                    return;
                }

                ExecuteActionOnSolutionAndSolutionCollection(solutionPair.Item1, solutionPair.Item2, PerformCopyFromSolutionCollectionToSolution);
            }
        }

        private async Task PerformCopyFromSolution1ToSolution2(string folder, Solution solutionSource, Solution solutionTarget)
        {
            try
            {
                ToggleControls(false, "Coping solution components...");

                var service = await GetService();
                var descriptor = await GetDescriptor();

                SolutionDescriptor solutionDescriptor = new SolutionDescriptor(_iWriteToOutput, service, descriptor);

                SolutionComponentRepository repository = new SolutionComponentRepository(service);

                var componentsSource = await repository.GetSolutionComponentsAsync(solutionSource.Id);

                var componentsTarget = await repository.GetSolutionComponentsAsync(solutionTarget.Id);

                var componentesOnlyInSource = SolutionDescriptor.GetComponentsInFirstNotSecond(componentsSource, componentsTarget);

                if (componentesOnlyInSource.Count > 0)
                {
                    this._iWriteToOutput.WriteToOutput(string.Empty);
                    this._iWriteToOutput.WriteToOutput("Creating backup Solution Components in '{0}'.", solutionTarget.UniqueName);

                    {
                        string fileName = EntityFileNameFormatter.GetSolutionFileName(
                            service.ConnectionData.Name
                            , solutionTarget.UniqueName
                            , "Components Backup"
                        );

                        {
                            string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                            await solutionDescriptor.CreateFileWithSolutionComponentsAsync(filePath, solutionTarget.Id);

                            this._iWriteToOutput.WriteToOutput("Created backup Solution Components in '{0}': {1}", solutionTarget.UniqueName, filePath);
                            this._iWriteToOutput.WriteToOutputFilePathUri(filePath);
                        }

                        {
                            fileName = fileName.Replace(".txt", ".xml");

                            string filePath = Path.Combine(_commonConfig.FolderForExport, FileOperations.RemoveWrongSymbols(fileName));

                            await solutionDescriptor.CreateFileWithSolutionImageAsync(filePath, solutionTarget.Id);
                        }
                    }

                    this._iWriteToOutput.WriteToOutput(string.Empty);
                    this._iWriteToOutput.WriteToOutput("Showing Unique Solution Components in '{0}'.", solutionSource.UniqueName);

                    {
                        string fileName = EntityFileNameFormatter.GetSolutionMultipleFileName(
                            service.ConnectionData.Name
                            , solutionSource.UniqueName
                            , solutionTarget.UniqueName
                            , "Unique Components for Adding"
                            );

                        string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                        await solutionDescriptor.CreateFileWithUniqueComponentsInSolution1Async(filePath, solutionSource.Id, solutionTarget.Id);

                        this._iWriteToOutput.WriteToOutput("Created file with Unique Components in '{0}' for Adding to '{1}': {2}", solutionSource.UniqueName, solutionTarget.UniqueName, filePath);
                        this._iWriteToOutput.WriteToOutputFilePathUri(filePath);
                    }


                    this._iWriteToOutput.WriteToOutput(string.Empty);
                    this._iWriteToOutput.WriteToOutput("Coping Solution Components from '{0}' into '{1}'.", solutionSource.UniqueName, solutionTarget.UniqueName);

                    await repository.AddSolutionComponentsAsync(solutionTarget.UniqueName, componentesOnlyInSource);

                    this._iWriteToOutput.WriteToOutput("Copied {0} components.", componentesOnlyInSource.Count.ToString());
                }
                else
                {
                    this._iWriteToOutput.WriteToOutput("All Solution Components '{0}' already added into '{1}'.", solutionSource.UniqueName, solutionTarget.UniqueName);
                }

                ToggleControls(true, "Coping solution components completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Coping solution components failed.");
            }
        }
        private async Task PerformCopyFromSolutionCollectionToSolution(string folder, Solution[] solutionSourceCollection, Solution solutionTarget)
        {
            try
            {
                ToggleControls(false, "Coping solution components...");

                string sourceName = string.Join(",", solutionSourceCollection.Select(e => e.UniqueName).OrderBy(s => s));

                var service = await GetService();
                var descriptor = await GetDescriptor();

                SolutionDescriptor solutionDescriptor = new SolutionDescriptor(_iWriteToOutput, service, descriptor);

                SolutionComponentRepository repository = new SolutionComponentRepository(service);

                List<SolutionComponent> componentsSource = new List<SolutionComponent>();

                {
                    var hash = new HashSet<Tuple<int, Guid>>();

                    var temp = await repository.GetSolutionComponentsForCollectionAsync(solutionSourceCollection.Select(e => e.Id));

                    foreach (var item in temp)
                    {
                        if (hash.Add(Tuple.Create(item.ComponentType.Value, item.ObjectId.Value)))
                        {
                            componentsSource.Add(item);
                        }
                    }
                }

                var componentsTarget = await repository.GetSolutionComponentsAsync(solutionTarget.Id);

                var componentesOnlyInSource = SolutionDescriptor.GetComponentsInFirstNotSecond(componentsSource, componentsTarget);

                var componentesOnlyInTarget = SolutionDescriptor.GetComponentsInFirstNotSecond(componentsTarget, componentsSource);

                if (componentesOnlyInSource.Count > 0)
                {
                    this._iWriteToOutput.WriteToOutput(string.Empty);
                    this._iWriteToOutput.WriteToOutput("Creating backup Solution Components in '{0}'.", solutionTarget.UniqueName);

                    {
                        string fileName = EntityFileNameFormatter.GetSolutionFileName(
                            service.ConnectionData.Name
                            , solutionTarget.UniqueName
                            , "Components Backup"
                        );

                        {
                            string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                            await solutionDescriptor.CreateFileWithSolutionComponentsAsync(filePath, solutionTarget.Id);

                            this._iWriteToOutput.WriteToOutput("Created backup Solution Components in '{0}': {1}", solutionTarget.UniqueName, filePath);
                            this._iWriteToOutput.WriteToOutputFilePathUri(filePath);
                        }

                        {
                            fileName = fileName.Replace(".txt", ".xml");

                            string filePath = Path.Combine(_commonConfig.FolderForExport, FileOperations.RemoveWrongSymbols(fileName));

                            await solutionDescriptor.CreateFileWithSolutionImageAsync(filePath, solutionTarget.Id);
                        }
                    }

                    this._iWriteToOutput.WriteToOutput(string.Empty);
                    this._iWriteToOutput.WriteToOutput("Showing Unique Solution Components in '{0}'.", sourceName);

                    {
                        string fileName = EntityFileNameFormatter.GetSolutionMultipleFileName(
                            service.ConnectionData.Name
                            , sourceName
                            , solutionTarget.UniqueName
                            , "Unique Components for Adding"
                            );

                        string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                        await solutionDescriptor.CreateFileWithComponentsInSolution1Async(
                            filePath
                            , sourceName
                            , solutionTarget.UniqueName
                            , componentsSource.Count
                            , componentsTarget.Count
                            , componentesOnlyInSource
                            , componentesOnlyInTarget.Count
                            );

                        this._iWriteToOutput.WriteToOutput("Created file with Unique Components in '{0}' for Adding to '{1}': {2}", sourceName, solutionTarget.UniqueName, filePath);
                        this._iWriteToOutput.WriteToOutputFilePathUri(filePath);
                    }

                    this._iWriteToOutput.WriteToOutput(string.Empty);
                    this._iWriteToOutput.WriteToOutput("Coping Solution Components from '{0}' into '{1}'.", sourceName, solutionTarget.UniqueName);

                    await repository.AddSolutionComponentsAsync(solutionTarget.UniqueName, componentesOnlyInSource);

                    this._iWriteToOutput.WriteToOutput("Copied {0} components.", componentesOnlyInSource.Count.ToString());
                }
                else
                {
                    this._iWriteToOutput.WriteToOutput("All Solution Components '{0}' already added into '{1}'.", sourceName, solutionTarget.UniqueName);
                }

                ToggleControls(true, "Coping solution components completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Coping solution components failed.");
            }
        }

        private async void miExportSolution_Click(object sender, RoutedEventArgs e)
        {
            string name = lstVwSolutions.SelectedItems
                .OfType<EntityViewItem>()
                .Where(en => en.Solution != null)
                .OrderBy(en => en.Solution.UniqueName)
                .Select(en => en.Solution)
                .FirstOrDefault()?.UniqueName;

            _commonConfig.Save();

            var service = await GetService();

            WindowHelper.OpenExportSolutionWindow(
                this._iWriteToOutput
                , service
                , _commonConfig
                , null
                , name
                );
        }

        private void miCreateNewSolution_Click(object sender, RoutedEventArgs e)
        {
            ConnectionData connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

            if (connectionData != null)
            {
                connectionData.OpenSolutionCreateInWeb();
            }
        }

        private void mIOpenSolutionListInWeb_Click(object sender, RoutedEventArgs e)
        {
            ConnectionData connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

            if (connectionData != null)
            {
                connectionData.OpenCrmWebSite(OpenCrmWebSiteType.Solutions);
            }
        }

        private void mIOpenCustomizationInWeb_Click(object sender, RoutedEventArgs e)
        {
            ConnectionData connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

            if (connectionData != null)
            {
                connectionData.OpenCrmWebSite(OpenCrmWebSiteType.Customization);
            }
        }

        private void mIClearUnmanagedSolution_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
                && menuItem.Tag is Solution solution
                && solution.IsManaged.GetValueOrDefault() == false
                )
            {
                string question = string.Format("Are you sure want to clear solution {0}?", solution.UniqueName);

                if (MessageBox.Show(question, "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK)
                {
                    return;
                }

                ExecuteActionOnSingleSolution(solution, PerformClearUnmanagedSolution);
            }
        }

        private async Task PerformClearUnmanagedSolution(string folder, Solution solution)
        {
            try
            {
                ToggleControls(false, "Clearing solution...");

                var service = await GetService();
                var descriptor = await GetDescriptor();

                SolutionDescriptor solutionDescriptor = new SolutionDescriptor(_iWriteToOutput, service, descriptor);

                this._iWriteToOutput.WriteToOutput(string.Empty);
                this._iWriteToOutput.WriteToOutput("Creating backup Solution Components in '{0}'.", solution.UniqueName);

                {
                    string fileName = EntityFileNameFormatter.GetSolutionFileName(
                        service.ConnectionData.Name
                        , solution.UniqueName
                        , "Components Backup"
                    );

                    {
                        string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                        await solutionDescriptor.CreateFileWithSolutionComponentsAsync(filePath, solution.Id);

                        this._iWriteToOutput.WriteToOutput("Created backup Solution Components in '{0}': {1}", solution.UniqueName, filePath);
                        this._iWriteToOutput.WriteToOutputFilePathUri(filePath);
                    }

                    {
                        fileName = fileName.Replace(".txt", ".xml");

                        string filePath = Path.Combine(_commonConfig.FolderForExport, FileOperations.RemoveWrongSymbols(fileName));

                        await solutionDescriptor.CreateFileWithSolutionImageAsync(filePath, solution.Id);
                    }
                }

                SolutionComponentRepository repository = new SolutionComponentRepository(service);
                await repository.ClearSolutionAsync(solution.UniqueName);

                ToggleControls(true, "Clearing solution completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Clearing solution failed.");
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.F5)
            {
                e.Handled = true;

                ShowExistingSolutions();
            }

            base.OnKeyDown(e);
        }

        private void mIOpenSolutionInWeb_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
                && menuItem.Tag is Solution solution
                )
            {
                ConnectionData connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

                if (connectionData != null)
                {
                    connectionData.OpenSolutionInWeb(solution.Id);
                }
            }
        }

        private void mIUsedEntitiesInWorkflows_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
                && menuItem.Tag is Solution solution
                )
            {
                ExecuteActionOnSingleSolution(solution, PerformCreateFileWithUsedEntitiesInWorkflows);
            }
        }

        private void mIUsedNotExistsEntitiesInWorkflows_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem
                && menuItem.Tag is Solution solution
                )
            {
                ExecuteActionOnSingleSolution(solution, PerformCreateFileWithUsedNotExistsEntitiesInWorkflows);
            }
        }

        private async Task PerformCreateFileWithUsedEntitiesInWorkflows(string folder, Solution solution)
        {
            try
            {
                ToggleControls(false, "Creating file with Used Entities in Workflows...");

                var service = await GetService();
                var descriptor = await GetDescriptor();

                var workflowDescriptor = new WorkflowUsedEntitiesDescriptor(_iWriteToOutput, service, descriptor);

                string fileName = EntityFileNameFormatter.GetSolutionFileName(
                    service.ConnectionData.Name
                    , solution.UniqueName
                    , "UsedEntitiesInWorkflows"
                    );

                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                var stringBuider = new StringBuilder();

                await workflowDescriptor.GetDescriptionWithUsedEntitiesInSolutionWorkflowsAsync(stringBuider, solution.Id);

                File.WriteAllText(filePath, stringBuider.ToString(), new UTF8Encoding(false));

                this._iWriteToOutput.WriteToOutput("Solution Used Entities was export into file '{0}'", filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);

                ToggleControls(true, "Creating file with Used Entities in Workflows completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Creating file with Used Entities in Workflows failed.");
            }
        }

        private async Task PerformCreateFileWithUsedNotExistsEntitiesInWorkflows(string folder, Solution solution)
        {
            try
            {
                ToggleControls(false, "Creating file with Used Not Exists Entities in Workflows...");

                var service = await GetService();
                var descriptor = await GetDescriptor();

                var workflowDescriptor = new WorkflowUsedEntitiesDescriptor(_iWriteToOutput, service, descriptor);

                string fileName = EntityFileNameFormatter.GetSolutionFileName(
                    service.ConnectionData.Name
                    , solution.UniqueName
                    , "UsedNotExistsEntitiesInWorkflows"
                    );

                string filePath = Path.Combine(folder, FileOperations.RemoveWrongSymbols(fileName));

                var stringBuider = new StringBuilder();

                await workflowDescriptor.GetDescriptionWithUsedNotExistsEntitiesInSolutionWorkflowsAsync(stringBuider, solution.Id);

                File.WriteAllText(filePath, stringBuider.ToString(), new UTF8Encoding(false));

                this._iWriteToOutput.WriteToOutput("Solution Used Not Exists Entities was export into file '{0}'", filePath);

                this._iWriteToOutput.PerformAction(filePath, _commonConfig);

                ToggleControls(true, "Creating file with Used Not Exists Entities in Workflows completed.");
            }
            catch (Exception ex)
            {
                this._iWriteToOutput.WriteErrorToOutput(ex);

                ToggleControls(true, "Creating file with Used Not Exists Entities in Workflows failed.");
            }
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu contextMenu)
            {
                contextMenu.Items.Clear();

                var list = lstVwSolutions.SelectedItems
                                 .OfType<EntityViewItem>()
                                 .Select(en => en.Solution)
                                 .OrderBy(en => en.UniqueName)
                                 .ToList();

                if (list.Any())
                {
                    if (list.Count == 1)
                    {
                        var solution = list.First();

                        FillSolutionButtons(solution, contextMenu.Items);
                    }
                    else
                    {
                        foreach (var solution in list)
                        {
                            if (contextMenu.Items.Count > 0)
                            {
                                contextMenu.Items.Add(new Separator());
                            }

                            MenuItem menuItem = new MenuItem()
                            {
                                Header = string.Format("Solution {0}", solution.UniqueNameEscapeUnderscore),
                            };

                            contextMenu.Items.Add(menuItem);

                            FillSolutionButtons(solution, menuItem.Items);
                        }
                    }
                }
            }
        }

        private void btnClearSolutionComponentFilter_Click(object sender, RoutedEventArgs e)
        {
            this._componentType = null;
            this._objectId = null;

            this.Dispatcher.Invoke(() =>
            {
                sepClearSolutionComponentFilter.IsEnabled = btnClearSolutionComponentFilter.IsEnabled = false;
                sepClearSolutionComponentFilter.Visibility = btnClearSolutionComponentFilter.Visibility = Visibility.Collapsed;
            });

            ShowExistingSolutions();
        }

        private void cmBCurrentConnection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_init > 0)
            {
                return;
            }

            foreach (var removed in e.RemovedItems.OfType<ConnectionData>())
            {
                removed.LastSelectedSolutionsUniqueName.CollectionChanged -= LastSelectedSolutionsUniqueName_CollectionChanged;
                removed.LastSelectedSolutionsUniqueName.CollectionChanged -= LastSelectedSolutionsUniqueName_CollectionChanged;
            }

            foreach (var added in e.AddedItems.OfType<ConnectionData>())
            {
                added.LastSelectedSolutionsUniqueName.CollectionChanged -= LastSelectedSolutionsUniqueName_CollectionChanged;
                added.LastSelectedSolutionsUniqueName.CollectionChanged += LastSelectedSolutionsUniqueName_CollectionChanged;
            }

            FillCopyComponentsToLastSelectedSolutions();

            this.Dispatcher.Invoke(() =>
            {
                this._itemsSource.Clear();
            });

            if (!_controlsEnabled)
            {
                return;
            }

            ConnectionData connectionData = null;

            cmBCurrentConnection.Dispatcher.Invoke(() =>
            {
                connectionData = cmBCurrentConnection.SelectedItem as ConnectionData;

                if (connectionData != null)
                {
                    if (!_syncCacheObjects.ContainsKey(connectionData.ConnectionId))
                    {
                        var syncObject = new object();

                        _syncCacheObjects.Add(connectionData.ConnectionId, syncObject);

                        BindingOperations.EnableCollectionSynchronization(connectionData.LastSelectedSolutionsUniqueName, syncObject);
                    }
                }
            });

            if (connectionData != null)
            {
                ShowExistingSolutions();
            }
        }

        private void lstVwSolutions_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                var item = ((FrameworkElement)e.OriginalSource).DataContext as EntityViewItem;

                if (item != null && item.Solution != null)
                {
                    ExecuteActionOnSingleSolution(item.Solution, PerformOpenSolutionComponentsInWindow);
                }
            }
        }
    }
}