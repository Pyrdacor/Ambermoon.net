using Ambermoon.Data.GameDataRepository.Data;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;

namespace Ambermoon.Data.GameDataRepository
{
    public class ImageList : List<Image>, IIndexed, IMutableIndex
    {

        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        #endregion


        #region Constructors

        public ImageList()
        {
        }

        public ImageList(IEnumerable<Image> frames)
            : base(frames)
        {
        }

        #endregion

    }

    public class ImageList<T> : ImageList
    {

        #region Properties

        public T AssociatedItem { get; }

        #endregion


        #region Constructors

        public ImageList(T associatedItem)
        {
            AssociatedItem = associatedItem;
        }

        public ImageList(T associatedItem, IEnumerable<Image> frames)
            : base(frames)
        {
            AssociatedItem = associatedItem;
        }

        #endregion
        
    }

    public class ImageWithPaletteIndexList : List<ImageWithPaletteIndex>, IIndexed, IMutableIndex
    {

        #region Properties

        uint IMutableIndex.Index
        {
            get;
            set;
        }

        public uint Index => (this as IMutableIndex).Index;

        #endregion


        #region Constructors

        public ImageWithPaletteIndexList()
        {
        }

        public ImageWithPaletteIndexList(IEnumerable<ImageWithPaletteIndex> frames)
            : base(frames)
        {
        }

        #endregion

    }
}
