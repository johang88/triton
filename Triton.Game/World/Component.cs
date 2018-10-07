using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics;
using Triton.Input;

namespace Triton.Game.World
{
    [DataContract]
    public class Component : IComponent
    {
        public GameObject Owner { get; private set; }
        protected GameObjectManager World => Owner.World;
        protected Graphics.Stage Stage => Owner.World.Stage;
        protected GameObject Parent => Owner.Parent;
        protected Camera Camera => Owner.World.Camera;
        protected InputManager Input => Owner.World.InputManager;

        public virtual void OnAttached(GameObject owner)
        {
            Owner = owner ?? throw new ArgumentNullException("owner");
        }

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
            var component = (Component)MemberwiseClone();
            component.Owner = null; // Detach

            return component;
        }
    }
}
