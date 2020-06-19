using System.Collections.Generic;

namespace Ambermoon.Data
{
    public interface IFileContainer
    {
        string Name { get; }
        Dictionary<int, IDataReader> Files { get; }
    }
}
