# REST API service that provides access to the animal card (advertisment) persistant storage / database

[![Build Status](https://drone.k8s.grechka.family/api/badges/LostPetInitiative/CardStorageRestAPI/status.svg)](https://drone.k8s.grechka.family/LostPetInitiative/CardStorageRestAPI)
![GitHub release (latest SemVer)](https://img.shields.io/github/v/release/LostPetInitiative/CardStorageRestAPI?sort=semver)
![Docker Image Version (latest semver)](https://img.shields.io/docker/v/lostpetinitiative/cassandra-rest-api?label=docker%20image&sort=semver)
![Docker Pulls](https://img.shields.io/docker/pulls/lostpetinitiative/cassandra-rest-api)

The REST API for the card (lost/found pet advertisement) storage. In practice it communicates with CassandraDB cluster.

The [Web app](https://github.com/LostPetInitiative/WebApp) uses this API to fetch pet card images as well as other card data and similarity feature vectors.

If the CassandraDB is empty, upon first request the service will deploy the needed CassandraDB schema (keyspace & tables)

## The repo

The API is ASP.NET CORE web api written in C# and packaged to be used as Docker image.

All pull requests are [checked with CI automation](https://drone.k8s.grechka.family/LostPetInitiative/CardStorageRestAPI).

Built releases are automatically published to [Docker Hub](https://hub.docker.com/repository/docker/lostpetinitiative/cassandra-rest-api) and are ready to use.

## How to use

The API is intended to be run in Linux container. e.g. with Docker.

You can set the following environmental variables to configer the service:
| EnvVar         | Description     | Example |
|--------------|-----------|------------|
| CASSANDRA_ADDRS | endpoint of cassandra      | 10.0.4.12:34248     |
|KEYSPACE  | keyspace to use  |  kashtanka |

Remark: If you run the service without specifying the Cassandra endpoint, the memory storage will be used. Might be useful for testing and debuging.


## How to build

### Prerequisites
You will need to install the following prerequsites to build the repo
1. [.NET 6.0 SDK](https://dotnet.microsoft.com/en-us/download/visual-studio-sdks)

### Development build & debug
1. Change dir to the root of the repo
2. Run `dotnet run`

### Production build
1. Change dir to the root of the repo
2. Run `dotnet build`

### Docker
The `Dockerfile` in the root of the repo defines the Docker image that creates a production build and prepares the web service to be run in production.
