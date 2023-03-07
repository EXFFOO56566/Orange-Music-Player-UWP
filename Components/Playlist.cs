using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace Orange_Music_Player
{
    public class Playlist :  NotificationBase
    {
        // Notifying properties
        private int id = -1;
        public int Id {
            get
            {
                return this.id;
            }
            set
            {
                SetProperty(this.id, value, () => this.id = value);
            }
        }
        private string name = String.Empty;
        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                SetProperty(this.name, value, () => this.name = value);
            }
        }
        private bool hasItems = false;
        public bool HasItems
        {
            get
            {
                return this.hasItems;
            }
            set
            {
                SetProperty(this.hasItems, value, () => this.hasItems = value);
            }
        }
        public PlaylistItem SelectedItem
        {
            get
            {
                if (SelectedIndex > -1)
                    try
                    {
                        return this.Items[SelectedIndex];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                return null;
            }
        }
        private bool shuffle;
        public bool Shuffle
        {
            get
            {
                return this.shuffle;
            }
            set
            {
                if (value && value != this.shuffle)
                {
                    this.GenerateRandomIndexes();
                }
                SetProperty(this.shuffle, value, () => this.shuffle = value);
            }
        }
        public ObservableCollection<PlaylistItem> Items { get; set; }

        // Other properties
        private List<AlbumCover> AlbumCovers;
        public List<int> RandomIndexes { get; set; }
        public int SelectedIndex { get; set; }
        public int RandomIndex {get; set; }
        public PlaylistItem HeldItem;

        // Constructor
        public Playlist()
        {
            Items = new ObservableCollection<PlaylistItem>();
            SelectedIndex = 0;
            RandomIndex = 0;
            HeldItem = null;
            Shuffle = false;
            RandomIndexes = new List<int>();
            AlbumCovers = new List<AlbumCover>();
        }

        public void PrepareForReorder()
        {
            HeldItem = SelectedItem;
        }

        public void SetSelectedIndexAfterReorder()
        {
            if (HeldItem != null)
            {
                SelectedIndex = Items.IndexOf(HeldItem);
                HeldItem = null;
            }
        }

        public void Add(PlaylistItem item)
        {
            Items.Add(item);
        }

        public void RemoveAt(int index)
        {
            PlaylistItem item;
            try
            {
                item = Items[index];
            } catch(IndexOutOfRangeException e)
            {
                throw e;
            }

            item.Source.Dispose();
            Items.RemoveAt(index);
            if (Items.Count() < 1)
            {
                HasItems = false;
                return;
            }
            if (SelectedIndex >= index && SelectedIndex > 0)
            {
                SelectedIndex--;
            }
            if (Shuffle)
            {
                RandomIndexes.RemoveAt(RandomIndexes.IndexOf(index));
                foreach (var ind in RandomIndexes.Select((value, i) => new { i, value }))
                {
                    if (ind.value > index)
                    {
                        RandomIndexes[ind.i] = ind.value - 1;
                    }
                }
            }
        }

        public void Clear()
        {
            foreach (var i in Items)
            {
                i.Source.Dispose();
            }
            Items.Clear();
            AlbumCovers.Clear();
            SelectedIndex = -1;
            HasItems = false;
        }

        public void UpdateAfterDeletedFromDb()
        {
            Id = -1;
            Name = null;
        }

        public PlaylistItem SetCurrentPositionAndGetItem(int index)
        {
            PlaylistItem item;
            try
            {
                item = Items[index];
            } catch(IndexOutOfRangeException e)
            {
                throw e;
            }

            SelectedIndex = index;
            return item;
        }

        public int GetNextIndex()
        {
            int next;
            if (Shuffle)
            {
                next = RandomIndex + 1;
                if (next < RandomIndexes.Count)
                {
                    RandomIndex = next;
                    return RandomIndexes[next];
                }
                return -1;
            }
            next = SelectedIndex + 1;
            if (next < Items.Count())
                return next;
            return -1;
        }

        public int GetPreviousIndex()
        {
            int prev;
            if (Shuffle)
            {
                prev = RandomIndex - 1;
                if (prev > -1)
                {
                    RandomIndex = prev;
                    return RandomIndexes[prev];
                } 
                return -1;
            }
            prev = SelectedIndex - 1;
            if (prev > -1)
                return prev;
            return -1;
        }

        public int Reset()
        {
            RandomIndex = 0;
            if (Shuffle)
            {
                GenerateRandomIndexes();
                return RandomIndexes[0];
            }
            return 0;
        }

        private void GenerateRandomIndexes()
        {
            if (Items.Count() < 1)
                return;
            var list = new List<int>(Enumerable.Range(0, Items.Count() - 1));
            // This is a cheap and not totally random way to do this, but it works ok
            RandomIndexes = list.OrderBy(a => Guid.NewGuid()).ToList();
        }

        private AlbumCover GetExistingAlbumCover(string path)
        {
            foreach(var i in AlbumCovers)
            {
                if (i.CoverPath == path)
                    return i;
            }
            return null;
        }

        // Loading From DB
        public async Task<bool> LoadDefault()
        {
            var id = PlaylistStatic.GetDefaultPlaylistId();
            bool loaded = await ClearAndLoadNew(id);
            Name = null;
            Id = -1;
            return loaded;
        }

        public async Task<bool> LoadNew(int id)
        {
            var name = PlaylistStatic.RetrieveNameById(id);
            if (name == null)
                return false;
            Name = name;
            Id = id;

            List<PlaylistItem> items = await PlaylistItemStatic.RetrievePlaylistItemsByPlaylistId(id);
   
            foreach (var item in items)
            {
                Add(item);
            }
            if (Shuffle)
            {
                GenerateRandomIndexes();
            }
            if (Items.Count() > 0)
            {
                HasItems = true;
                if (SelectedIndex == -1)
                    SelectedIndex = 0;

            } else
                HasItems = false;
            return true;
        }

        public async Task<bool> ClearAndLoadNew(int id)
        {
            Clear();
            return await LoadNew(id);
        }

        // Loading files
        public async Task<bool> LoadFiles(IEnumerable<StorageFile> audioFiles, IEnumerable<StorageFile> imageFiles)
        {
            AlbumCover cover;
            StorageFile imageFile = imageFiles.FirstOrDefault();
            if (imageFile != null)
            {
                cover = GetExistingAlbumCover(imageFile.Path);
                if (cover == null)
                {
                    cover = await AlbumCover.FromStorageFile(imageFile);
                    if (cover != null)
                    {
                        AlbumCovers.Add(cover);
                        string faToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(imageFile);
                    }
                }
            } else
            {
                cover = null;
            }

            foreach (var file in audioFiles)
            {
                var item = await PlaylistItemStatic.LoadFromFile(file);

                Add(item);
                string faToken = Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file);
                if (cover != null)
                {
                    item.MainCover = cover;
                }
            }
            if (Shuffle)
            {
                GenerateRandomIndexes();
            }

            if (Items.Count() > 0)
                HasItems = true;
            else
                HasItems = false;

            if (SelectedIndex == -1)
            {
                SelectedIndex = 0;
            }
            return true;
        }

        // Saving
        public int Save(string name)
        {
            return PlaylistStatic.Save(this, name);
        }

        public int SaveAs(string name)
        {
            var currentName = Name;
            Name = null;
            var id = Save(name);
            if (id == -1)
                Name = currentName;
            return id;
        }

        public void SaveDefault()
        {
            var currentId = Id;
            var currentName = Name;
            Id = PlaylistStatic.GetDefaultPlaylistId();
            Name = PlaylistStatic.DefaultDBName;
            Save(Name);
            Id = currentId;
            currentName = Name;
        }
    }
}
