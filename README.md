# raft_server
Implementation of the Raft consensus protocol in C#

https://raft.github.io/

This is currently just at test to have a working basic implementation without any focus on performance or features.

Currently the server and client have basic functions to start up and reach quorum and the client can send commands that are distributed and applied.

The log i minimal with no optimisations and there is no persistant storage.

The only transport is an in-memory debug transport and all servers run in the same application.

### Roadmap

1. Testing
  * How does the cluster handle disconnects
  * How does it handle reconnects
  * Can it resync a peer
  * What happens with more servers
  * What happens when we no longer can commit due to to few clients
2. Add persistant storage
  * Logfile
  * StateMachine
  * Configuration
3. Add http transport and the ability to use separate applications and machines
4. Add join and leave mechanics so the cluster configuration can be changes without downtime
5. Add the ability to enforce leader even with one peer for recovery
6. Add TTL for messages where the client waits longer than the server to avoid a client giving up while the server commits the command
7. Add a way for a leader to realize it cannot commit due to loss of peers and can communicate this to clients
8. Experiment with auto adjustable timeouts so a fast stable network could have shorter roundtrips for commit while an unstable will take longer time but still get the job done
9. Implement snapshots for faster replay on new clients and the ability to truncate the log
