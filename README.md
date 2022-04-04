# Dolittle Event Store Database Benchmarks

This repository contains benchmarks for experimenting with the database-layer of the Event Store implementation.

### Latest results:
From commit `9672038`, the benchmarks were run with the `dolittle/mongodb:4.2.2` image, on this machine:
``` ini
BenchmarkDotNet=v0.13.1, OS=macOS Monterey 12.2.1 (21D62) [Darwin 21.3.0]
Intel Core i7-7920HQ CPU 3.10GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.200
  [Host]     : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT
  DefaultJob : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT
```

#### With 10 max concurrent commits
| Method                | NumberOfEvents | ConcurrentBatches | Current Implementation | In-Memory Sequence Number |    InsertMany | Single event no transaction | Simulated batching |
|-----------------------|----------------|-------------------|-----------------------:|--------------------------:|--------------:|----------------------------:|-------------------:|
| **CommitEvents**      | **1**          | **1**             |           **9.045 ms** |              **3.218 ms** |  **3.038 ms** |                **1.599 ms** |       **1.607 ms** |
| CommitAggregateEvents | 1              | 1                 |              10.631 ms |                  4.271 ms |      4.587 ms |                    4.490 ms |           4.576 ms |
| **CommitEvents**      | **1**          | **10**            |         **185.578 ms** |             **16.062 ms** | **15.669 ms** |                **7.796 ms** |       **3.340 ms** |
| CommitAggregateEvents | 1              | 10                |             160.353 ms |                 22.860 ms |     24.461 ms |                   23.881 ms |          18.093 ms |
| **CommitEvents**      | **10**         | **1**             |          **27.726 ms** |             **15.227 ms** |  **3.339 ms** |                **3.307 ms** |       **3.197 ms** |
| CommitAggregateEvents | 10             | 1                 |              28.147 ms |                 16.932 ms |      4.722 ms |                    4.724 ms |           4.703 ms |
| **CommitEvents**      | **10**         | **10**            |         **490.392 ms** |             **78.581 ms** | **17.605 ms** |               **17.532 ms** |       **4.992 ms** |
| CommitAggregateEvents | 10             | 10                |             574.842 ms |                 85.274 ms |     25.487 ms |                   25.488 ms |          19.969 ms |
| **CommitEvents**      | **100**        | **1**             |         **182.336 ms** |            **137.177 ms** |  **5.074 ms** |                **5.013 ms** |       **4.955 ms** |
| CommitAggregateEvents | 100            | 1                 |             161.238 ms |                150.989 ms |      7.030 ms |                    6.521 ms |           6.570 ms |
| **CommitEvents**      | **100**        | **10**            |       **3,194.218 ms** |            **734.457 ms** | **33.373 ms** |               **30.649 ms** |      **21.548 ms** |
| CommitAggregateEvents | 100            | 10                |           3,345.203 ms |                727.342 ms |     42.630 ms |                   39.188 ms |          36.524 ms |

#### With 100 max concurrent commits
The results are a little confusing, and it seems like running it concurrently with 100 breaks a few things.
There might be some bugs in the code, so take them with a grain of salt until we can verify it:

| Method                | NumberOfEvents | ConcurrentBatches | Current Implementation | In-Memory Sequence Number |       InsertMany | Single event no transaction | Simulated batching |
|-----------------------|----------------|-------------------|-----------------------:|--------------------------:|-----------------:|----------------------------:|-------------------:|
| **CommitEvents**      | **1**          | **1**             |           **9.372 ms** |              **2.904 ms** |    **22.644 ms** |                **9.058 ms** |       **1.535 ms** |
| CommitAggregateEvents | 1              | 1                 |              10.313 ms |                  4.311 ms |        30.495 ms |                   30.666 ms |           4.319 ms |
| **CommitEvents**      | **1**          | **10**            |         **183.587 ms** |             **15.392 ms** |   **140.955 ms** |               **67.702 ms** |       **3.196 ms** |
| CommitAggregateEvents | 1              | 10                |             204.522 ms |                 22.874 ms |       220.078 ms |                  243.284 ms |          17.600 ms |
| **CommitEvents**      | **1**          | **100**           |                 **NA** |            **143.371 ms** | **1,585.905 ms** |              **742.964 ms** |       **4.807 ms** |
| CommitAggregateEvents | 1              | 100               |                     NA |                241.898 ms |     2,262.191 ms |                2,284.739 ms |         141.394 ms |
| **CommitEvents**      | **10**         | **1**             |          **21.143 ms** |             **17.494 ms** |     **3.181 ms** |               **24.618 ms** |       **3.061 ms** |
| CommitAggregateEvents | 10             | 1                 |              26.565 ms |                 17.071 ms |        33.672 ms |                   34.214 ms |           4.414 ms |
| **CommitEvents**      | **10**         | **10**            |         **450.867 ms** |             **84.382 ms** |   **162.570 ms** |              **150.074 ms** |       **4.654 ms** |
| CommitAggregateEvents | 10             | 10                |             487.659 ms |                 81.377 ms |       235.724 ms |                  234.747 ms |          18.514 ms |
| **CommitEvents**      | **10**         | **100**           |                 **NA** |            **821.723 ms** | **1,687.986 ms** |                      **NA** |      **20.604 ms** |
| CommitAggregateEvents | 10             | 100               |                     NA |                873.690 ms |     2,415.658 ms |                1,652.235 ms |         159.691 ms |
| **CommitEvents**      | **100**        | **1**             |         **501.691 ms** |            **132.130 ms** |    **39.169 ms** |               **40.105 ms** |       **4.680 ms** |
| CommitAggregateEvents | 100            | 1                 |                     NA |                138.455 ms |        47.915 ms |                   46.006 ms |           6.146 ms |
| **CommitEvents**      | **100**        | **10**            |                 **NA** |            **644.026 ms** |   **267.704 ms** |              **279.766 ms** |      **20.810 ms** |
| CommitAggregateEvents | 100            | 10                |                     NA |                647.852 ms |       365.102 ms |                  356.790 ms |          34.429 ms |
| **CommitEvents**      | **100**        | **100**           |                 **NA** |          **7,027.007 ms** |           **NA** |            **2,936.477 ms** |     **181.270 ms** |
| CommitAggregateEvents | 100            | 100               |                     NA |              7,139.298 ms |       436.113 ms |                2,796.198 ms |         317.002 ms |
