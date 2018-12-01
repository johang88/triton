using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Triton
{
    [DataContract]
    public abstract class GameObjectComponent : IGameObjectComponent
    {
        public GameObject Owner { get; private set; }

        public virtual void OnAttached(GameObject owner)
            => Owner = owner ?? throw new ArgumentNullException("owner");

        public virtual void OnActivate()
        {
        }

        public virtual void OnDeactivate()
        {
        }

        public virtual void OnDetached()
        {
        }

        public virtual void Update(float dt)
        {
        }

        public object Clone()
        {
            var component = (GameObjectComponent)MemberwiseClone();
            component.Owner = null; // Detach

            return component;
        }
    }
}
