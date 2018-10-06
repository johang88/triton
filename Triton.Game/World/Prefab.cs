using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton.Game.World
{
    public class Prefab : GameObject, ICloneable
    {
        public GameObject Instantiate(GameObjectManager world)
        {
            var gameObject = (GameObject)Clone();
            world.Add(gameObject);

            return gameObject;
        }
    }
}
