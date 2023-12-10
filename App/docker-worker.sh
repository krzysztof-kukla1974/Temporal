PLATFORM=linux/arm64
IMAGE_NAME="trace-worker-image"
TEMPORAL_HOST="172.18.0.4:7233"
CLIENT_HOST="127.0.0.1:7233"
CLIENT_DATE_FROM="2023-01-01"
CLIENT_DATE_TO="2023-01-05"

docker ps -a
docker stop t1
docker rm t1
docker images
docker rmi $IMAGE_NAME

docker build --platform=$PLATFORM -t $IMAGE_NAME -f TraceWorker/Dockerfile .

docker run --name=t1 -d --network temporal-network $IMAGE_NAME $TEMPORAL_HOST

cd ~/Projects/Temporal/App/TraceClient/bin/Release/net6.0
./TraceClient $CLIENT_HOST $CLIENT_DATE_FROM $CLIENT_DATE_TO
docker logs t1 | grep Results