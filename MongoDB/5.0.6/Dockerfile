FROM mongo:5.0.6

COPY replicaset-initiate.js /docker-entrypoint-initdb.d/

CMD ["mongod", "--replSet", "rs:qa0"]