using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using RaftCommon;
using RaftServer;

namespace Raft
{
    public static class Program
    {
        const int Heartbeat = 50;

        private static async Task Main()
        {
            const LogLevel logLevel = LogLevel.Basic;
            var watch = Stopwatch.StartNew();
            var transport = new MemoryTransport(new Diagnostic("transport", watch, logLevel)); 
            var serializer = new ProtoBufSerializer();
            var s1 = new MemoryStorage();
            s1.Write("config", serializer.Serialize(new RaftConfig(new List<string> {"s2", "s3"}, "s1", Heartbeat)));
            var sm1 = new StateMachine();
            sm1.RegisterCommand("put", data =>
            {
                Console.WriteLine($"s1:put: {serializer.Deserialize<string>(data)}");
                return null;
            });
            var rs1 = new RaftServer.RaftServer(new RaftLog(s1), transport.GetPeerTransport("s1"), s1, sm1, serializer, new Diagnostic("s1", watch, logLevel));
            var s2 = new MemoryStorage();
            s2.Write("config", serializer.Serialize(new RaftConfig(new List<string> { "s1", "s3" }, "s2", Heartbeat)));
            var sm2 = new StateMachine();
            sm2.RegisterCommand("put", data =>
            {
                Console.WriteLine($"s2:put: {serializer.Deserialize<string>(data)}");
                return null;
            });
            var rs2 = new RaftServer.RaftServer(new RaftLog(s2), transport.GetPeerTransport("s2"), s2, sm2, serializer, new Diagnostic("s2", watch, logLevel));
            var s3 = new MemoryStorage();
            s3.Write("config", serializer.Serialize(new RaftConfig(new List<string> { "s2", "s1" }, "s3", Heartbeat)));
            var sm3 = new StateMachine();
            sm3.RegisterCommand("put", data =>
            {
                Console.WriteLine($"s3:put: {serializer.Deserialize<string>(data)}");
                return null;
            });
            var rs3 = new RaftServer.RaftServer(new RaftLog(s3), transport.GetPeerTransport("s3"), s3, sm3, serializer, new Diagnostic("s3", watch, logLevel));
            var cs = new CancellationTokenSource();
            var ct = cs.Token;
            // ReSharper disable MethodSupportsCancellation
            var p1 = Task.Run(async () => await rs1.Run(ct));
            var p2 = Task.Run(async () => await rs2.Run(ct));
            var p3 = Task.Run(async () => await rs3.Run(ct));

            var client = new RaftClient.RaftClient(transport.GetPeerTransport("c1"), serializer, new List<string>{"s1"}, "c1", Heartbeat);

            var p4 = client.Connect(ct);
            p4.Wait();
            await client.Command("put", serializer.Serialize("Hello World"), ct);

            Console.WriteLine("Press enter to quit");
            var input = Console.ReadLine();
            while (!string.IsNullOrWhiteSpace(input))
            {
                await client.Command("put", serializer.Serialize(input), ct);
                input = Console.ReadLine();
            }
            cs.Cancel();
            Task.WaitAll(p1, p2, p3);
        }
    }
}
