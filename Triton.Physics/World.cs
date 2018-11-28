using BulletSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Triton.Common;
using Triton.Graphics;

namespace Triton.Physics
{
    public class World : IDisposable
    {
        private DiscreteDynamicsWorld _world;
        private CollisionDispatcher _dispatcher;
        private DbvtBroadphase _broadphase;
        private CollisionConfiguration _collisionConfiguration;

        private readonly ResourceManager _resourceManager;
        private readonly List<Body> _bodies = new List<Body>();
        private readonly DebugDrawer _debugDraw;

        private Vector3[] _debugColors = new Vector3[]
        {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, 0, 1),
            new Vector3(1, 1, 0),
            new Vector3(1, 0, 1),
            new Vector3(0, 1, 1),
        };

        public Vector3 Gravity
        {
            get { return Conversion.ToTritonVector(_world.Gravity); }
            set { _world.Gravity = Conversion.ToBulletVector(ref value); }
        }

        public World(Backend backend, ResourceManager resourceManager)
        {
            _resourceManager = resourceManager;

            _collisionConfiguration = new DefaultCollisionConfiguration();
            _dispatcher = new CollisionDispatcher(_collisionConfiguration);

            _broadphase = new DbvtBroadphase();
            _world = new DiscreteDynamicsWorld(_dispatcher, _broadphase, null, _collisionConfiguration);
            _world.Gravity = new BulletSharp.Math.Vector3(0, -10, 0);

            _debugDraw = new DebugDrawer(backend, resourceManager);

            _world.DebugDrawer = _debugDraw;
        }

        public void Dispose()
        {
            _world.Dispose();
            _broadphase.Dispose();
            _dispatcher.Dispose();
            _collisionConfiguration.Dispose();
        }

        public void Update(float stepSize)
        {
            foreach (var body in _bodies)
            {
                body.Update(stepSize);
            }

            _world.StepSimulation(stepSize, 2);

            var numManifolds = _world.Dispatcher.NumManifolds;
            for (var i = 0; i < numManifolds; i++)
            {
                var contactManifold = _world.Dispatcher.GetManifoldByIndexInternal(i);

                if (contactManifold.NumContacts == 0)
                    continue;

                if (contactManifold.Body0.UserObject is Body bodyA && contactManifold.Body1.UserObject is Body bodyB)
                {
                    bodyA.OnCollision(bodyB);
                    bodyB.OnCollision(bodyA);
                }
            }
        }

        Body CreateRigidBody(CollisionShape shape, Matrix4 startTransform, BodyFlags flags, float mass)
        {
            if (flags.HasFlag(BodyFlags.Static))
            {
                mass = 0.0f;
            }

            BulletSharp.Math.Vector3 localInertia = BulletSharp.Math.Vector3.Zero;
            shape.CalculateLocalInertia(mass, out localInertia);

            DefaultMotionState myMotionState = new DefaultMotionState(Conversion.ToBulletMatrix(ref startTransform));

            RigidBodyConstructionInfo rbInfo = new RigidBodyConstructionInfo(mass, myMotionState, shape, localInertia);
            RigidBody rigidBody = new RigidBody(rbInfo);

            var body = new Body(rigidBody)
            {
                Flags = flags
            };

            rigidBody.UserObject = body;
            _bodies.Add(body);
            _world.AddRigidBody(rigidBody);

            return body;
        }

        public Body CreateMeshBody(Resources.Mesh mesh, Vector3 position, float mass, BodyFlags flags = BodyFlags.None)
        {
            _resourceManager.AddReference(mesh);

            var shape = mesh.Shape;

            var body = CreateRigidBody(shape, Matrix4.CreateTranslation(position), flags, mass);
            body.Mesh = mesh;

            return body;
        }

        public Body CreateSphereBody(float radius, Vector3 position, float mass, BodyFlags flags = BodyFlags.None)
        {
            var shape = new SphereShape(radius);
            return CreateRigidBody(shape, Matrix4.CreateTranslation(position), flags, mass);
        }

        public Body CreateBoxBody(float length, float height, float width, Vector3 position, float mass, BodyFlags flags = BodyFlags.None)
        {
            var shape = new BoxShape(length, height, width);
            return CreateRigidBody(shape, Matrix4.CreateTranslation(position), flags, mass);
        }

        public CharacterController CreateCharacterController(float length, float radius)
        {
            var controller = new CharacterController(_world, radius, length);

            _bodies.Add(controller);

            return controller;
        }

        public void RemoveBody(Body body)
        {
            _world.RemoveRigidBody(body.RigidBody);
            _bodies.Remove(body);

            if (body.Mesh != null)
            {
                _resourceManager.Unload(body.Mesh);
            }

            body.Dispose();

            if (body.RigidBody != null)
            {
                body.Dispose();
                body.RigidBody.Dispose();
            }
        }

        public void DrawDebugInfo(Camera camera)
        {
            _world.DebugDrawWorld();
            _debugDraw.Render(camera);
        }

        public bool Raycast(Vector3 from, Vector3 to, RaycastCallback callback, out Body body, out Vector3 normal, out float fraction)
        {
            var btTo = Conversion.ToBulletVector(ref from);
            var btFrom = Conversion.ToBulletVector(ref to);

            body = null;
            normal = Vector3.Zero;
            fraction = 0.0f;

            using (var allResults = new AllHitsRayResultCallback(btFrom, btTo))
            {
                _world.RayTest(btFrom, btTo, allResults);

                if (!allResults.HasHit)
                {
                    return false;
                }

                float closesHitFraction = float.MaxValue;
                for (var i = 0; i < allResults.HitFractions.Count; i++)
                {
                    if (allResults.CollisionObjects[i].UserObject is Body hitBody)
                    {
                        var hitNormal = Conversion.ToTritonVector(allResults.HitNormalWorld[i]);
                        if (callback?.Invoke(hitBody, hitNormal, allResults.HitFractions[i]) == true && allResults.HitFractions[i] < closesHitFraction)
                        {
                            closesHitFraction = allResults.HitFractions[i];
                            normal = hitNormal;
                            body = hitBody;
                        }
                    }
                }

                if (body != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
