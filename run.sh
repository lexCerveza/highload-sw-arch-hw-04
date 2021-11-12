#!/bin/bash

# Install apache utils
sudo apt-get update && sudo apt-get install siege -y

echo "Siege installed"

# Create mongodb volume folder
sudo mkdir -p /srv/docker/mongodb/data

# Run services
docker-compose up --build -d
sudo chown -R 472:472 /srv/docker/mongodb/data

echo "Services spinned up"

# Wait for services spinning up + heat up the app
sleep 10
curl http://127.0.0.1:80

# Run stress tests
siege -c10 -t60S http://127.0.0.1:80
siege -c25 -t60S http://127.0.0.1:80
siege -c50 -t60S http://127.0.0.1:80
siege -c65 -t60S http://127.0.0.1:80
siege -c100 -t60S http://127.0.0.1:80

# Stop the services
docker-compose down
echo "Services stopped"