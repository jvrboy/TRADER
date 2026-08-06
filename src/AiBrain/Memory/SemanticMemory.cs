using AI_Brain.Memory.Interfaces;

namespace AI_Brain.Memory
{
    public class SemanticMemory : ISemanticMemory
    {
        private Dictionary<string, Concept> _knowledgeBase = new Dictionary<string, Concept>();

        public void Store(Concept data)
        {
            _knowledgeBase[data.Name] = data;
        }

        public Concept Retrieve(object query)
        {
            if (query is string name && _knowledgeBase.ContainsKey(name))
            {
                return _knowledgeBase[name];
            }
            return null;
        }

        public void Decay()
        {
            // Semantic memory decays very slowly or not at all in this model
        }
    }
}
