CREATE KEYSPACE IF NOT EXISTS kashtanka
	WITH REPLICATION = {
		'class': 'SimpleStrategy',
		'replication_factor': 1
	}
	AND DURABLE_WRITES = true;
