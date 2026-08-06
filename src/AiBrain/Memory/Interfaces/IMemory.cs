namespace AI_Brain.Memory.Interfaces
{
    public interface IMemory<T>
    {
        void Store(T data);
        T Retrieve(object query);
        void Decay();
    }

    public interface ISensoryMemory : IMemory<double[]> { }
    public interface IWorkingMemory : IMemory<object> { }
    public interface IEpisodicMemory : IMemory<Experience> { }
    public interface ISemanticMemory : IMemory<Concept> { }
    public interface IProceduralMemory : IMemory<Skill> { }

    public class Experience
    {
        public DateTime Timestamp { get; set; }
        public double[] State { get; set; }
        public double[] Action { get; set; }
        public double Reward { get; set; }
    }

    public class Concept
    {
        public string Name { get; set; }
        public double[] Vector { get; set; }
        public List<string> Associations { get; set; } = new List<string>();
    }

    public class Skill
    {
        public string Name { get; set; }
        public Action<object> Execute { get; set; }
    }
}
