using System;
using System.IO;
using TagLibUWP;
using Windows.Media.Core;
using Windows.UI;
using Windows.UI.Xaml.Media.Imaging;

namespace Orange_Music_Player
{
    public class PlaylistItem
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Genre { get; set; }
        public uint Track { get; set; }
        public BitmapImage Cover { get; set; }
        private AlbumCover mainCover;
        public AlbumCover MainCover {
            get => this.mainCover;
            set
            {
                this.SetAlbumCover(ref value);
            }
        }
        public MediaSource Source { get; }
        public string MusicPath { get; }

        public PlaylistItem(string p, MediaSource s, Tag t, BitmapImage c)
        {
            MusicPath = p;
            Source = s;
            Title = t.Title;
            Artist = t.Artist;
            Album = t.Album;
            Track = t.Track;
            Genre = t.Genre;
            Cover = c;
            FixEmptyFields();
        }

        private void FixEmptyFields()
        {
            if (String.IsNullOrEmpty(Title))
            {
                Title = Path.GetFileName(MusicPath);
            }
            if (String.IsNullOrEmpty(Artist))
            {
                Artist = "Unknown Artist";
            }
            if (String.IsNullOrEmpty(Album))
            {
                Album = "Unknown Album";
            }
        }

        public void SetAlbumCover(ref AlbumCover a)
        {
            mainCover = a;
            if (Cover == null)
            {
                Cover = mainCover.Cover;
            }
        }
    }
}
