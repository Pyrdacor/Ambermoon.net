using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.Repository
{
    public class TextList : List<string>
    {
        public TextList()
        {
        }

        public TextList(IEnumerable<string> texts)
            : base(texts)
        {
        }
    }

    public class TextList<T> : TextList
    {
        public TextList(T associatedItem)
        {
            AssociatedItem = associatedItem;
        }

        public TextList(T associatedItem, IEnumerable<string> texts)
            : base(texts)
        {
            AssociatedItem = associatedItem;
        }

        public T AssociatedItem { get; }
    }
}
