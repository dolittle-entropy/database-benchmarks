FROM mongo:4.4.13

COPY replicaset-initiate.js /docker-entrypoint-initdb.d/

CMD ["mongod", "--replSet", "rs0"]