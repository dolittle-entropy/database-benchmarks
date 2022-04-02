// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MongoDB.Driver;

namespace Benchmarks;

/// <summary>
/// Benchmarks that simulate committing events with an in-memory batching of concurrent requests.
/// </summary>
public class Batching : JobBase
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
    [Params(1, 10)]
    public int ConcurrentBatches { get; set; }

    /// <summary>
    /// Commits non-aggregate events.
    /// </summary>
    [Benchmark]
    public async Task CommitEvents()
    {
        if (NumberOfEvents == 1 && ConcurrentBatches == 1)
        {
            await EventLog.InsertOneAsync(new CommittedEvent
            {
                SequenceNumber = _eventLogSequenceNumber++,
                EventSource = EventSource,
                Content = 1.ToString(),
            }).ConfigureAwait(false);
            return;
        }
        using var session = await Client.StartSessionAsync().ConfigureAwait(false);
        await session.WithTransactionAsync(async (transaction, cancellationToken) =>
        {
            await EventLog.InsertManyAsync(
                    transaction,
                    Enumerable.Range(1, ConcurrentBatches).SelectMany(_ =>
                        Enumerable.Range(0, NumberOfEvents).Select(n =>
                            new CommittedEvent
                            {
                                SequenceNumber = _eventLogSequenceNumber++,
                                EventSource = EventSource,
                                Content = n.ToString(),
                            })))
                .ConfigureAwait(false);
            return _eventLogSequenceNumber;
        });
    }

    /// <summary>
    /// Commits aggregate events.
    /// </summary>
    [Benchmark]
    public async Task CommitAggregateEvents()
    {
        
        using var session = await Client.StartSessionAsync().ConfigureAwait(false);
        await session.WithTransactionAsync(async (transaction, cancellationToken) =>
        {
            var events = new List<CommittedEvent>();

            for (var _ = 0; _ < ConcurrentBatches; _++)
            {
                var aggregateType = Guid.NewGuid();
                var aggregateRootVersion = 0;
                
                for (var n = 0; n < NumberOfEvents; n++)
                {
                    events.Add(new CommittedEvent
                    {
                        SequenceNumber = _eventLogSequenceNumber++,
                        EventSource = EventSource,
                        AggregateType = aggregateType,
                        AggregateRootVersion = (ulong)aggregateRootVersion++,
                    });
                }

                await Aggregates.InsertOneAsync(
                    transaction,
                    new AggregateRoot
                    {
                        EventSource = EventSource,
                        AggregateType = aggregateType,
                        Version = (ulong) aggregateRootVersion,
                    },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            await EventLog.InsertManyAsync(
                transaction,
                events,
                new InsertManyOptions
                {
                    IsOrdered = true,
                },
                cancellationToken).ConfigureAwait(false);
            
            return _eventLogSequenceNumber;
        }).ConfigureAwait(false);
    }
}