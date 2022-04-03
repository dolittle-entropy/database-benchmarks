// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MongoDB.Driver;

namespace Benchmarks;

/// <summary>
/// Benchmarks that simulate committing events using an implementation that keeps track of the Event Log Sequence Number in-memory.
/// </summary>
public class WithoutSequenceCount : JobBase
{
    const string EventSource = "a62611fb-ef61-4c28-a1dc-5be183f424cf";
    
    ulong _eventLogSequenceNumber = 0;

    /// <inheritdoc />
    protected override void Setup()
    {
        Aggregates.Indexes.CreateOne(new CreateIndexModel<AggregateRoot>(
            Builders<AggregateRoot>.IndexKeys
                .Ascending(_ => _.EventSource)
                .Ascending(_ => _.AggregateType),
            new CreateIndexOptions
            {
                Unique = true,
            }));
    }
    
    /// <summary>
    /// Gets the number of events to commit in each batch.
    /// </summary>
    [Params(1, 10, 100)]
    public int NumberOfEvents { get; set; }
    
    /// <summary>
    /// Gets the number of concurrent batches to commit.
    /// </summary>
    [Params(1, 10, 100)]
    public int ConcurrentBatches { get; set; }

    /// <summary>
    /// Commits non-aggregate events.
    /// </summary>
    [Benchmark]
    public Task CommitEvents()
        => Task.WhenAll(
            Enumerable.Range(1, ConcurrentBatches).Select(async _ =>
            {
                using var session = await Client.StartSessionAsync().ConfigureAwait(false);
                await session.WithTransactionAsync(async (transaction, cancellationToken) =>
                {
                    for (var n = 0; n < NumberOfEvents; n++)
                    {
                        await EventLog.InsertOneAsync(
                                transaction,
                                new CommittedEvent
                                {
                                    SequenceNumber = Interlocked.Increment(ref _eventLogSequenceNumber),
                                    EventSource = EventSource,
                                    Content = n.ToString(),
                                },
                                cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                    }

                    return _eventLogSequenceNumber;
                }).ConfigureAwait(false);
            }));

    /// <summary>
    /// Commits aggregate events.
    /// </summary>
    [Benchmark]
    public Task CommitAggregateEvents()
        => Task.WhenAll(
            Enumerable.Range(1, ConcurrentBatches).Select(async _ =>
            {
                using var session = await Client.StartSessionAsync().ConfigureAwait(false);
                await session.WithTransactionAsync(async (transaction, cancellationToken) =>
                {
                    var sequenceNumber = Interlocked.Add(ref _eventLogSequenceNumber, (ulong)NumberOfEvents) - (ulong)NumberOfEvents;
                    var aggregateType = Guid.NewGuid();
                    var aggregateRootVersion = 0;

                    for (var n = 0; n < NumberOfEvents; n++)
                    {
                        await EventLog.InsertOneAsync(
                                transaction,
                                new CommittedEvent
                                {
                                    SequenceNumber = sequenceNumber,
                                    EventSource = EventSource,
                                    AggregateType = aggregateType,
                                    AggregateRootVersion = (ulong)aggregateRootVersion,
                                    Content = n.ToString(),
                                },
                                cancellationToken: cancellationToken)
                            .ConfigureAwait(false);
                        sequenceNumber++;
                        aggregateRootVersion++;
                    }

                    await Aggregates.InsertOneAsync(
                            transaction,
                            new AggregateRoot
                            {
                                EventSource = EventSource,
                                AggregateType = aggregateType,
                                Version = (ulong) aggregateRootVersion,
                            },
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    return sequenceNumber;
                }).ConfigureAwait(false);
            }));
}