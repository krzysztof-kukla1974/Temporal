REMOTE_HOST="20.83.233.60"
REMOTE_USER="superuser"
PLATFORM="linux/amd64"
IMAGE_NAME="trace-worker-image"
TEMPORAL_HOST=172.18.0.4:7233

scp ~/Downloads/$IMAGE_NAME.img $REMOTE_USER@$REMOTE_HOST:~/Downloads
ssh -tt $REMOTE_USER@$REMOTE_HOST << EOF

docker ps -a
docker stop t1 t2 t3 t4 t5
docker rm t1 t2 t3 t4 t5
docker images
docker rmi $IMAGE_NAME
docker load -i ~/Downloads/$IMAGE_NAME.img
docker run --name=t1 -d --network temporal-network $IMAGE_NAME $TEMPORAL_HOST

exit
EOF