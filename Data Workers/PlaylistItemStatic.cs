using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TagLibUWP;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Orange_Music_Player
{
    public static class PlaylistItemStatic
    {
        public async static Task<PlaylistItem> LoadFromFile(StorageFile file)
        {
            try
            {
                var source = MediaSource.CreateFromStorageFile(file);
                var info = await Task.Run(() => TagManager.ReadFile(file));
                var tags = info.Tag;
                BitmapImage cover = await GetCoverImageFromPicture(tags.Image);
                return new PlaylistItem(file.Path, source, tags, cover);
            } catch(Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public async static Task<PlaylistItem> LoadFromPath(string p)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(p);
                return await LoadFromFile(file);
            } catch(Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        private static async Task<BitmapImage> GetCoverImageFromPicture(Picture picture)
        {
            if (picture != null)
            {
                using (InMemoryRandomAccessStream ms = new InMemoryRandomAccessStream())
                {
                    using (DataWriter writer = new DataWriter(ms.GetOutputStreamAt(0)))
                    {
                        writer.WriteBytes(picture.Data);
                        await writer.StoreAsync();
                    }
                    var image = new BitmapImage();
                    await image.SetSourceAsync(ms);
                    return image;
                }
            }
            return null;
        }

        public static async Task<List<PlaylistItem>> RetrievePlaylistItemsByPlaylistId(int id)
        {
            using(SqliteConnection db = OnelyDB.Open())
            {
                SqliteCommand command = new SqliteCommand
                {
                    Connection = db,
                    CommandText = "SELECT audio_path, cover_path FROM playlist_items WHERE playlist_id=@ID ORDER BY sort_order ASC"
                };
                command.Parameters.AddWithValue("@ID", id);
                var res = OnelyDB.ExecuteReader(command);

                if (!res.HasRows)
                    return null;

                List<PlaylistItem> items = new List<PlaylistItem>();
                List<AlbumCover> covers = new List<AlbumCover>();

                while (res.Read())
                {
                    var item = await LoadFromPath(res.GetString(0));
                    if (item != null)
                    {
                        items.Add(item);
                        var coverPath = res.GetString(1);
                        if ((item.Cover == null) && (!String.IsNullOrEmpty(coverPath)))
                        {
                            AlbumCover cover = GetExistingAlbumCover(coverPath, covers);
                            if (cover == null)
                            {
                                cover = await AlbumCover.FromPath(coverPath);
                                if (cover != null)
                                {
                                    covers.Add(cover);
                                }
                            }
                            if (cover != null)
                                item.MainCover = cover;
                        }
                    }
                }
                covers.Clear();
                OnelyDB.Close(db);
                return items;
            }
        } 

        public static void DeleteBasedOnPlaylistId(int id, SqliteConnection db)
        {
            SqliteCommand command = new SqliteCommand
            {
                Connection = db,
                CommandText = "DELETE FROM playlist_items WHERE playlist_id=@ID"
            };
            command.Parameters.AddWithValue("@ID", id);
            OnelyDB.ExecuteReader(command);
        }

        private static AlbumCover GetExistingAlbumCover(string path, List<AlbumCover> covers)
        {
            foreach(var cover in covers)
            {
                if (path == cover.CoverPath)
                {
                    return cover;
                }
            }
            return null;
        }

        public static void Save(PlaylistItem item, int PlaylistId, int sortOrder, SqliteConnection db)
        {
            SqliteCommand command = new SqliteCommand
            {
                Connection = db,
                CommandText = "INSERT INTO playlist_items(playlist_id, audio_path, cover_path, sort_order) VALUES (@PlaylistId, @Path, @CoverPath, @Sort)"
            };
            var coverPath = String.Empty;
            if (item.MainCover != null)
            {
                coverPath = item.MainCover.CoverPath;
            }
            command.Parameters.AddWithValue("@PlaylistId", PlaylistId);
            command.Parameters.AddWithValue("@Path", item.MusicPath);
            command.Parameters.AddWithValue("@CoverPath", coverPath);
            command.Parameters.AddWithValue("@Sort", sortOrder);
            OnelyDB.ExecuteReader(command);
        }
    }
}
