using Microsoft.Data.Sqlite;
using System;

namespace Orange_Music_Player
{
    public static class PlaylistStatic
    {
        public static string DefaultDBName = "|Onely-LastViewedPlaylist#|";
        public static PlaylistReferenceCollection<PlaylistReference> GetSavedPlaylists()
        {
            using (SqliteConnection db = OnelyDB.Open())
            {

                PlaylistReferenceCollection<PlaylistReference> playlists = new PlaylistReferenceCollection<PlaylistReference>(); 
                SqliteCommand command = new SqliteCommand
                {
                    Connection = db,
                    CommandText = "SELECT id, name FROM playlists WHERE name != @Default ORDER BY name"
                };
                command.Parameters.AddWithValue("@Default", DefaultDBName);
                var res = OnelyDB.ExecuteReader(command);

                while (res.Read())
                {
                    playlists.Add(new PlaylistReference(res.GetInt32(0), res.GetString(1)));
                }

                db.Close();

                return playlists;
            }
        }

        public static void DeletePlaylistById(int id)
        {
            using (SqliteConnection db = OnelyDB.Open())
            {
                PlaylistItemStatic.DeleteBasedOnPlaylistId(id, db);
                SqliteCommand command = new SqliteCommand
                {
                    Connection = db,
                    CommandText = "DELETE FROM playlists WHERE id=@ID"
                };
                command.Parameters.AddWithValue("@ID", id);
                OnelyDB.ExecuteReader(command);

                OnelyDB.Close(db);
            }
        }

        public static string RetrieveNameById(int id)
        {
            using (SqliteConnection db = OnelyDB.Open())
            {
                SqliteCommand command = new SqliteCommand
                {
                    Connection = db,
                    CommandText = "SELECT name FROM playlists WHERE id=@ID"
                };
                command.Parameters.AddWithValue("@ID", id);
                var res = command.ExecuteReader();

                if (!res.HasRows)
                {
                    OnelyDB.Close(db);
                    return null;
                }
                
                var name = String.Empty;
                while (res.Read())
                {
                    name = res.GetString(0);
                }
                return name;
            }
        }

        public static int Save(Playlist playlist, string name)
        {
            using (SqliteConnection db = OnelyDB.Open())
            {
                SqliteCommand command;
                if (playlist.Name == null)
                {
                    // Is name taken?
                    command = new SqliteCommand
                    {
                        Connection = db,
                        CommandText = "SELECT id FROM playlists WHERE name=@Name"
                    };
                    command.Parameters.AddWithValue("@Name", name);
                    var res = OnelyDB.ExecuteReader(command);
                    if (res.HasRows)
                    {
                        return -1;
                    }

                    command = new SqliteCommand
                    {
                        Connection = db,
                        CommandText = "INSERT INTO playlists(name) VALUES (@Name)"
                    };
                    command.Parameters.AddWithValue("@Name", name);
                    OnelyDB.ExecuteReader(command);

                    playlist.Id = OnelyDB.GetLastInsertId(db);
                    playlist.Name = name;
                }
                else
                {
                    if (name != playlist.Name)
                    {
                        command = new SqliteCommand
                        {
                            Connection = db,
                            CommandText = "UPDATE playlists SET name=@Name WHERE id=@ID"
                        };
                        command.Parameters.AddWithValue("@Name", name);
                        command.Parameters.AddWithValue("@ID", playlist.Id);
                        OnelyDB.ExecuteReader(command);
                        playlist.Name = name;
                    }
                    
                    PlaylistItemStatic.DeleteBasedOnPlaylistId(playlist.Id, db);
                }
                int count = 0;
                foreach (var item in playlist.Items)
                {
                    PlaylistItemStatic.Save(item, playlist.Id, count, db);
                    count++;
                }
                OnelyDB.Close(db);
                return playlist.Id;
            }
        }

        public static int GetDefaultPlaylistId()
        {
            using (SqliteConnection db = OnelyDB.Open())
            {
                int id = -1;
                SqliteCommand command = new SqliteCommand
                {
                    Connection = db,
                    CommandText = "SELECT id FROM playlists WHERE name=@Default"
                };
                command.Parameters.AddWithValue("@Default", DefaultDBName);
                var res = OnelyDB.ExecuteReader(command);
                while (res.Read())
                {
                    id = res.GetInt32(0);
                }
                return id;
            }
        }

        public static bool IsDefaultDB(string name)
        {
            if (name == DefaultDBName)
                return true;
            return false;
        }
    }
}
