using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Orange_Music_Player.AttachedProperties
{
    public class Ap : DependencyObject
    {
        // PlaylistId
        public static readonly DependencyProperty PlaylistIdProperty = DependencyProperty.RegisterAttached("PlaylistId", typeof(int), typeof(Ap), new PropertyMetadata(""));

        public static int GetPlaylistId(DependencyObject d)
        {
            return (int)d.GetValue(PlaylistIdProperty);
        }

        public static void SetPlaylistId(DependencyObject d, int value)
        {
            d.SetValue(PlaylistIdProperty, value);
        }

        // PlaylistName
        public static readonly DependencyProperty PlaylistNameProperty = DependencyProperty.RegisterAttached("PlaylistName", typeof(string), typeof(Ap), new PropertyMetadata(""));

        public static string GetPlaylistName(DependencyObject d)
        {
            return (string)d.GetValue(PlaylistNameProperty);
        }

        public static void SetPlaylistName(DependencyObject d, string value)
        {
            d.SetValue(PlaylistNameProperty, value);
        }
    }
}
