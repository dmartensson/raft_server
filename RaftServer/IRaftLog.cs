using System.Collections.Generic;

namespace RaftServer
{
    public interface IRaftLog
    {
        (uint term, ulong index) LastEntry();
        void Append(IRaftEntry entry);
        void Truncate(ulong logIndex);
        bool HasEntry(uint term, ulong logIndex);
        //void SetCommitIndex(ulong commitIndex);
        IRaftEntry GetEntry(ulong @ulong);
        List<IRaftEntry> GetEntries(ulong fromLogIndex, ulong toLogIndex, int maxEntries);
        ulong NextIndex();
    }
}