using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton
{
    public class GameObjectManager
    {
        private readonly List<GameObject> GameObjects = new List<GameObject>();
        private readonly HashSet<GameObject> GameObjectsToAdd = new HashSet<GameObject>();
        private readonly HashSet<GameObject> GameObjectsToRemove = new HashSet<GameObject>();

        public Services Services { get; }

        public GameObjectManager(Services services)
        {
            Services = services;
        }

        /// <summary>
        /// Removes all game objects from the world.
        /// They will be instantly removed, this function is not safe to call in <see cref="GameObject.Update"/> or <see cref="IGameObjectComponent.Update"/>
        /// </summary>
        public void Clear()
        {
            foreach (var gameOject in GameObjects)
            {
                gameOject.World = null;
            }

            GameObjects.Clear();
        }

        /// <summary>
        /// Add a game object to the world, the game object will be added in the next frame.
        /// </summary>
        /// <param name="gameObject"></param>
        public void Add(GameObject gameObject)
        {
            if (gameObject == null) throw new ArgumentNullException(nameof(gameObject));

            GameObjectsToAdd.Add(gameObject);
        }

        /// <summary>
        /// Removes a game object from the world, the game object will be removed in the next frame.
        /// </summary>
        /// <param name="gameObject"></param>
        public void Remove(GameObject gameObject)
            => GameObjectsToRemove.Add(gameObject ?? throw new ArgumentNullException(nameof(gameObject)));

        /// <summary>
        /// Update all active game objects and their components
        /// Any game objects pending for removal / adding will be removed / added.
        /// </summary>
        /// <param name="stepSize"></param>
        public void Update(float dt)
        {
            HandlePendingGameObjects();

            foreach (var gameObject in GameObjects)
            {
                gameObject.Update(dt);
            }

            // We do both a Pre and Post step to prevent any issues where the Resoruce GC would trigger before the objects are attached to the world
            HandlePendingGameObjects();
        }

        private void HandlePendingGameObjects()
        {
            foreach (var gameObject in GameObjectsToAdd)
            {
                GameObjects.Add(gameObject);
                gameObject.World = this;
            }
            GameObjectsToAdd.Clear();

            foreach (var gameObject in GameObjectsToRemove)
            {
                if (GameObjects.Remove(gameObject))
                {
                    gameObject.World = null;
                }
            }
            GameObjectsToRemove.Clear();
        }
    }

}
