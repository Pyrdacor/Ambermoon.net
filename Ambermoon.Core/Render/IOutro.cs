using Ambermoon.Data;
using System;

namespace Ambermoon.Render
{
    public interface IOutro
    {
        bool Active { get; }
        void Start(Savegame savegame);
        void Update(double deltaTime);
        void Destroy();
        void Click(bool right);
        void Abort();
    }

    public interface IOutroFactory
    {
        IOutro Create(Action finishAction);
    }
}
