namespace Ambermoon.Data.GameDataRepository
{
    public class ImageList : List<Image>
    {
        public ImageList()
        {
        }

        public ImageList(IEnumerable<Image> frames)
            : base(frames)
        {
        }
    }

    public class ImageList<T> : ImageList
    {
        public ImageList(T associatedItem)
        {
            AssociatedItem = associatedItem;
        }

        public ImageList(T associatedItem, IEnumerable<Image> frames)
            : base(frames)
        {
            AssociatedItem = associatedItem;
        }

        public T AssociatedItem { get; }
    }
}
