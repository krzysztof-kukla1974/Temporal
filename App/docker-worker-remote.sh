REMOTE_HOST="20.83.233.60"
REMOTE_USER="superuser"
PLATFORM="linux/amd64"
IMAGE_NAME="trace-worker-image"
TEMPORAL_HOST="172.19.0.4:7233"
CLIENT_HOST="172.19.0.4:7233"
CLIENT_DATE_FROM="2023-01-01"
CLIENT_DATE_TO="2023-01-05"

echo "### image cleanup ###"
docker ps -a
docker stop t1
docker rm t1
docker images
docker rmi $IMAGE_NAME

echo "### docker build ###"
docker build --platform=$PLATFORM -t $IMAGE_NAME -f TraceWorker/Dockerfile .

echo "### docker save ###"
docker save $IMAGE_NAME -o ~/Downloads/$IMAGE_NAME.img

echo "### upload image onto remote machine ###"
scp ~/Downloads/$IMAGE_NAME.img $REMOTE_USER@$REMOTE_HOST:~/Downloads
echo "### upload client onto remote machine ###"
tar -cvzf ~/Downloads/client.tar ~/Projects/Temporal/App/TraceClient/bin/Release/net6.0/*
scp ~/Downloads/client.tar $REMOTE_USER@$REMOTE_HOST:~/Downloads
ssh -tt $REMOTE_USER@$REMOTE_HOST << EOF

docker ps -a
docker stop t1
docker rm t1
docker images
docker rmi $IMAGE_NAME
docker load -i ~/Downloads/$IMAGE_NAME.img
docker run --name=t1 -d --network temporal-network $IMAGE_NAME $TEMPORAL_HOST
rm -f ~/Downloads/client
tar -xvf ~/Downloads/client.tar -C ~/Downloads
cd ~/Downloads/Users/krzysztof.kukla/Projects/Temporal/App/TraceClient/bin/Release/net6.0
dotnet TraceClient.dll $CLIENT_HOST $CLIENT_DATE_FROM $CLIENT_DATE_TO
docker logs t1 | grep Results
exit
EOF