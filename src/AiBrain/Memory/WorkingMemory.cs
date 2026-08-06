using AI_Brain.Memory.Interfaces;

namespace AI_Brain.Memory
{
    public class WorkingMemory : IWorkingMemory
    {
        private Dictionary<string, object> _activeItems = new Dictionary<string, object>();
        private int _maxItems;

        public WorkingMemory(int maxItems = 7)
        {
            _maxItems = maxItems;
        }

        public void Store(object data)
        {
            // Simple implementation: use hash as key
            string key = data.GetHashCode().ToString();
            _activeItems[key] = data;
            
            if (_activeItems.Count > _maxItems)
            {
                var firstKey = _activeItems.Keys.First();
                _activeItems.Remove(firstKey);
            }
        }

        public object Retrieve(object query)
        {
            if (query is string key && _activeItems.ContainsKey(key))
            {
                return _activeItems[key];
            }
            return null;
        }

        public void Decay()
        {
            if (_activeItems.Count > 0)
            {
                var firstKey = _activeItems.Keys.First();
                _activeItems.Remove(firstKey);
            }
        }
    }
}
