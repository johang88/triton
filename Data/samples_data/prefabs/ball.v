{
   "Position":"0 0 0",
   "Orientation":"0 0 0 1",
   "Scale":"1 1 1",
   "Children":[

   ],
   "Components":[
      {
         "$type":"Triton.Game.World.Components.MeshRenderer, Triton.Game",
         "CastShadows":true,
         "Mesh":"/models/sphere"
      },
      {
         "$type": "Triton.Physics.Components.RigidBodyComponent, Triton.Physics",
		 "ColliderShape": {
           "$type": "Triton.Physics.Shapes.SphereColliderShape, Triton.Physics",
           "Radius": 0.5
         },
		 "Mass": 1.0
      }
   ]
}