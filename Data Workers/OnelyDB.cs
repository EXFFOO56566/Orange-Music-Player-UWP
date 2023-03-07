using Microsoft.Data.Sqlite;
using System;
using System.Diagnostics;

namespace Orange_Music_Player
{
    public class OnelyDB
    {
        private static string PlaylistDBName = "OnelyPlaylists.db";
        public static void InitDb()
        {
            using (SqliteConnection db = Open())
            {

                string tableCommand = "CREATE TABLE IF NOT EXISTS playlists (id INTEGER PRIMARY KEY AUTOINCREMENT, name VARCHAR(256) NOT NULL, created_at DATETIME DEFAULT CURRENT_TIMESTAMP, CONSTRAINT unique_name UNIQUE (name))";
                SqliteCommand createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteReader();

                tableCommand = "CREATE TABLE IF NOT EXISTS playlist_items (id INTEGER PRIMARY KEY AUTOINCREMENT, playlist_id INTEGER, audio_path VARCHAR(256) NOT NULL, cover_path VARCHAR(256) NULL, sort_order INTEGER, FOREIGN KEY(playlist_id) REFERENCES playlists(id))";
                createTable = new SqliteCommand(tableCommand, db);
                createTable.ExecuteReader();

                Close(db);
            }
        }

        public static SqliteConnection Open()
        {
            SqliteConnection db = new SqliteConnection("Filename=" + PlaylistDBName);
            db.Open();
            return db;
        }

        public static void Close(SqliteConnection db)
        {
            db.Close();
        }

        public static SqliteDataReader ExecuteReader(SqliteCommand command)
        {
            try {
                return command.ExecuteReader();
            } catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return null;
            }
        }

        public static int GetLastInsertId(SqliteConnection db)
        {
            SqliteCommand command = new SqliteCommand
            {
                Connection = db,
                CommandText = "SELECT last_insert_rowid()"
            };
            try
            {
                int id = (int)(long)command.ExecuteScalar();
                return id;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                return -1;
            }
        }
    }
}