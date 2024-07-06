using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using System.Diagnostics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

/*
    TODO:
        - [x] Setup File Getters for both steam and windows save files
        - [x] Compare both files once selected, and determine the most recent
        - [x] Sync Files

        Extra:
        - [] refactor to an enum for windows and steam; Use an object to store relavent data for each.
        - [] Save previously used file paths.
        - [] Restore backups if sync fails
        - [] Set the filename texts  using a setter on the file vars
        - [] add a sync status which is checked on any relavant changes, and handles enable/disable of syncbtn and sync status text
        - [] Add suggested path text
            - Steam: C:\Program Files (x86)\Steam)\steamapps\common\Deep Rock Galactic\FSD\Saved\SaveGames > [numbers]_Player.sav
            - Windows: C:\Users\(Your username here)\AppData\Local\Packages > CoffeeStainStudios.DeepRockGalactic_[characters] > SystemAppData > wgs > [dir characters] > [file characters]
 */


namespace Deep_Rock_Galactic_Save_File_Manager
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        StorageFile steamSaveFile = null;
        StorageFile windowsSaveFile = null;
        string mostRecent = string.Empty;
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "DRG Save File Manager";
        }

        private async void steamSaveFilePicker_Click(object sender, RoutedEventArgs e)
        {
            this.setSteamSaveFile(await GetFile());
        }

        private async void windowsSaveFilePicker_Click(object sender, RoutedEventArgs e)
        {
            this.setWindowsSaveFile(await GetFile());
        }

        private void setSteamSaveFile(StorageFile file)
        {
            if (file != null)
            {
                this.steamSaveFile = file;
                steamSaveFile_SelectedFileName.Text = file.Name;
            }
            compareSaveFiles();
        }

        private void setWindowsSaveFile(StorageFile file)
        {
            if (file != null)
            {
                this.windowsSaveFile = file;
                windowsSaveFile_SelectedFileName.Text = file.Name;
            }
            compareSaveFiles();
        }

        private async void compareSaveFiles()
        {
            if (this.steamSaveFile == null || this.windowsSaveFile == null)
            {
                compareFileText.Text = "Select Both Save Files before Comparing";
                syncBtn.IsEnabled = false;
                return;
            }

            BasicProperties steamSaveFileProperties = await this.steamSaveFile.GetBasicPropertiesAsync();
            DateTimeOffset steamFileDate = steamSaveFileProperties.DateModified;

            BasicProperties windowsSaveFileProperties = await this.windowsSaveFile.GetBasicPropertiesAsync();
            DateTimeOffset windowsFileDate = windowsSaveFileProperties.DateModified;

            steamDateText.Text = "Steam: " + steamFileDate;
            windowsDateText.Text = "Windows: " + windowsFileDate;

            if (steamFileDate > windowsFileDate)
            {
                //update windows file
                this.mostRecent = "Steam";
            }
            else if (steamFileDate < windowsFileDate)
            {
                //update steam file
                this.mostRecent = "Windows";
            }
            else
            {
                //Both files have same date; Are in Sync
                this.mostRecent = "None";
                syncBtn.IsEnabled = false;
                compareFileText.Text = "Files are already Synced";
                return;
            }
            compareFileText.Text = "Most Recent: " + this.mostRecent;
            syncBtn.IsEnabled = true;
        }

        private async void syncBtn_Click(object sender, RoutedEventArgs e)
        {
            attemptSync();
        }

        private async void attemptSync()
        {
            //Backup
            bool backupSuccess = await backupSaveFiles();
            if (!backupSuccess)
            {
                await ShowAlert("Backup Failed. Sync Aborted.");
                return;
            }
            //Run Sync
            bool syncSuccess = await syncSaveFiles();
            if (syncSuccess)
            {
                syncBtn.IsEnabled = false;
                compareFileText.Text = "Files are already Synced";
                await ShowAlert("Save Files Synced.");
            }
        }

        private async Task<bool> syncSaveFiles()
        {
            StorageFile fileToCopy = this.mostRecent.ToLower() == "steam" ? this.steamSaveFile : this.windowsSaveFile;
            StorageFile fileToReplace = this.mostRecent.ToLower() == "steam" ? this.windowsSaveFile : this.steamSaveFile;

            try
            {
                StorageFolder fileToReplaceParent = await fileToReplace.GetParentAsync();
                StorageFile copiedSaveFile = await fileToCopy.CopyAsync(fileToReplaceParent, fileToReplace.Name, NameCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                await ShowAlert($"Sync file Copy Failed. {ex.Message}");
                //TODO: Restore backups
                return false;
            }
            return true;
        }

        private async Task<bool> backupSaveFiles()
        {
            // Create backup directories in AppData
            StorageFolder appDataFolder = ApplicationData.Current.LocalFolder;
            StorageFolder steamBakFolder;
            StorageFolder windowsBakFolder;
            string curFolderErrorLog = "main";
            try
            {
                DateTime now = DateTime.Now;
                string bakFolderName = $"{now.Month}_{now.Day}_{now.Year}_{now.Hour}_{now.Minute}_{now.Second}";
                StorageFolder bakFolder = await appDataFolder.CreateFolderAsync($"DRG_Save_bak_{bakFolderName}", CreationCollisionOption.OpenIfExists);
                curFolderErrorLog = "steam";
                steamBakFolder = await bakFolder.CreateFolderAsync("steam", CreationCollisionOption.OpenIfExists);
                curFolderErrorLog = "windows";
                windowsBakFolder = await bakFolder.CreateFolderAsync("windows", CreationCollisionOption.OpenIfExists);
            }
            catch (Exception ex)
            {
                await ShowAlert($"An error occurred when attempting to create the {curFolderErrorLog} backup folders.. {ex.Message}");
                return false;
            }

            // Copy Files
            string curFileErrorLog = "Steam";
            try
            {
                await this.steamSaveFile.CopyAsync(steamBakFolder, this.steamSaveFile.Name, NameCollisionOption.ReplaceExisting);
                curFileErrorLog = "windows";
                await this.windowsSaveFile.CopyAsync(windowsBakFolder, this.windowsSaveFile.Name, NameCollisionOption.ReplaceExisting);
            }
            catch (Exception ex)
            {
                await ShowAlert($"An error occurred when attempting to copy the {curFileErrorLog} save files.. {ex.Message}");
                return false;
            }
            return true;
        }

        private async Task<StorageFile> GetFile()
        {
            var filePicker = new FileOpenPicker();
            filePicker.FileTypeFilter.Add("*");

            // Set the window handle for the file picker (required for WinUI 3)
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

            return await filePicker.PickSingleFileAsync();
        }

        public async Task ShowAlert(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Alert",
                Content = message,
                CloseButtonText = "OK"
            };

            dialog.XamlRoot = this.Content.XamlRoot;
            await dialog.ShowAsync();
        }

    }

    public class FileHandler
    {

        public string id;
        public Action<StorageFile> OnFileSelected;

        public FileHandler(string id)
        {
            this.id = id;
        }

    }

}




//== ORIG before File Container Class
//namespace Deep_Rock_Galactic_Save_File_Manager
//{
//    /// <summary>
//    /// An empty window that can be used on its own or navigated to within a Frame.
//    /// </summary>
//    public sealed partial class MainWindow : Window
//    {

//        StorageFile steamSaveFile = null;
//        StorageFile windowsSaveFile = null;
//        string mostRecent = string.Empty;
//        public MainWindow()
//        {
//            this.InitializeComponent();
//            this.Title = "DRG Save File Manager";
//        }

//        private async void steamSaveFilePicker_Click(object sender, RoutedEventArgs e)
//        {
//            this.setSteamSaveFile(await GetFile());
//        }

//        private async void windowsSaveFilePicker_Click(object sender, RoutedEventArgs e)
//        {
//            this.setWindowsSaveFile(await GetFile());
//        }

//        private void setSteamSaveFile(StorageFile file)
//        {
//            if(file!=null)
//            {
//                this.steamSaveFile = file;
//                steamSaveFile_SelectedFileName.Text = file.Name;
//            }
//            compareSaveFiles();
//        }

//        private void setWindowsSaveFile(StorageFile file)
//        {
//            if (file != null)
//            {
//                this.windowsSaveFile = file;
//                windowsSaveFile_SelectedFileName.Text = file.Name;
//            }
//            compareSaveFiles();
//        }

//        private async void compareSaveFiles()
//        {
//            if(this.steamSaveFile==null || this.windowsSaveFile==null)
//            {
//                compareFileText.Text = "Select Both Save Files before Comparing";
//                syncBtn.IsEnabled = false;
//                return;
//            }

//            BasicProperties steamSaveFileProperties = await this.steamSaveFile.GetBasicPropertiesAsync();
//            DateTimeOffset steamFileDate = steamSaveFileProperties.DateModified;

//            BasicProperties windowsSaveFileProperties = await this.windowsSaveFile.GetBasicPropertiesAsync();
//            DateTimeOffset windowsFileDate = windowsSaveFileProperties.DateModified;

//            steamDateText.Text = "Steam: " + steamFileDate;
//            windowsDateText.Text = "Windows: " + windowsFileDate;

//            if(steamFileDate >  windowsFileDate)
//            {
//                //update windows file
//                this.mostRecent = "Steam";
//            } else if(steamFileDate < windowsFileDate)
//            {
//                //update steam file
//                this.mostRecent = "Windows";
//            }
//            else
//            {
//                //Both files have same date; Are in Sync
//                this.mostRecent = "None";
//                syncBtn.IsEnabled = false;
//                compareFileText.Text = "Files are already Synced";
//                return;
//            }
//            compareFileText.Text = "Most Recent: "+this.mostRecent;
//            syncBtn.IsEnabled = true;
//        }

//        private async void syncBtn_Click(object sender, RoutedEventArgs e)
//        {
//            attemptSync();
//        }

//        private async void attemptSync()
//        {
//            //Backup
//            bool backupSuccess = await backupSaveFiles();
//            if (!backupSuccess)
//            {
//                await ShowAlert("Backup Failed. Sync Aborted.");
//                return;
//            }
//            //Run Sync
//            bool syncSuccess = await syncSaveFiles();
//            if(syncSuccess)
//            {
//                syncBtn.IsEnabled = false;
//                compareFileText.Text = "Files are already Synced";
//                await ShowAlert("Save Files Synced.");
//            }
//        }

//        private async Task<bool> syncSaveFiles()
//        {
//            StorageFile fileToCopy = this.mostRecent.ToLower() == "steam" ? this.steamSaveFile : this.windowsSaveFile;
//            StorageFile fileToReplace = this.mostRecent.ToLower() == "steam" ? this.windowsSaveFile : this.steamSaveFile;

//            try
//            {
//                StorageFolder fileToReplaceParent = await fileToReplace.GetParentAsync();
//                StorageFile copiedSaveFile = await fileToCopy.CopyAsync(fileToReplaceParent, fileToReplace.Name, NameCollisionOption.ReplaceExisting);
//            }
//            catch (Exception ex)
//            {
//                await ShowAlert($"Sync file Copy Failed. {ex.Message}");
//                //TODO: Restore backups
//                return false;
//            }
//            return true;
//        }

//        private async Task<bool> backupSaveFiles()
//        {
//            // Create backup directories in AppData
//            StorageFolder appDataFolder = ApplicationData.Current.LocalFolder;
//            StorageFolder steamBakFolder;
//            StorageFolder windowsBakFolder;
//            string curFolderErrorLog = "main";
//            try
//            {
//                DateTime now = DateTime.Now;
//                string bakFolderName =$"{now.Month}_{now.Day}_{now.Year}_{now.Hour}_{now.Minute}_{now.Second}";
//                StorageFolder bakFolder = await appDataFolder.CreateFolderAsync($"DRG_Save_bak_{bakFolderName}", CreationCollisionOption.OpenIfExists);
//                curFolderErrorLog = "steam";
//                steamBakFolder = await bakFolder.CreateFolderAsync("steam", CreationCollisionOption.OpenIfExists);
//                curFolderErrorLog = "windows";
//                windowsBakFolder = await bakFolder.CreateFolderAsync("windows", CreationCollisionOption.OpenIfExists);
//            }
//            catch (Exception ex) {
//                await ShowAlert($"An error occurred when attempting to create the {curFolderErrorLog} backup folders.. {ex.Message}");
//                return false;
//            }

//            // Copy Files
//            string curFileErrorLog = "Steam";
//            try
//            {
//                await this.steamSaveFile.CopyAsync(steamBakFolder, this.steamSaveFile.Name, NameCollisionOption.ReplaceExisting);
//                curFileErrorLog = "windows";
//                await this.windowsSaveFile.CopyAsync(windowsBakFolder, this.windowsSaveFile.Name, NameCollisionOption.ReplaceExisting);
//            } catch(Exception ex)
//            {
//                await ShowAlert($"An error occurred when attempting to copy the {curFileErrorLog} save files.. {ex.Message}");
//                return false;
//            }
//            return true;
//        }

//        private async Task<StorageFile> GetFile()
//        {
//            var filePicker = new FileOpenPicker();
//            filePicker.FileTypeFilter.Add("*");

//            // Set the window handle for the file picker (required for WinUI 3)
//            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
//            WinRT.Interop.InitializeWithWindow.Initialize(filePicker, hwnd);

//            return await filePicker.PickSingleFileAsync();
//        }

//        public async Task ShowAlert(string message)
//        {
//            ContentDialog dialog = new ContentDialog
//            {
//                Title = "Alert",
//                Content = message,
//                CloseButtonText = "OK"
//            };

//            dialog.XamlRoot = this.Content.XamlRoot;
//            await dialog.ShowAsync();
//        }

//    }
//}
