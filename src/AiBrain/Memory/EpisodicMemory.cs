using AI_Brain.Memory.Interfaces;

namespace AI_Brain.Memory
{
    public class EpisodicMemory : IEpisodicMemory
    {
        private List<Experience> _experiences = new List<Experience>();

        public void Store(Experience data)
        {
            _experiences.Add(data);
        }

        public Experience Retrieve(object query)
        {
            // Simple retrieval by timestamp or similarity (not implemented here)
            return _experiences.LastOrDefault();
        }

        public void Decay()
        {
            if (_experiences.Count > 1000)
            {
                _experiences.RemoveAt(0);
            }
        }
    }
}
