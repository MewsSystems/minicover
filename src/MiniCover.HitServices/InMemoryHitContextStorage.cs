using System.Collections.Generic;

namespace MiniCover.HitServices
{
    /// <summary>
    /// Buffering decorator over <see cref="IHitContextStorage"/>. Snapshots HitContext data
    /// in memory during Save, then delegates to the inner storage on Flush.
    /// </summary>
    public class InMemoryHitContextStorage : IHitContextStorage
    {
        private readonly IHitContextStorage _inner;
        private readonly Dictionary<string, (HitContext Context, string HitsPath)> _storage
            = new Dictionary<string, (HitContext, string)>();

        public InMemoryHitContextStorage(IHitContextStorage inner)
        {
            _inner = inner;
        }

        public void Save(HitContext hitContext, string hitsPath)
        {
            var snapshot = new HitContext(
                hitContext.Id,
                hitContext.AssemblyName,
                hitContext.ClassName,
                hitContext.MethodName,
                hitContext.Hits);

            lock (_storage)
            {
                _storage[snapshot.Id] = (snapshot, hitsPath);
            }
        }

        public bool Clear(string hitsPath)
        {
            lock (_storage)
            {
                _storage.Clear();
            }
            return _inner.Clear(hitsPath);
        }

        /// <summary>
        /// Delegates all buffered entries to the inner storage and clears the dictionary.
        /// The lock is held during I/O intentionally — Flush runs once after tests complete,
        /// not concurrently with Save. On partial failure, already-written entries remain in
        /// the dictionary so Flush can be retried.
        /// </summary>
        public void Flush()
        {
            lock (_storage)
            {
                foreach (var kvp in _storage)
                {
                    _inner.Save(kvp.Value.Context, kvp.Value.HitsPath);
                }

                _storage.Clear();
            }
        }
    }
}
