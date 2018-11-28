using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Common.Collections;

namespace Triton.Game.World
{
    [DataContract]
    public class GameObject : ICloneable
	{
        [DataMember] public TrackingCollection<GameObject> Children { get; } = new TrackingCollection<GameObject>();
        [DataMember] public TrackingCollection<IComponent> Components { get; } = new TrackingCollection<IComponent>();
		public GameObject Parent { get; private set; }

        public bool Active => World != null;

        [DataMember] public Vector3 Position = Vector3.Zero;
        [DataMember] public Quaternion Orientation = Quaternion.Identity;
        [DataMember] public Vector3 Scale = new Vector3(1, 1, 1);

        private GameObjectManager _world;
        public GameObjectManager World
        {
            get { return _world; }
            internal set
            {
                if (value == null)
                {
                    if (_world == null) return;

                    OnDetached();
                    _world = null;
                }
                else
                {
                    if (_world != null) throw new InvalidOperationException("GameObject already attached to a World");

                    _world = value;
                    OnAttached();
                }
            }
        }

        public GameObject()
        {
            Children.OnAdd += Children_OnAdd;
            Children.OnRemove += Children_OnRemove;

            Components.OnAdd += Components_OnAdd;
            Components.OnRemove += Components_OnRemove;
        }

        private void OnAttached()
		{
            foreach (var component in Components)
            {
                component.OnActivate();
            }

            foreach (var child in Children)
            {
                child.World = World;
            }
		}

		private void OnDetached()
		{
            foreach (var component in Components)
            {
                component.OnDeactivate();
            }

            foreach (var child in Children)
            {
                child.World = null;
            }
		}

		public void Update(float dt)
		{
            foreach (var component in Components)
            {
                component.Update(dt);
            }

            foreach (var child in Children)
            {
                child.Update(dt);
            }
		}
        
        private void Children_OnAdd(GameObject child)
        {
            if (Active)
            {
                child.World = World;
            }
        }

        private void Children_OnRemove(GameObject child)
        {
            child.World = null;
        }

        private void Components_OnAdd(IComponent component)
        {
            if (component == null) throw new ArgumentNullException("component");
            if (component.Owner != null) throw new InvalidOperationException("Component already attached to a game object");

            component.OnAttached(this);

            if (Active)
            {
                component.OnDeactivate(); 
            }
        }

        private void Components_OnRemove(IComponent component)
        {
            component.OnDeactivate();
            component.OnDetached();
        }

        public TComponentType GetComponent<TComponentType>() 
            => (TComponentType)Components.First(c => c is TComponentType);

        public bool HasComponent<TComponentType>()
            => Components.Any(c => c is TComponentType);

        public object Clone()
        {
            var gameObject = new GameObject
            {
                Position = Position,
                Orientation = Orientation,
                Scale = Scale
            };

            foreach (var component in Components)
            {
                gameObject.Components.Add((IComponent)component.Clone());
            }

            foreach (var child in Children)
            {
                gameObject.Children.Add((GameObject)child.Clone());
            }

            return gameObject;
        }
    }
}
