#REMOTE_HOST="20.83.233.60"
#REMOTE_USER="superuser"
PLATFORM=linux/arm64/v8
#PLATFORM=amd64
IMAGE_NAME="trace-worker-image"
#DOCKER_FOLDER=$(date "+%Y%m%d%H%M%S")
#N="100000000000"

dotnet publish -c Release

docker ps -a
docker stop t1 t2 t3 t4 t5
docker rm t1 t2 t3 t4 t5
docker images
docker rmi $IMAGE_NAME
docker build --platform=$PLATFORM -t $IMAGE_NAME -f Dockerfile .
docker save $IMAGE_NAME -o ~/Downloads/$IMAGE_NAME.img

#scp ~/Downloads/$IMAGE_NAME.img $REMOTE_USER@$REMOTE_HOST:~/Downloads

#ssh -tt $REMOTE_USER@$REMOTE_HOST << EOF
#docker ps -a
#docker stop c1 c2 c3 c4 c5
#docker rm c1 c2 c3 c4 c5
#docker images
#docker rmi $IMAGE_NAME
#docker load -i ~/Downloads/$IMAGE_NAME.img
docker run --name=t1 -d $IMAGE_NAME
#exit
#EOF