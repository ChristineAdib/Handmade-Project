using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Interfaces
{
    public interface IChunkService
    {
        IReadOnlyList<string> Split(string text);
    }
}
