using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Storage.Pickers;
using Windows.UI.Input;
using Windows.UI.Notifications;
using Windows.Foundation;
using Orange_Music_Player.AttachedProperties;
using System.ComponentModel;
using Windows.Data.Xml.Dom;
using System.Xml.Linq;
using Windows.UI.ViewManagement;

namespace Orange_Music_Player
{
    
    public sealed partial class PlayerView : Page, INotifyPropertyChanged
    {

        // Notifying properties
        private string playlistNameToSave = String.Empty;
        public string PlaylistNameToSave
        {
            get => this.playlistNameToSave;
            set
            {
                this.playlistNameToSave = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("PlaylistNameToSave"));
                }
            }
        }

        private string playlistToDelete = String.Empty;
        public string PlaylistToDelete
        {
            get => this.playlistToDelete;
            set
            {
                this.playlistToDelete = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("PlaylistToDelete"));
                }
            }
        }

        private bool showOpenPane = false;
        public bool ShowOpenPane
        {
            get => this.showOpenPane;
            set
            {
                this.showOpenPane = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("ShowOpenPane"));
                }
            }
        }

        private bool playlistNameTaken = false;
        public bool PlaylistNameTaken
        {
            get => this.playlistNameTaken;
            set
            {
                this.playlistNameTaken = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("PlaylistNameTaken"));
                }
            }
        }

        private bool okToSave = false;
        public bool OkToSave
        {
            get => this.okToSave;
            set
            {
                this.okToSave = value;
                if (null != this.PropertyChanged)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("OkToSave"));
                }
            }
        }

        // Private properties
        private bool okToDelete = false;
        private int itemToRemove = -1;
        private string[] allowedAudioFileTypes = { ".flac", ".mp3", ".m4a", ".aac", ".wav", ".ogg", ".aif", ".aiff" };
        private string[] allowedImageFileTypes = { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf" };
        private PlaylistReferenceCollection<PlaylistReference> SavedPlaylists;
        private Player player;
        private bool ReorderingInitiated = false;

        // Constructor
        public PlayerView()
        {
            this.InitializeComponent();
            player = new Player();
            player.LoadDefaultPlaylist();
            SavedPlaylists = PlaylistStatic.GetSavedPlaylists();
            Windows.ApplicationModel.Core.CoreApplication.Suspending += (ss, ee) =>
            {
                player.Playlist.SaveDefault();
            };
        }

        // Player controls
        private void Play(int index) => player.Play(index);

        private void Play() => player.Play();

        private void Pause() => player.Pause();

        private void FF() => player.FF();

        private void RW() => player.RW();

        private void TogglePlayPause() => player.TogglePlayPause();

        private void ToggleShuffle() => player.ToggleShuffle();
        
        private void ClearPlaylist() => player.ClearPlaylist();

        private void DeleteItem(int index) => player.DeleteItem(index);

        private void DeleteFlyoutItem()
        {
            if (itemToRemove < 0 || itemToRemove >= player.Playlist.Items.Count())
                return;
            player.DeleteItem(itemToRemove);
            itemToRemove = -1;
        }

        private void ToggleRepeatMode() => player.ToggleRepeatMode();

        // Loading files 
        private void AddFileFilters(ref FileOpenPicker picker)
        {
            foreach(var t in allowedAudioFileTypes)
            {
                picker.FileTypeFilter.Add(t);
            }
            foreach(var t in allowedImageFileTypes)
            {
                picker.FileTypeFilter.Add(t);
            }
        }

        private void AddFileFilters(ref FolderPicker picker)
        {
            foreach (var t in allowedAudioFileTypes)
            {
                picker.FileTypeFilter.Add(t);
            }
            foreach (var t in allowedImageFileTypes)
            {
                picker.FileTypeFilter.Add(t);
            }
        }

        private async void AddFiles()
        {
            var filePicker = new FileOpenPicker();
            AddFileFilters(ref filePicker);

            IReadOnlyList<StorageFile> files = await filePicker.PickMultipleFilesAsync();
            await LoadFiles(files);
        }

        private async void AddFolder()
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            AddFileFilters(ref folderPicker);

            StorageFolder folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                IReadOnlyList<StorageFile> files = await folder.GetFilesAsync();
                await LoadFiles(files);
            }
        }

        private async void Playlist_Drop(object sender, DragEventArgs e)
        {
            if (ReorderingInitiated)
            {
                Playlist_DragItemsCompleted();
                return;
            }
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                IEnumerable<StorageFile> files = null;
                IEnumerable<StorageFile> tmpFiles = null;
                IEnumerable<StorageFolder> folders = items.OfType<StorageFolder>();
                if (folders.Count() > 0)
                {
                    foreach (var folder in folders)
                    {
                        tmpFiles = await folder.GetFilesAsync();
                        if (files == null)
                        {
                            files = tmpFiles;
                        }
                        else
                        {
                            files = files.Concat(tmpFiles);
                        }
                    }
                }
                tmpFiles = items.OfType<StorageFile>();
                if (files == null)
                {
                    files = tmpFiles;
                }
                else
                {
                    files = files.Concat(tmpFiles);
                }
                await LoadFiles(files);
            }
        }

        public async Task LoadFiles(IEnumerable<StorageFile> files)
        {
            ShowOpenPane = false;
            if (files != null)
            {
                var currentPlaylistSize = player.Playlist.Items.Count();
                IEnumerable<StorageFile> audioFiles = files.Where(i => allowedAudioFileTypes.Contains(i.FileType));
                IEnumerable<StorageFile> imageFiles = AlbumCover.GetValidCoverImagesFromFiles(files);
                await player.LoadFilesToPlaylist(audioFiles, imageFiles);
                var sizeDifference = player.Playlist.Items.Count() - currentPlaylistSize;
                if (sizeDifference > 0)
                {
                    var msg = sizeDifference + " track";
                    if (sizeDifference != 1)
                        msg += "s";
                    ServeToast(msg + " added");
                }
            }
        }

        // Loading playlists
        private void TogglePlaylistPane()
        {
            ShowOpenPane = !ShowOpenPane;
        }

        private void OpenPlaylist(object sender, RoutedEventArgs e)
        {
            var info = GetPlaylistLabelsFromElement(sender);
            player.LoadPlaylist(info.Item1);
            PlaylistNameToSave = player.Playlist.Name;
            ShowOpenPane = false;
        }

        private void OpenPlaylist(object sender, TappedRoutedEventArgs e)
        {
            var info = GetPlaylistLabelsFromElement(sender);
            player.LoadPlaylist(info.Item1);
            PlaylistNameToSave = player.Playlist.Name;
            ShowOpenPane = false;
            UpdateTitleBar();
        }

        private void AddToExistingPlaylist(object sender, TappedRoutedEventArgs e)
        {
            var info = GetPlaylistLabelsFromElement(sender);
            player.AddPlaylistToExistingPlaylist(info.Item1);
            ShowOpenPane = false;
            e.Handled = true;
            ServeToast(info.Item2 + " was added to the current playlist");
        }

        private Tuple<int, string> GetPlaylistLabelsFromElement(object e)
        {
            var element = (UIElement)e;
            return new Tuple<int, string>(Ap.GetPlaylistId(element), Ap.GetPlaylistName(element));
        }

        // Saving playlists
        private async void OpenSaveDialog()
        {
            ShowOpenPane = false;
            CheckIfNameTaken();
            await SavePlaylistDialog.ShowAsync();
        }

        private void SavePlaylist()
        {
            string name = PlaylistNameToSave;
            if (name.Length < 1)
            {
                // empty name should be captured in view
                return;
            }
            if (String.IsNullOrEmpty(player.Playlist.Name))
            {
                var id = player.SavePlaylist(name);
                if (id == -1)
                {
                    // name was already taken
                    // should be captured in view
                    return;
                }
                SavedPlaylists.Add(new PlaylistReference(id, name));
                UpdateTitleBar();
                return;
            }

            if (name != player.Playlist.Name)
            {
                var newId = player.SavePlaylistAs(name);
                SavedPlaylists.Add(new PlaylistReference(newId, name));
                UpdateTitleBar();
            } 
            else
            {
                player.SavePlaylist(name);
                ServeToast(name + " saved");
            }
        }

        private void CheckIfNameTaken()
        {
            var name = PlaylistNameToSave;
            if (!String.IsNullOrEmpty(player.Playlist.Name))
            {
                PlaylistNameTaken = false;
                if (name.Length > 0)
                    OkToSave = true;
                return;
            }
            
            foreach(var playlist in SavedPlaylists)
            {
                if (name == playlist.Name)
                {
                    PlaylistNameTaken = true;
                    OkToSave = false;
                    return;
                }
            }
            PlaylistNameTaken = false;
            if (name.Length > 0)
                OkToSave = true;
            else
                OkToSave = false;
            return;
        }

        // Deleting playlists
        private void ConfirmDelete()
        {
            okToDelete = true;
        }

        private async void DeletePlaylist(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
            var info = GetPlaylistLabelsFromElement(sender);
            PlaylistToDelete = info.Item2;
            var id = info.Item1;
            await ConfirmDeleteDialog.ShowAsync();
            if (!okToDelete)
                return;
            PlaylistStatic.DeletePlaylistById(id);
            if (id == player.Playlist.Id)
            {
                player.Playlist.UpdateAfterDeletedFromDb();
                UpdateTitleBar();
            }
            okToDelete = false;
            ServeToast(PlaylistToDelete + " has been deleted");
            foreach(var playlist in SavedPlaylists)
            {
                if (playlist.Id == id)
                {
                    SavedPlaylists.Remove(playlist);
                    if (SavedPlaylists.Count() < 1)
                        ShowOpenPane = false;
                    return;
                }
            }
        }

        // Additional UX control
        private void PlaylistItem_DoubleTapped()
        {
            Play(player.TargetIndex);
        }

        private void Playlist_DragOver(object sender, DragEventArgs e)
        {
            if (ReorderingInitiated)
            {
                e.AcceptedOperation = DataPackageOperation.Move;
            }
            else
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
        }

        private void Playlist_DragItemsStart()
        {
            ReorderingInitiated = true;
            player.Playlist.PrepareForReorder();
        }

        private void Playlist_DragItemsCompleted()
        {
            ReorderingInitiated = false;
            player.Playlist.SetSelectedIndexAfterReorder();
        }

        private void ShowPlaylistFlyout(object sender, RightTappedRoutedEventArgs e)
        {
            ListView list = (ListView)sender;
            RemoveItemFlyout.ShowAt(list, e.GetPosition(list));
            var item = ((FrameworkElement)e.OriginalSource).DataContext as PlaylistItem;
            itemToRemove = player.Playlist.Items.IndexOf(item);
        }

        private void PlaylistList_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            switch (e.Key)
            {
                case VirtualKey.P:
                    if (player.TargetIndex > -1)
                    {
                        try
                        {
                            var item = player.Playlist.Items[player.TargetIndex];
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                        Play(player.TargetIndex);
                    }
                    break;

                case VirtualKey.Delete:
                    if (player.TargetIndex > -1)
                        DeleteItem(player.TargetIndex);
                    break;

                case VirtualKey.Space:
                    TogglePlayPause();
                    break;

                default:
                    break;
            }
        }

        private void ScrollPlaylistItemIntoView(object sender, SelectionChangedEventArgs e)
        {
            var listView = (ListView)sender;
            listView.ScrollIntoView(listView.SelectedItem);
        }

        private void ProgressBarSeek(object sender, PointerRoutedEventArgs e)
        {
            var bar = (UIElement)sender;
            PointerPoint pointer = e.GetCurrentPoint(bar);
            Point position = pointer.Position;
            var size = bar.RenderSize;
            var ratio = (position.X / size.Width);
            if (!player.IsPlaying)
                player.PlayProgress = ratio * 100;
            player.SeekFromRatio(ratio);
        }

        private void CursorShowHand()
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Hand, 1);
        }

        private void CursorShowArrow()
        {
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(Windows.UI.Core.CoreCursorType.Arrow, 1);
        }

        // Making Toast
        private static XmlDocument CreateToast(string msg)
        {
            var xDoc = new XDocument(
                new XElement("toast",
                    new XAttribute("duration", "short"),
                    new XElement("visual", 
                        new XElement("binding", new XAttribute("template", "ToastText02"), 
                            new XElement("text", "Onely", new XAttribute("id", 1)), 
                            new XElement("text", msg, new XAttribute("id", 2))
                        )
                    )
                )
            );

            var xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xDoc.ToString());
            return xmlDoc;
        }

        private static void ServeToast(string msg)
        {
            var toastXml = CreateToast(msg);
            var toaster = new ToastNotification(toastXml);
            var notifier = ToastNotificationManager.CreateToastNotifier();
            toaster.ExpirationTime = DateTime.Now.AddSeconds(4);
            notifier.Show(toaster);
        }

        // Showing info in Title Bar
        private void UpdateTitleBar()
        {
            ApplicationView appView = ApplicationView.GetForCurrentView();
            if (!String.IsNullOrEmpty(player.Playlist.Name))
                appView.Title = player.Playlist.Name;
            else
                appView.Title = String.Empty;
        }

        public event PropertyChangedEventHandler PropertyChanged;

    }
}
