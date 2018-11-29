using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Tools
{
    public class Factory<TIdentifier, TBaseType>
    {
        public delegate TBaseType ConstructionDelegate();

        public Factory()
        {
            ConstructionFunctions = new Dictionary<TIdentifier, ConstructionDelegate>();
        }

        public void Add(TIdentifier id, ConstructionDelegate constructionFunction)
        {
            if (constructionFunction == null)
                throw new ArgumentNullException("constructionFunction");

            if (ConstructionFunctions.ContainsKey(id))
                throw new InvalidOperationException("there is already a construction function for id " + id.ToString());

            ConstructionFunctions.Add(id, constructionFunction);
        }

        public bool Exists(TIdentifier id)
        {
            return ConstructionFunctions.ContainsKey(id);
        }

        public bool Remove(TIdentifier id)
        {
            return ConstructionFunctions.Remove(id);
        }

        public TBaseType Create(TIdentifier id)
        {
            if (!ConstructionFunctions.ContainsKey(id))
                throw new KeyNotFoundException("there is no creator function for id " + id.ToString());

            return ConstructionFunctions[id]();
        }

        Dictionary<TIdentifier, ConstructionDelegate> ConstructionFunctions
        {
            get;
            set;
        }
    }
}
