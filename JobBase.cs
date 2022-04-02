// Copyright (c) Dolittle. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using BenchmarkDotNet.Attributes;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Benchmarks;

/// <summary>
/// Represents a base for <see cref="BenchmarkDotNet.Jobs.Job"/> that initializes the MongoDB database and collections to use for each benchmark.
/// </summary>
public abstract class JobBase
{
    string _databaseName;

    /// <summary>
    /// Sets up the MongoDB client, and the 'event-log' and 'aggregates' collections.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _databaseName = Guid.NewGuid().ToString();
        Client= new MongoClient("mongodb://localhost:27017");
        var database = Client.GetDatabase(_databaseName);
        
        database.CreateCollection("event-log");
        EventLog = database.GetCollection<CommittedEvent>("event-log");
        
        database.CreateCollection("aggregates");
        Aggregates = database.GetCollection<AggregateRoot>("aggregates");
        
        Setup();
    }

    /// <summary>
    /// Drops the created database for the current benchmark.
    /// </summary>
    [GlobalCleanup]
    public void GlobalCleanup()
        => Client.DropDatabase(_databaseName);
    
    /// <summary>
    /// Gets the <see cref="IMongoClient"/> to use for the benchmarks.
    /// </summary>
    protected IMongoClient Client { get; private set; }
    
    /// <summary>
    /// Gets the 'event-log' <see cref="IMongoCollection{TDocument}"/> to use for the benchmarks.
    /// </summary>
    protected IMongoCollection<CommittedEvent> EventLog { get; private set; }
    
    /// <summary>
    /// Gets the 'aggregates' <see cref="IMongoCollection{TDocument}"/> to use for the benchmarks.
    /// </summary>
    protected IMongoCollection<AggregateRoot> Aggregates { get; private set; }

    /// <summary>
    /// The method that sets up the environment for each benchmark to run.
    /// </summary>
    protected abstract void Setup();
    
    /// <summary>
    /// Represents a committed event in the 'event-log' collection.
    /// </summary>
    protected class CommittedEvent
    {
        /// <summary>
        /// The Event Log Sequence Number - unique for each committed event.
        /// </summary>
        [BsonId]
        [BsonRepresentation(BsonType.Decimal128)]
        public ulong SequenceNumber { get; set; }
        
        /// <summary>
        /// The Event Source of the committed event.
        /// </summary>
        public string EventSource { get; set; }
        
        /// <summary>
        /// The Aggregate Root type that committed the event.
        /// </summary>
        public Guid AggregateType { get; set; }
        
        /// <summary>
        /// The version of the Aggregate Root instance that committed the event.
        /// </summary>
        [BsonRepresentation(BsonType.Decimal128)]
        public ulong AggregateRootVersion { get; set; }
        
        /// <summary>
        /// The actual content of the committed event.
        /// </summary>
        public string Content { get; set; }
    }

    /// <summary>
    /// Represents an Aggregate Root instance in the 'aggregates' collection.
    /// </summary>
    protected class AggregateRoot
    {
        /// <summary>
        /// The Event Source of the Aggregate Root instance.
        /// </summary>
        public string EventSource { get; set; }
        
        /// <summary>
        /// The Type of the Aggregate Root instance.
        /// </summary>
        public Guid AggregateType { get; set; }
        
        /// <summary>
        /// The version of, or number of events committed by, the Aggregate Root instance.
        /// </summary>
        [BsonRepresentation(BsonType.Decimal128)]
        public ulong Version { get; set; }
    }
}