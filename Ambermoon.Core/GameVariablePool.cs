using System.Collections.Generic;

namespace Ambermoon
{
    public class GameVariablePool
    {
        readonly Dictionary<uint, int> variables = new Dictionary<uint, int>();

        public void Clear()
        {
            variables.Clear();
        }

        public int this[uint index]
        {
            get
            {
                if (!variables.ContainsKey(index))
                    return variables[index] = 0;

                return variables[index];
            }
            set
            {
                variables[index] = value;
            }
        }
    }
}
