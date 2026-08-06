using AI_Brain.Memory.Interfaces;

namespace AI_Brain.Memory
{
    public class ProceduralMemory : IProceduralMemory
    {
        private Dictionary<string, Skill> _skills = new Dictionary<string, Skill>();

        public void Store(Skill data)
        {
            _skills[data.Name] = data;
        }

        public Skill Retrieve(object query)
        {
            if (query is string name && _skills.ContainsKey(name))
            {
                return _skills[name];
            }
            return null;
        }

        public void Decay()
        {
            // Skills are persistent
        }
    }
}
