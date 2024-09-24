using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVP_2_0_Server
{
    class Model
    {
        public ulong Id;
        ClientConnection parent;

        public Model(ulong modelId, ClientConnection modelParent)
        {
            Id = modelId;
            parent = modelParent;
            Console.WriteLine($"Created {Id} model for {parent.Id} client!");
        }

        ~Model()
        {
            Console.WriteLine($"Model {Id} destructor");
        }
    }
}
