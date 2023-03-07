using System.Collections.ObjectModel;

namespace Orange_Music_Player
{
    public class PlaylistReference
    {
        public int Id { get; }
        public string Name { get; }

        public PlaylistReference(int id, string name)
        {
            Id = id;
            Name = name;
        }
    }

    public class PlaylistReferenceCollection<T> : ObservableCollection<T>
    {

    }
}
