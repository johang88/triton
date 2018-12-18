{
   "Position":"0 0 0",
   "Orientation":"0 0 0 1",
   "Scale":"1 1 1",
   "Children":[
   ],
   "Components":[
      {
         "$type":"Triton.Graphics.Components.MeshComponent, Triton.Graphics",
         "CastShadows":true,
         "Mesh":"/models/city"
      },
      {
         "$type": "Triton.Physics.Components.RigidBodyComponent, Triton.Physics",
		 "ColliderShape": {
           "$type": "Triton.Physics.Shapes.MeshColliderShape, Triton.Physics",
           "Mesh": "/collision/city"
         },
		 "RigidBodyType": 2
      }
   ]
}