## 1. Run eClaimApp, eClaimApi, SQL Server, and Redis in Docker container 

### SQL Server image  

#### 1. Pull MSSQL image
    $ docker pull mcr.microsoft.com/mssql/server:2022-latest  

#### 2. Run MSSQL in container
    $ docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=Amit@123" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2022-latest

#### 3. Check SQL Server running status
    $ docker ps 

    Output :
    CONTAINER ID        IMAGE                                           COMMAND                     CREATED          STATUS                    PORTS                                            NAMES
    6ed4682938bb        mcr.microsoft.com/mssql/server:2022-latest      "/opt/mssql/bin/laun…"      14 minutes ago   Up 14 minutes             0.0.0.0:1433->1433/tcp, [::]:1433->1433/tcp      sqlserver

#### 4. Check Hostname for connect SQL server
    $ hostname -I

#### 5. SQL connection details
    Server: 172.17.221.91,1433
    Login: SA
    Password: Amit@123    


### Redis image   

#### 1. Pull Redis image
    $ docker pull redis

#### 2. Run Redis in container
    $ docker run -d --name redis-server -p 6379:6379 -v redis_data:/data redis redis-server --appendonly yes

    Comment:
	-d → Detached mode (runs in background)
	--name redis-server → Names the container
	-p 6379:6379 → Maps Redis port 6379 to localhost:6379
	redis → The image name
    -v redis_data:/data → Stores Redis data in a Docker volume
	--appendonly yes → Enables persistence

#### 3. Check to Redis connection
    d$ ocker exec -it redis-server redis-cli

    Try for testing:
		ping
	Response should be:
		PONG

### Run backend Application in container

#### 1. Pull app image
    git clone https://github.com/amitkumaramithotmailcom/E-Claim-Docker.git

#### 2. Move on dockerfile folder
    $ cd E-Claim-Docker
    E-Claim-Docker$ cd E-Claim-Service
    
#### 3. Run docker build command for docker image
    $ docker build -t amitkumaramit/eclaim_api -f EClaim.API/Dockerfile .

    Comment:
    amitkumaramithotmailcom/e-claim-docker : Image Name
    EClaim.API/Dockerfile : docker file path

    Dockerfile : [text](https://github.com/amitkumaramithotmailcom/E-Claim-Docker/blob/main/E-Claim-Service/EClaim.API/Dockerfile)
        

#### 4. Run api application
    $ docker run -d -p 5000:5000 -e ASPNETCORE_URLS=http://+:5000 amitkumaramit/eclaim_api

#### 5. Check backend app running status
    $ docker ps 

    Output :
    CONTAINER ID   IMAGE                        COMMAND                  CREATED          STATUS          PORTS                                                                                         NAMES
    6b380631a8c0   amitkumaramit/eclaim_api     "dotnet EClaim.API.d…"   12 minutes ago   Up 12 minutes   8080/tcp, 0.0.0.0:5000->80/tcp, 0.0.0.0:5001->80/tcp, [::]:5000->80/tcp, [::]:5001->80/tcp    eclaim_api

#### 6. App accessable on below port
    http://172.17.221.91:5000/
    http://172.17.221.91:5001/
    http://172.17.221.91:5000/swagger/index.html


### Run Frontend Application in container

#### 1. Move on dockerfile folder
    E-Claim-Docker$ cd EClaim.Application
    
#### 2. Run docker build command for docker image
    $ docker build -t eclaim-application-image -f EClaim.Application/Dockerfile .

    Dockerfile : [text](https://github.com/amitkumaramithotmailcom/E-Claim-Docker/blob/main/EClaim.Application/EClaim.Application/Dockerfile)

#### 3. Run frontend application
	$docker run -d -p 5005:8080 --name eclaim_app eclaim-application-image

#### 4. Check app running status
    $ docker ps 

    Output :
    CONTAINER ID   IMAGE                          COMMAND                  CREATED          STATUS          PORTS                                                                                        NAMES
    e79babff646f   amitkumaramit/eclaim_app:1.0   "dotnet EClaim.Appli…"   18 seconds ago   Up 18 seconds   8081/tcp, 0.0.0.0:5005->81/tcp, 0.0.0.0:5006->81/tcp, [::]:5005->81/tcp, [::]:5006->81/tcp   eclaim_app

#### 5. App accessable on below port
    http://172.17.221.91:5005/

-------------
-------------

## 2. Run containers using docker compose (App, Redis)

#### 1. API - Move on docker-compose.yml file folder
    $ cd E-Claim-Service

    docker-compose.yml file : [text](https://github.com/amitkumaramithotmailcom/E-Claim-Docker/blob/main/E-Claim-Service/docker-compose.yml)

#### 2. API - Run docker-compose command
    $ docker-compose up --build -d

#### 3. APP - Move on docker-compose.yml file folder
    $ cd EClaim.Application

    docker-compose.yml file : [text](https://github.com/amitkumaramithotmailcom/E-Claim-Docker/blob/main/EClaim.Application/docker-compose.yml)

#### 4. API - Run docker-compose command
    $ docker-compose up --build -d

#### 5. Check app running status
    $ docker ps 


-------------
-------------

## 3. Create image and push in Docker Hub Registry
#### 1. Docker Sign in
	$ docker login
#### 2. First create a repo on Docker Hub if not exists(e.g., amitkumaramit/eclaim_api)

#### 2. Check images list
	$ docker images

    REPOSITORY                       TAG           IMAGE ID       CREATED        SIZE
    amitkumaramit/eclaim_app         1.0           dd66691aacc8   24 hours ago   289MB
    amitkumaramit/eclaim_api         1.0           33281b937363   25 hours ago   239MB
    mcr.microsoft.com/mssql/server   2022-latest   298baf34796c   5 weeks ago    1.63GB
    redis                            7             56825e2b84a2   6 weeks ago    117MB

#### 3. Tag image
	$ docker tag <RepositoryName>:<Tag> amitkumaramit/eclaim_api:1.0
      
        e.g. $ docker tag e-claim-service-api:latest amitkumaramit/eclaim_api:1.0
			 $ docker tag eclaimapplication-app:latest amitkumaramit/eclaim_app:1.0

#### 4. Push image to Docker Hub
	$ docker push amitkumaramit/eclaim_api:1.0
	
#### 5. Pull image from Docker Hub
	$ docker pull amitkumaramit/eclaim_api:1.0

-------------
-------------

## 4. Create image and push in Docker Hub Registry
#### 1. Stop containers if running
    Stop all containers:
	# docker stop $(docker ps -aq)

    Stop single containers:
	# docker stop <ContainerId>

#### 2. Remove containers
    Remove single containers:
	$ docker rm <RepositoryName>

    Remove all containers:
	docker rm $(docker ps -aq)	

#### 3. Remove images
    Remove single image:
    $ docker rmi <RepositoryName>

    Remove all images:
    docker rmi $(docker images -aq)

    Remove all images (force remove, even if containers are using the image):
	docker rmi -f $(docker images -aq)

#### 4. dockercompose file for run containers using images
    API docker-compose.yml : [text](https://github.com/amitkumaramithotmailcom/E-Claim-Docker/blob/main/Api_Image/docker-compose.yml)
    APP docker-compose.yml : [text](https://github.com/amitkumaramithotmailcom/E-Claim-Docker/blob/main/App_Image/docker-compose.yml)

#### 5. Move on docker-compose.yml file folder
    For API
    $ cd Api_Image

    For APP
    $ cd App_Image

#### 6. Execute docker-compose command for up images
	$ docker-compose up -d

#### 7. execute docker-compose command for down images
	$ docker-compose down






