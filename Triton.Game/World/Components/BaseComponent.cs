using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Graphics;
using Triton.Input;

namespace Triton.Game.World.Components
{
    [DataContract]
    public class BaseComponent : IGameObjectComponent
    {
        public GameObject Owner { get; private set; }
        protected GameObjectManager World => Owner.World;
        protected Graphics.Stage Stage => Owner.World.Services.Get<Graphics.Stage>();
        protected GameObject Parent => Owner.Parent;
        protected Camera Camera => Stage.Camera;
        protected InputManager Input => Owner.World.Services.Get<InputManager>();
        protected Physics.World PhysicsWorld => Owner.World.Services.Get<Physics.World>();
        protected Resources.ResourceManager ResourceManager => Owner.World.Services.Get<Resources.ResourceManager>();

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
            var component = (BaseComponent)MemberwiseClone();
            component.Owner = null; // Detach

            return component;
        }
    }
}
