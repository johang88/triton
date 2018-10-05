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

        private readonly List<Body> Bodies = new List<Body>();
        private readonly DebugDrawer DebugDrawer;

        private Vector3[] DebugColors = new Vector3[]
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
            _collisionConfiguration = new DefaultCollisionConfiguration();
            _dispatcher = new CollisionDispatcher(_collisionConfiguration);

            _broadphase = new DbvtBroadphase();
            _world = new DiscreteDynamicsWorld(_dispatcher, _broadphase, null, _collisionConfiguration);
            _world.Gravity = new BulletSharp.Math.Vector3(0, -10, 0);

            DebugDrawer = new DebugDrawer(backend, resourceManager);

            _world.DebugDrawer = DebugDrawer;
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
            foreach (var body in Bodies)
            {
                body.Update(stepSize);
            }

            _world.StepSimulation(stepSize, 10);
        }

        RigidBody CreateRigidBody(CollisionShape shape, bool isStatic, Matrix4 startTransform, float mass)
        {
            BulletSharp.Math.Vector3 localInertia = BulletSharp.Math.Vector3.Zero;
            if (isStatic)
            {
                mass = 0.0f;
                shape.CalculateLocalInertia(mass, out localInertia);
            }

            DefaultMotionState myMotionState = new DefaultMotionState(Conversion.ToBulletMatrix(ref startTransform));

            RigidBodyConstructionInfo rbInfo = new RigidBodyConstructionInfo(mass, myMotionState, shape, localInertia);
            RigidBody body = new RigidBody(rbInfo);

            return body;
        }

        public Body CreateMeshBody(Resources.Mesh mesh, Vector3 position, bool isStatic = false, float mass = 1.0f)
        {
            var shape = mesh.Shape;
            var rigidBody = CreateRigidBody(shape, isStatic, Matrix4.CreateTranslation(position), mass);

            var body = new Body(rigidBody);

            Bodies.Add(body);

            _world.AddRigidBody(rigidBody);

            return body;
        }

        public Body CreateSphereBody(float radius, Vector3 position, bool isStatic = false, float mass = 1.0f)
        {
            var shape = new SphereShape(radius);
            var rigidBody = CreateRigidBody(shape, isStatic, Matrix4.CreateTranslation(position), mass);

            var body = new Body(rigidBody);

            Bodies.Add(body);

            _world.AddRigidBody(rigidBody);

            return body;
        }

        public Body CreateBoxBody(float length, float height, float width, Vector3 position, bool isStatic = false, float mass = 1.0f)
        {
            var shape = new BoxShape(length, height, width);
            var rigidBody = CreateRigidBody(shape, isStatic, Matrix4.CreateTranslation(position), mass);

            var body = new Body(rigidBody);

            Bodies.Add(body);

            _world.AddRigidBody(rigidBody);

            return body;
        }

        public CharacterController CreateCharacterController(float length, float radius)
        {
            var controller = new CharacterController(_world, radius, length);

            Bodies.Add(controller);

            return controller;
        }

        //public bool Raycast(Vector3 origin, Vector3 direction, RaycastCallback callback, out Body body, out Vector3 normal, out float fraction)
        //{
        //    RigidBody rigidBody;
        //    JVector jitterNormal;

        //    Jitter.Collision.RaycastCallback jitterCallback = (RigidBody body1, JVector normal1, float fraction1) =>
        //    {
        //        return callback((Body)body1.Tag, Conversion.ToTritonVector(ref normal1), fraction1);
        //    };

        //    var res = PhysicsWorld.CollisionSystem.Raycast(Conversion.ToBulletVector(ref origin), Conversion.ToBulletVector(ref direction), jitterCallback, out rigidBody, out jitterNormal, out fraction);

        //    normal = Conversion.ToTritonVector(ref jitterNormal);
        //    if (rigidBody != null)
        //    {
        //        body = (Body)rigidBody.Tag;
        //    }
        //    else
        //    {
        //        body = null;
        //    }

        //    return res;
        //}

        public void RemoveBody(Body body)
        {
            _world.RemoveRigidBody(body.RigidBody);
            Bodies.Remove(body);

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
            DebugDrawer.Render(camera);
        }
    }
}
