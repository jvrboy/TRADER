using AI_Brain.Memory.Interfaces;

namespace AI_Brain.Memory
{
    public class SensoryMemory : ISensoryMemory
    {
        private Queue<double[]> _buffer = new Queue<double[]>();
        private int _capacity;

        public SensoryMemory(int capacity = 10)
        {
            _capacity = capacity;
        }

        public void Store(double[] data)
        {
            _buffer.Enqueue(data);
            if (_buffer.Count > _capacity)
            {
                _buffer.Dequeue();
            }
        }

        public double[] Retrieve(object query)
        {
            return _buffer.LastOrDefault() ?? Array.Empty<double>();
        }

        public void Decay()
        {
            if (_buffer.Count > 0) _buffer.Dequeue();
        }
    }
}
