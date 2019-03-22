using System;
using System.Collections.Generic;
using System.Linq;

namespace RaftServer
{
    public class RaftLog : IRaftLog
    {
        private IRaftStorage _storage;
        private readonly List<IRaftEntry> _log = new List<IRaftEntry>();

        public RaftLog(IRaftStorage storage)
        {
            _storage = storage;
        }
        public (uint term, ulong index) LastEntry()
        {
            if (!_log.Any())
            {
                return (0, 0);
            }
            var existing = GetEntry(_log.Max(l => l.LogIndex));
            return  (existing.Term, existing.LogIndex);
        }

        public void Append(IRaftEntry entry)
        {
            var existing = GetEntry(entry.LogIndex);
            if (existing != null && existing.Term != entry.Term)
                Truncate(entry.LogIndex);
            _log.Add(entry);
        }

        public void Truncate(ulong logIndex)
        {
            if (_log.Any())
                _log.RemoveAll(l => l.LogIndex >= logIndex);
        }

        public bool HasEntry(uint term, ulong logIndex)
        {
            if (logIndex == 0)
                return true;
            return _log.Any(l => l.LogIndex == logIndex && l.Term == term);
        }

        public IRaftEntry GetEntry(ulong logIndex)
        {
            return _log.FirstOrDefault(l => l.LogIndex == logIndex);
        }

        public List<IRaftEntry> GetEntries(ulong fromLogIndex, ulong toLogIndex, int maxEntries)
        {
            if (toLogIndex <= fromLogIndex)
                return new List<IRaftEntry>();
            return _log.Where(l => l.LogIndex >= fromLogIndex && l.LogIndex <= toLogIndex).OrderBy(l => l.LogIndex).Take(maxEntries).ToList();
        }

        public ulong NextIndex()
        {
            if (!_log.Any())
                return 1;
            return _log.Max(l => l.LogIndex) + 1;
        }
    }
}
