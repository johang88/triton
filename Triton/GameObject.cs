using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Triton.Collections;

namespace Triton
{
    [DataContract]
    public class GameObject : ICloneable
    {
        [DataMember] public TrackingCollection<GameObject> Children { get; } = new TrackingCollection<GameObject>();
        [DataMember] public TrackingCollection<IGameObjectComponent> Components { get; } = new TrackingCollection<IGameObjectComponent>();
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

        private void Components_OnAdd(IGameObjectComponent component)
        {
            if (component == null) throw new ArgumentNullException("component");
            if (component.Owner != null) throw new InvalidOperationException("Component already attached to a game object");

            component.OnAttached(this);

            if (Active)
            {
                component.OnDeactivate();
            }
        }

        private void Components_OnRemove(IGameObjectComponent component)
        {
            component.OnDeactivate();
            component.OnDetached();
        }

        public TComponentType GetComponent<TComponentType>() where TComponentType : IGameObjectComponent
            => (TComponentType)Components.First(c => c is TComponentType);

        public bool HasComponent<TComponentType>() where TComponentType : IGameObjectComponent
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
                var clonedComponent = (IGameObjectComponent)component.Clone();
                gameObject.Components.Add(clonedComponent);
            }

            foreach (var child in Children)
            {
                gameObject.Children.Add((GameObject)child.Clone());
            }

            return gameObject;
        }

        public void GetWorldMatrix(out Matrix4 world)
        {
            var scale = Matrix4.Scale(Scale);
            Matrix4.Rotate(ref Orientation, out var rotation);
            Matrix4.CreateTranslation(ref Position, out var translation);

            Matrix4.Mult(ref scale, ref rotation, out var rotationScale);
            Matrix4.Mult(ref rotationScale, ref translation, out world);
        }
    }
}
