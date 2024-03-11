using CliWrap;
using CliWrap.Buffered;
using Nucs.JsonSettings;
using Windows.System;

namespace WinUIBundleMaker.Views;

public sealed partial class MainPage : Page
{
    List<string> msixFiles = new List<string>();
    string selectedKitPath = null;
    public MainPage()
    {
        this.InitializeComponent();
        appTitleBar.Window = App.CurrentWindow;
        Loaded += MainPage_Loaded;
    }

    private void MainPage_Loaded(object sender, RoutedEventArgs e)
    {
        GetWindowsKit();
    }

    private void GetWindowsKit()
    {
        ProgressStatus.IsActive = true;

        if (Directory.Exists(TxtWindowsKit.Text))
        {
            var files = Directory.EnumerateFiles(TxtWindowsKit.Text, "makeappx.exe", SearchOption.AllDirectories);
            RadioGroup.Items.Clear();

            if (files.Any())
            {
                foreach (var item in files)
                {
                    var fileName = GetSimplifiedName(item);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        var radio = new RadioButton
                        {
                            Content = fileName,
                            Tag = item
                        };
                        RadioGroup.Items.Add(radio);
                    }
                }

                InfobarStatus.Title = $"{RadioGroup.Items.Count()} Windows Kit Found";
                InfobarStatus.Severity = InfoBarSeverity.Success;
                InfobarStatus.IsOpen = true;
            }
            else
            {
                InfobarStatus.Title = "Windows Kit Not found";
                InfobarStatus.Severity = InfoBarSeverity.Error;
                InfobarStatus.IsOpen = true;
            }
        }
        else
        {
            InfobarStatus.Title = "Directory Not found";
            InfobarStatus.Severity = InfoBarSeverity.Error;
            InfobarStatus.IsOpen = true;
            TxtMSIXPath.IsEnabled = false;
            BtnBrowseMSIX.IsEnabled = false;
            RadioGroup.Items.Clear();
        }

        ProgressStatus.IsActive = false;
    }
    private string GetSimplifiedName(string item)
    {
        string text = null;
        try
        {
            var removeText = item.Substring(0, item.IndexOf("10.0."));
            text = item.Replace(removeText, "").Replace("makeappx.exe", "").Replace("\\", "-");
            var lastIndex = text.LastIndexOf("-");
            text = text.Remove(lastIndex);
            return text;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private async void BtnBrowseMSIX_Click(object sender, RoutedEventArgs e)
    {
        ProgressStatus.IsActive = true;
        msixFiles.Clear();

        var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.CurrentWindow);
        if (folder != null)
        {
            TxtMSIXPath.Text = folder.Path;

            var files = Directory.EnumerateFiles(TxtMSIXPath.Text, "*.msix", SearchOption.AllDirectories).ToList();

            if (files.Any())
            {
                var bundleDir = TxtMSIXPath.Text;
                if (ChkMove.IsChecked.Value)
                {
                    bundleDir = Path.Combine(TxtMSIXPath.Text, "BundleDirectory");

                    if (!Directory.Exists(bundleDir))
                    {
                        Directory.CreateDirectory(bundleDir);
                    }
                }
                
                InfobarMSIXStatus.Message = "";
                foreach (var item in files)
                {
                    var dest = Path.Combine(bundleDir, Path.GetFileName(item));

                    if (ChkMove.IsChecked.Value)
                        File.Move(item, dest);

                    InfobarMSIXStatus.Message += Path.GetFileName(item) + Environment.NewLine;
                    msixFiles.Add(dest);
                }

                InfobarMSIXStatus.Title = $"{files.Count()} MSIX found";
                InfobarMSIXStatus.Severity = InfoBarSeverity.Success;
                InfobarMSIXStatus.IsOpen = true;

                BtnBundle.IsEnabled = true;
            }
            else
            {
                InfobarMSIXStatus.Title = "MSIX files not found";
                InfobarMSIXStatus.Severity = InfoBarSeverity.Error;
                InfobarMSIXStatus.IsOpen = true;
                BtnBundle.IsEnabled = false;
            }
        }

        ProgressStatus.IsActive = false;
    }

    private async void BtnBundle_Click(object sender, RoutedEventArgs e)
    {
        if (msixFiles.Any())
        {
            ProgressStatus.IsActive = true;
            BtnBundle.IsEnabled = false;

            var inputDirectoryPath = TxtMSIXPath.Text;

            if (ChkMove.IsChecked.Value)
            {
                inputDirectoryPath = Path.Combine(TxtMSIXPath.Text, "BundleDirectory");
            }
            string outputFilePath = Path.Combine(inputDirectoryPath, "Bundle.msixbundle");

            var result = await Cli.Wrap(selectedKitPath)
                .WithArguments($"bundle /d \"{inputDirectoryPath}\" /p \"{outputFilePath}\"")
                .ExecuteBufferedAsync();

            ProgressStatus.IsActive = false;
            BtnBundle.IsEnabled = true;

            if (result.ExitCode == 0)
            {
                InfobarCompleteStatus.Title = "Bundle Created successfully!";
                InfobarCompleteStatus.Message = outputFilePath;
                InfobarCompleteStatus.Severity = InfoBarSeverity.Success;
                InfobarCompleteStatus.IsOpen = true;
                BtnOpenBundleFolder.Visibility = Visibility.Visible;
            }
            else
            {
                InfobarCompleteStatus.Title = "Bundle Creation failed!";
                InfobarCompleteStatus.Message = result.ExitCode.ToString();
                InfobarCompleteStatus.Severity = InfoBarSeverity.Error;
                InfobarCompleteStatus.IsOpen = true;
                BtnOpenBundleFolder.Visibility = Visibility.Collapsed;
            }
        }
    }

    private void RadioGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var radio = sender as RadioButtons;
        if (radio != null && radio.SelectedItem != null)
        {
            var radioItem = radio.SelectedItem as RadioButton;
            TxtMSIXPath.IsEnabled = true;
            BtnBrowseMSIX.IsEnabled = true;
            selectedKitPath = radioItem.Tag?.ToString();
        }
    }

    private async void BtnOpenBundleFolder_Click(object sender, RoutedEventArgs e)
    {
        var inputDirectoryPath = TxtMSIXPath.Text;
        if (ChkMove.IsChecked.Value)
        {
            inputDirectoryPath = Path.Combine(TxtMSIXPath.Text, "BundleDirectory");

        }
        await Launcher.LaunchFolderPathAsync(inputDirectoryPath);
    }

    private void TxtWindowsKit_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        GetWindowsKit();
    }

    private async void ShieldPower_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/winUICommunity/WinUICommunity"));
    }
    private async void ShieldDeveloper_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/ghost1372/"));
    }
    private async void ShieldGithub_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/ghost1372/"));
    }

    private async void BtnBrowseKit_Click(object sender, RoutedEventArgs e)
    {
        var folder = await FileAndFolderPickerHelper.PickSingleFolderAsync(App.CurrentWindow);
        if (folder != null)
        {
            msixFiles.Clear();
            TxtWindowsKit.Text = folder.Path;
            GetWindowsKit();
        }
    }
}

