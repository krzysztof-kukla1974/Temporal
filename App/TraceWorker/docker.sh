PLATFORM=linux/arm64
IMAGE_NAME="trace-worker-image"
TEMPORAL_HOST=172.18.0.4:7233

dotnet publish -c Release

docker ps -a
docker stop t1 t2 t3 t4 t5
docker rm t1 t2 t3 t4 t5
docker images
docker rmi $IMAGE_NAME
docker build --platform=$PLATFORM -t $IMAGE_NAME -f Dockerfile .
docker save $IMAGE_NAME -o ~/Downloads/$IMAGE_NAME.img
docker run --name=t1 -d --network temporal-network $IMAGE_NAME $TEMPORAL_HOST