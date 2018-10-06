using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;
using Triton.Graphics;
using Triton.Input;

namespace Triton.Game.World
{
    public class GameObjectManager
    {
        private readonly List<GameObject> GameObjects = new List<GameObject>();
        private readonly HashSet<GameObject> GameObjectsToAdd = new HashSet<GameObject>();
        private readonly HashSet<GameObject> GameObjectsToRemove = new HashSet<GameObject>();

        public Stage Stage { get; private set; }
        public InputManager InputManager { get; private set; }
        public ResourceManager ResourceManager { get; private set; }
        public Triton.Physics.World PhysicsWorld { get; private set; }

        public Graphics.Camera Camera { get; private set; }

        public GameObjectManager(Stage stage, InputManager inputManager, ResourceManager resourceManager, Triton.Physics.World physicsWorld, Graphics.Camera camera)
        {
            Stage = stage ?? throw new ArgumentNullException("stage");
            InputManager = inputManager ?? throw new ArgumentNullException("inputManager");
            ResourceManager = resourceManager ?? throw new ArgumentNullException("resourceManager");
            PhysicsWorld = physicsWorld ?? throw new ArgumentNullException("physicsWorld");
            Camera = camera ?? throw new ArgumentNullException("camera");
        }

        /// <summary>
        /// Removes all game objects from the world.
        /// They will be instantly removed, this function is not safe to call in <see cref="GameObject.Update"/> or <see cref="IComponent.Update"/>
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
            if (gameObject is Prefab) throw new InvalidProgramException("can not add a prefab directly to manager");

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

            foreach (var gameObject in GameObjects)
            {
                gameObject.Update(dt);
            }
        }
    }
}
