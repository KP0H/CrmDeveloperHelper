using Nav.Common.VSPackages.CrmDeveloperHelper.Model;
using Nav.Common.VSPackages.CrmDeveloperHelper.Repository;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Nav.Common.VSPackages.CrmDeveloperHelper.Views
{
    public partial class WindowCommonConfiguration : WindowBase
    {
        private readonly object sysObjectUtils = new object();

        private CommonConfiguration _config;

        public WindowCommonConfiguration(CommonConfiguration config)
        {
            InputLanguageManager.SetInputLanguage(this, CultureInfo.CreateSpecificCulture("en-US"));

            this._config = config;

            InitializeComponent();

            BindingOperations.EnableCollectionSynchronization(this._config.Utils, sysObjectUtils);

            lstVwUtils.ItemsSource = this._config.Utils;

            LoadFields();
        }

        protected override void OnClosed(EventArgs e)
        {
            BindingOperations.ClearAllBindings(lstVwUtils);

            lstVwUtils.Items.DetachFromSourceCollection();

            lstVwUtils.ItemsSource = null;

            base.OnClosed(e);
        }

        private void LoadFields()
        {
            chBClearOutputWindow.IsChecked = _config.ClearOutputWindowBeforeCRMOperation;

            chBDoNotPromtPublishMessage.IsChecked = _config.DoNotPropmPublishMessage;

            txtBFolderForExport.Text = _config.FolderForExport;

            txtBCompareProgram.Text = _config.CompareProgram;

            txtBCompareArgumentsFormat.Text = _config.CompareArgumentsFormat;

            txtBCompareArgumentsThreeWayFormat.Text = _config.CompareArgumentsThreeWayFormat;

            txtBTextEditorProgram.Text = _config.TextEditorProgram;

            txtBFileNameFormsEvents.Text = _config.FormsEventsFileName;

            txtBFileNamePluginConfiguration.Text = _config.PluginConfigurationFileName;
        }

        private void WriteFields()
        {
            _config.ClearOutputWindowBeforeCRMOperation = chBClearOutputWindow.IsChecked.GetValueOrDefault();

            _config.DoNotPropmPublishMessage = chBDoNotPromtPublishMessage.IsChecked.GetValueOrDefault();

            _config.FolderForExport = txtBFolderForExport.Text.Trim();

            _config.CompareProgram = txtBCompareProgram.Text.Trim();

            _config.CompareArgumentsFormat = txtBCompareArgumentsFormat.Text.Trim();

            _config.CompareArgumentsThreeWayFormat = txtBCompareArgumentsThreeWayFormat.Text.Trim();

            _config.TextEditorProgram = txtBTextEditorProgram.Text.Trim();

            _config.FormsEventsFileName = txtBFileNameFormsEvents.Text.Trim();

            _config.PluginConfigurationFileName = txtBFileNamePluginConfiguration.Text.Trim();

            if (string.IsNullOrEmpty(_config.PluginConfigurationFileName))
            {
                _config.PluginConfigurationFileName = "Plugins Configuration";
            }

            if (string.IsNullOrEmpty(_config.FormsEventsFileName))
            {
                _config.FormsEventsFileName = "System Forms Events";
            }

            _config.Save();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;

            this.Close();
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

        private void ToggleControl(Control c, bool enabled)
        {
            c.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (c is TextBox)
                    {
                        ((TextBox)c).IsReadOnly = !enabled;
                    }
                    else
                    {
                        c.IsEnabled = enabled;
                    }
                }
                catch (Exception)
                {
                }
            });
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfigIsValid())
            {
                return;
            }

            WriteFields();

            this.Close();
        }

        private bool ConfigIsValid()
        {
            StringBuilder message = new StringBuilder();

            {
                string pathExport = txtBFolderForExport.Text.Trim();

                if (!string.IsNullOrEmpty(pathExport))
                {
                    if (!Directory.Exists(pathExport))
                    {
                        if (message.Length > 0) { message.AppendLine(); }

                        message.Append("Folder for Export files does not exit.");
                    }
                }
            }

            {
                string pathFile = txtBCompareProgram.Text.Trim();

                if (!string.IsNullOrEmpty(pathFile))
                {
                    if (File.Exists(pathFile))
                    {
                        {
                            string format = txtBCompareArgumentsFormat.Text.Trim();

                            if (string.IsNullOrEmpty(format))
                            {
                                if (message.Length > 0) { message.AppendLine(); }

                                message.Append("Difference two files arguments format is empty.");
                            }
                            else
                            {
                                if (!format.Contains("%f1"))
                                {
                                    if (message.Length > 0) { message.AppendLine(); }

                                    message.Append("Difference two files arguments format does not contain %f1.");
                                }

                                if (!format.Contains("%f2"))
                                {
                                    if (message.Length > 0) { message.AppendLine(); }

                                    message.Append("Difference two files arguments format does not contain %f2.");
                                }
                            }
                        }

                        {
                            string formatThreeWay = txtBCompareArgumentsThreeWayFormat.Text.Trim();

                            if (!string.IsNullOrEmpty(formatThreeWay))
                            {
                                if (!formatThreeWay.Contains("%f1"))
                                {
                                    if (message.Length > 0) { message.AppendLine(); }

                                    message.Append("ThreeWay Difference arguments format does not contain %f1.");
                                }

                                if (!formatThreeWay.Contains("%f2"))
                                {
                                    if (message.Length > 0) { message.AppendLine(); }

                                    message.Append("ThreeWay Difference arguments format does not contain %f2.");
                                }

                                if (!formatThreeWay.Contains("%fl"))
                                {
                                    if (message.Length > 0) { message.AppendLine(); }

                                    message.Append("ThreeWay Difference arguments format does not contain %fl.");
                                }
                            }
                        }
                    }
                    else
                    {
                        if (message.Length > 0) { message.AppendLine(); }

                        message.Append("Program for difference not exists.");
                    }
                }
            }

            {
                string pathFile = txtBTextEditorProgram.Text.Trim();

                if (!string.IsNullOrEmpty(pathFile))
                {
                    if (!File.Exists(pathFile))
                    {
                        if (message.Length > 0) { message.AppendLine(); }

                        message.Append("Program for edit text not exists.");
                    }
                }
            }

            if (message.Length > 0)
            {
                MessageBox.Show(message.ToString(), Properties.MessageBoxStrings.ErrorTitle, MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }

            return true;
        }

        private void btnClearCacheTranslations_Click(object sender, RoutedEventArgs e)
        {
            TranslationRepository.ClearCache();
        }

        private void btnOpenConfigFolder_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(this._config.Path))
            {
                string folder = Path.GetDirectoryName(this._config.Path);

                if (Directory.Exists(folder))
                {
                    try
                    {
                        Process.Start(folder);
                    }
                    catch (Exception)
                    { }
                }
            }
        }

        private void btnSelectFolderForExport_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtBFolderForExport.Text = dialog.SelectedPath;
                }
            }
        }

        private void tSBCreate_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog1 = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Crm Scv Util (CrmScvUtil.exe)|*.exe",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog1.ShowDialog().GetValueOrDefault())
            {
                try
                {
                    CrmSvcUtil util = new CrmSvcUtil
                    {
                        Path = openFileDialog1.FileName,
                        Id = Guid.NewGuid()
                    };

                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(util.Path);
                    util.Version = string.Format("{0}.{1}.{2}.{3}", versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductPrivatePart, versionInfo.ProductBuildPart);

                    this._config.Utils.Add(util);
                }
                catch (Exception ex)
                {
                    Helpers.DTEHelper.WriteExceptionToOutput(ex);
#if DEBUG
                    if (System.Diagnostics.Debugger.IsAttached) System.Diagnostics.Debugger.Break();
#endif
                }
            }
        }

        private void tSBUp_Click(object sender, RoutedEventArgs e)
        {
            if (lstVwUtils.SelectedItems.Count == 1)
            {
                CrmSvcUtil util = lstVwUtils.SelectedItem as CrmSvcUtil;

                int index = _config.Utils.IndexOf(util);

                if (index != 0)
                {
                    _config.Utils.Move(index, index - 1);
                }

                lstVwUtils.SelectedItem = util;
            }
        }

        private void tSBDown_Click(object sender, RoutedEventArgs e)
        {
            if (lstVwUtils.SelectedItems.Count == 1)
            {
                CrmSvcUtil util = lstVwUtils.SelectedItem as CrmSvcUtil;

                int index = _config.Utils.IndexOf(util);

                if (index != _config.Utils.Count - 1)
                {
                    _config.Utils.Move(index, index + 1);
                }

                lstVwUtils.SelectedItem = util;
            }
        }

        private void tSBDelete_Click(object sender, RoutedEventArgs e)
        {
            if (lstVwUtils.SelectedItems.Count == 1)
            {
                CrmSvcUtil util = lstVwUtils.SelectedItems[0] as CrmSvcUtil;

                if (MessageBox.Show(Properties.MessageBoxStrings.DeleteCrmSvcUtil, Properties.MessageBoxStrings.QuestionTitle, MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
                {
                    _config.Utils.Remove(util);
                }
            }
        }

        private void lstVwUtils_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateButtonsEnable();
        }

        private void UpdateButtonsEnable()
        {
            this.lstVwUtils.Dispatcher.Invoke(() =>
            {
                try
                {
                    bool enabled = this.lstVwUtils.SelectedItems.Count > 0;

                    UIElement[] list = { tSBDelete, tSBUp, tSBDown };

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
    }
}