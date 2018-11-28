using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Triton
{
    public class Services
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public void Add<T>(T service) where T : class
            => _services.Add(typeof(T), service ?? throw new ArgumentNullException(nameof(service)));

        public T Get<T>() where T : class
            => (T)_services[typeof(T)];
    }
}
