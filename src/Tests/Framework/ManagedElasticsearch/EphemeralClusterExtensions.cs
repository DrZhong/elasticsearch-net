﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Elastic.Managed.Ephemeral;
using Elastic.Xunit;
using Elasticsearch.Net;
using Nest;

namespace Tests.Framework.ManagedElasticsearch
{
	internal static class EphemeralClusterExtensions
	{
		public static bool RunningFiddler => Process.GetProcessesByName("fiddler").Any();

		public static IElasticClient GetOrAddClient<TConfig>(
			this IEphemeralCluster<TConfig> cluster,
			Func<ConnectionSettings, ConnectionSettings> createSettings = null,
			Func<ICollection<Uri>, IConnectionPool> createPool = null)
			where TConfig : EphemeralClusterConfiguration
		{
			return cluster.GetOrAddClient(c =>
			{
				var host = (RunningFiddler) ? "ipv4.fiddler" : "localhost";
				createSettings = createSettings ?? (s => s);
				createPool = createPool ?? (uris => new StaticConnectionPool(uris));
				var connectionPool = createPool(c.NodesUris(host));
				var connection = TestClient.Configuration.RunIntegrationTests
					? TestClient.CreateLiveConnection()
					: new InMemoryConnection();
				var settings = TestClient.CreateSettings(createSettings, connection, connectionPool);
				if (cluster.ClusterConfiguration.EnableSecurity)
					settings = settings.BasicAuthentication(ClusterAuthentication.Admin.Username, ClusterAuthentication.Admin.Password);
				var client = new ElasticClient(settings);
				return client;
			});
		}
	}
}